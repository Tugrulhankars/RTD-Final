using Application.Features.Strategies.Rules;
using Application.Features.Strategies.Dtos;
using Application.Services;
using Infrastructure.Services.Grpc.Services;
using Infrastructure.Services.RabbitMQ;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NRules;
using NRules.Fluent;
using StrategyRuleService.Protos;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using Domain.Enums;
using Domain.Events;

namespace Application.Services
{
    public class NRulesService : INRulesService
    {
        private readonly ILogger<NRulesService> _logger;
        private readonly ISessionFactory _sessionFactory;
        private readonly ConcurrentDictionary<string, ISession> _strategySessions;
        private readonly IServiceScopeFactory _scopeFactory;

        public NRulesService(
            ILogger<NRulesService> logger, 
            IServiceScopeFactory scopeFactory)
        {
            _logger = logger;
            _scopeFactory = scopeFactory;
            _strategySessions = new ConcurrentDictionary<string, ISession>();
            _logger.LogInformation("NRulesService instance oluşturuldu: {Hash}", GetHashCode());

            // NRules repository ve factory oluştur
            var repository = new RuleRepository();
            repository.Load(x => x.From(typeof(TimeCheckRule).Assembly));
            _sessionFactory = repository.Compile();
        }

        public async Task ProcessRulesAsync()
        {
            try
            {
                _logger.LogInformation("ProcessRulesAsync başladı - Toplam {Count} strateji işlenecek", _strategySessions.Count);

                if (_strategySessions.Count == 0)
                {
                    _logger.LogWarning("ProcessRulesAsync: Hiç strateji yok, işlenecek bir şey yok");
                    return;
                }

                foreach (var kvp in _strategySessions)
                {
                    var strategyName = kvp.Key;
                    var session = kvp.Value;
                    
                    _logger.LogInformation("Strateji işleniyor: {StrategyName}", strategyName);
                    
                    await UpdateContextAsync(session);
                    
                    // Step değişikliklerini yakalamak için birden fazla kez Fire() çağır
                    // Her Fire() sonrası fact'leri Update et ki sonraki kurallar tetiklensin
                    int maxIterations = 10; // Maksimum iterasyon sayısı (sonsuz döngüyü önlemek için)
                    int iteration = 0;
                    int previousFiredCount = 0;
                    
                    while (iteration < maxIterations)
                    {
                        _logger.LogDebug("Kurallar tetikleniyor: {StrategyName}, Iteration: {Iteration}", strategyName, iteration + 1);
                        
                        var firedCount = session.Fire();
                        _logger.LogDebug("Fired rules count: {Count}", firedCount);
                        
                        // Eğer hiç kural tetiklenmediyse, döngüden çık
                        if (firedCount == 0)
                        {
                            _logger.LogDebug("Hiç kural tetiklenmedi, döngü sonlandı");
                            break;
                        }
                        
                        // Fact'leri güncelle (Step değişikliklerini session'a bildir)
                        var facts = session.Query<StockWorkflow>().ToList();
                        foreach (var fact in facts)
                        {
                            session.Update(fact);
                            _logger.LogDebug("Fact güncellendi: {Symbol}, Step={Step}", fact.Symbol, fact.Step);
                        }
                        
                        // Eğer aynı sayıda kural tetiklendiyse (değişiklik yok), döngüden çık
                        if (firedCount == previousFiredCount && iteration > 0)
                        {
                            _logger.LogDebug("Kural tetiklenme sayısı değişmedi, döngü sonlandı");
                            break;
                        }
                        
                        previousFiredCount = firedCount;
                        iteration++;
                    }
                    
                    if (iteration >= maxIterations)
                    {
                        _logger.LogWarning("Maksimum iterasyon sayısına ulaşıldı: {StrategyName}", strategyName);
                    }
                    
                    // StrategyEvent'leri topla ve kaydet
                    _logger.LogDebug("Event'ler kaydediliyor: {StrategyName}", strategyName);
                    await SaveStrategyEventsAsync(session);
                }
                
                _logger.LogInformation("ProcessRulesAsync tamamlandı");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Kurallar işlenirken hata oluştu");
            }
        }

        public async Task AddStrategyAsync(string strategyName, object context)
        {
            try
            {
                var session = _sessionFactory.CreateSession();
                
                // Eğer context StockWorkflow ise, TradeService ve PortfolioService delegate'lerini set et
                if (context is StockWorkflow workflow)
                {
                    workflow.TradeService = async (request) =>
                    {
                        using var scope = _scopeFactory.CreateScope();
                        var tradeService = scope.ServiceProvider.GetService<ITradeService>();
                        if (tradeService == null)
                        {
                            _logger.LogWarning("TradeService çözümlenemedi. Alım/Satım isteği simüle edilecek. Symbol={Symbol}", workflow.Symbol);
                            return null;
                        }

                        return await tradeService.CreateTrade(request);
                    };
                    
                    workflow.PortfolioService = async (portfolioId, symbol) =>
                    {
                        if (portfolioId <= 0 || string.IsNullOrWhiteSpace(symbol))
                            return false;

                        using var scope = _scopeFactory.CreateScope();
                        var portfolioService = scope.ServiceProvider.GetService<IPortfolioService>();
                        if (portfolioService == null)
                        {
                            _logger.LogWarning("PortfolioService çözümlenemedi. Symbol={Symbol}", symbol);
                            return false;
                        }

                        return await portfolioService.IsInPortfolio(portfolioId, symbol);
                    };
                    
                    workflow.AccountService = async (accountId) =>
                    {
                        if (accountId <= 0)
                            return 0;

                        using var scope = _scopeFactory.CreateScope();
                        var accountService = scope.ServiceProvider.GetService<IAccountService>();
                        if (accountService == null)
                        {
                            _logger.LogWarning("AccountService çözümlenemedi. AccountId={AccountId}", accountId);
                            return 0;
                        }

                        try
                        {
                            var balance = await accountService.GetAccountBalanceAsync(accountId);
                            return (decimal)balance;
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "AccountService'den bakiye alınamadı. AccountId={AccountId}", accountId);
                            return 0;
                        }
                    };
                    
                    workflow.MarketDataService = async (symbol) =>
                    {
                        if (string.IsNullOrWhiteSpace(symbol))
                            return null;

                        using var scope = _scopeFactory.CreateScope();
                        var marketDataService = scope.ServiceProvider.GetService<IMarketDataService>();
                        if (marketDataService == null)
                        {
                            _logger.LogWarning("MarketDataService çözümlenemedi. Symbol={Symbol}", symbol);
                            return null;
                        }

                        try
                        {
                            return await marketDataService.GetStockInfo(symbol);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "MarketDataService'den stock info alınamadı. Symbol={Symbol}", symbol);
                            return null;
                        }
                    };
                }
                
                session.Insert(context);

                if (_strategySessions.TryAdd(strategyName, session))
                {
                    _logger.LogInformation("Strateji başarıyla eklendi: {StrategyName}", strategyName);
                }
                else
                {
                    _logger.LogWarning("Strateji eklenemedi, zaten mevcut: {StrategyName}", strategyName);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Strateji eklenirken hata oluştu: {StrategyName}", strategyName);
            }

        }

        public async Task RemoveStrategyAsync(string strategyName)
        {
            try
            {
                if (_strategySessions.TryRemove(strategyName, out var session))
                {
                    // ISession IDisposable değil, sadece kaldır
                    _logger.LogInformation("Strateji kaldırıldı: {StrategyName}", strategyName);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Strateji kaldırılırken hata oluştu: {StrategyName}", strategyName);
            }
        }

        private async Task UpdateContextAsync(ISession session)
        {
            var currentTime = DateTime.Now;
            
            using var scope = _scopeFactory.CreateScope();
            var marketDataService = scope.ServiceProvider.GetService<IMarketDataService>();

            // Context'i güncelle
            var facts = session.Query<StockWorkflow>().ToList();
            foreach (var fact in facts)
            {
                try
                {
                    // MarketDataService'ten gerçek veri al (eğer mevcut ise)
                    if (marketDataService != null && !string.IsNullOrEmpty(fact.Symbol))
                    {
                        try
                        {
                            // Tüm market verilerini tek seferde al (StockInfoDto)
                            var stockInfo = await marketDataService.GetStockInfo(fact.Symbol);
                            
                            if (stockInfo?.Quote != null)
                            {
                                var quote = stockInfo.Quote;
                                
                                // Açılış fiyatı günün açılış fiyatıdır ve bir kez alınmalı, sabit kalmalı
                                if (fact.OpeningPrice <= 0)
                                {
                                    fact.OpeningPrice = quote.OpenPrice;
                                }
                                
                                // Sürekli güncellenen fiyat verileri
                                fact.CurrentPrice = quote.CurrentPrice;
                                fact.HighPrice = quote.HighPrice;
                                fact.LowPrice = quote.LowPrice;
                                fact.PreviousClosePrice = quote.PreviousClosePrice;
                                fact.Change = quote.Change;
                                fact.PercentChange = quote.PercentChange;
                                
                                // Şirket bilgileri
                                if (stockInfo.Profile != null)
                                {
                                    fact.CompanyName = stockInfo.Profile.Name;
                                    fact.Exchange = stockInfo.Profile.Exchange;
                                    fact.Industry = stockInfo.Profile.Industry;
                                    fact.Currency = stockInfo.Profile.Currency;
                                }
                                
                                // Finansal metrikler
                                if (stockInfo.Metrics != null)
                                {
                                    fact.Pe = stockInfo.Metrics.Pe;
                                    fact.Pb = stockInfo.Metrics.Pb;
                                    fact.Roe = stockInfo.Metrics.Roe;
                                    fact.NetMargin = stockInfo.Metrics.NetMargin;
                                    fact.DebtEquity = stockInfo.Metrics.DebtEquity;
                                }
                                
                                _logger.LogDebug("MarketDataService'ten tüm veriler alındı: {Symbol} - Current={CurrentPrice:F2}, Open={OpenPrice:F2}, Change={PercentChange:F2}%", 
                                    fact.Symbol, fact.CurrentPrice, fact.OpeningPrice, fact.PercentChange);
                            }
                            else
                            {
                                // Fallback: Eski metodlar
                                var currentPrice = await marketDataService.GetStockCurrentPrice(fact.Symbol);
                                if (fact.OpeningPrice <= 0)
                                {
                                    var openingPrice = await marketDataService.GetStockOpeningPrice(fact.Symbol);
                                    fact.OpeningPrice = (decimal)openingPrice;
                                }
                                fact.CurrentPrice = (decimal)currentPrice;
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "MarketDataService'ten veri alınamadı, simüle veri kullanılacak: {Symbol}", fact.Symbol);
                            await UpdateWithSimulatedData(fact, currentTime);
                        }
                    }
                    else
                    {
                        // MarketDataService yoksa simüle veri kullan
                        await UpdateWithSimulatedData(fact, currentTime);
                    }
                    
                    // Zarar yüzdesi hesapla
                    if (fact.OpeningPrice > 0)
                    {
                        fact.TotalLossPercent = ((fact.CurrentPrice - fact.OpeningPrice) / fact.OpeningPrice) * 100;
                    }
                    
                    // Zamanı güncelle
                    fact.Now = currentTime;
                    
                    // NRules'a fact değişikliğini bildir
                    session.Update(fact);
                    
                    _logger.LogDebug("Veri güncellendi: {Symbol} - Fiyat={CurrentPrice:F2}, Zarar={TotalLossPercent:F2}%, Step={Step}", 
                        fact.Symbol, fact.CurrentPrice, fact.TotalLossPercent, fact.Step);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Context güncellenirken hata oluştu: {Symbol}", fact.Symbol);
                }
            }
        }
        
        private async Task UpdateWithSimulatedData(StockWorkflow fact, DateTime currentTime)
        {
            var random = new Random();
            // Fiyat değişimi simülasyonu (%-2 ile +2 arası)
            var priceChange = (decimal)(random.NextDouble() - 0.5) * 4m;
            fact.CurrentPrice = Math.Max(1, fact.CurrentPrice + priceChange);
            
            // İlk kez açılış fiyatı set ediliyorsa
            if (fact.OpeningPrice == 0)
            {
                fact.OpeningPrice = fact.CurrentPrice;
            }
        }
        
        private async Task SaveStrategyEventsAsync(ISession session)
        {
            try
            {
                // StockWorkflow'lardan StrategyEvent'leri topla
                var workflows = session.Query<StockWorkflow>().ToList();
                
                _logger.LogDebug("SaveStrategyEventsAsync: {Count} workflow bulundu", workflows.Count);
                
                foreach (var workflow in workflows)
                {
                    if (workflow.StrategyEvents != null && workflow.StrategyEvents.Any())
                    {
                        _logger.LogInformation("[{Symbol}] StrategyId={StrategyId}, Step={Step} - {Count} adet StrategyEvent bulundu", 
                            workflow.Symbol, workflow.StrategyId, workflow.Step, workflow.StrategyEvents.Count);
                        
                        // Her event'i veritabanına kaydet
                        foreach (var strategyEvent in workflow.StrategyEvents)
                        {
                            try
                            {
                                // StrategyId'nin doğru set edildiğinden emin ol
                                if (strategyEvent.StrategyId <= 0 && workflow.StrategyId > 0)
                                {
                                    strategyEvent.StrategyId = workflow.StrategyId;
                                    _logger.LogDebug("StrategyEvent StrategyId güncellendi: {OldId} -> {NewId}", 
                                        strategyEvent.StrategyId, workflow.StrategyId);
                                }
                                
                                // Event'i veritabanına kaydet
                                using var scope = _scopeFactory.CreateScope();
                                var strategyEventRepository = scope.ServiceProvider.GetRequiredService<IStrategyEventRepository>();
                                var savedEvent = await strategyEventRepository.AddAsync(strategyEvent);
                                
                                _logger.LogInformation("✓ Event kaydedildi: Id={Id}, StrategyId={StrategyId}, Step={Step}, RuleName={RuleName}, Action={Action}, Reason={Reason}, Fiyat={Price:F2}, Timestamp={Timestamp}", 
                                    savedEvent.Id, savedEvent.StrategyId, savedEvent.Step, savedEvent.RuleName, 
                                    savedEvent.Action, savedEvent.Reason, savedEvent.Price, savedEvent.Timestamp);
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "✗ StrategyEvent kaydedilirken hata oluştu: RuleName={RuleName}, StrategyId={StrategyId}, Step={Step}, Action={Action}", 
                                    strategyEvent.RuleName, strategyEvent.StrategyId, strategyEvent.Step, strategyEvent.Action);
                            }
                        }
                        
                        // Başarılı alış/satış durumlarında notification gönder
                        var successfulBuyOrSellEvents = workflow.StrategyEvents
                            .Where(e => e.Action == "BUY" || e.Action == "SELL")
                            .ToList();
                        
                        // Piyasa kapalı durumunda notification gönder
                        var marketClosedEvents = workflow.StrategyEvents
                            .Where(e => e.Action == "MARKET_CLOSED")
                            .ToList();
                        
                        // Başarılı alış veya satış yapıldıysa notification gönder
                        if (successfulBuyOrSellEvents.Any())
                        {
                            try
                            {
                                await SendNotificationAsync(workflow, successfulBuyOrSellEvents);
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "Notification gönderilirken hata oluştu: StrategyId={StrategyId}", 
                                    workflow.StrategyId);
                                // Notification hatası event kaydetmeyi engellemez
                            }
                        }
                        
                        // Piyasa kapalıyken notification gönder
                        if (marketClosedEvents.Any())
                        {
                            try
                            {
                                await SendMarketClosedNotificationAsync(workflow, marketClosedEvents.First());
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "Piyasa kapalı notification gönderilirken hata oluştu: StrategyId={StrategyId}", 
                                    workflow.StrategyId);
                                // Notification hatası event kaydetmeyi engellemez
                            }
                        }
                        
                        // Event'leri temizle (tekrar kaydedilmemesi için)
                        workflow.StrategyEvents.Clear();
                        _logger.LogDebug("[{Symbol}] StrategyEvents temizlendi", workflow.Symbol);
                    }
                    else
                    {
                        _logger.LogDebug("[{Symbol}] StrategyId={StrategyId}, Step={Step} - Event yok", 
                            workflow.Symbol, workflow.StrategyId, workflow.Step);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "StrategyEvent'ler kaydedilirken genel hata oluştu");
            }
        }
        
        private async Task SendNotificationAsync(StockWorkflow workflow, List<Domain.Entities.StrategyEvent> successfulBuyOrSellEvents)
        {
            using var scope = _scopeFactory.CreateScope();
            var strategyRepository = scope.ServiceProvider.GetRequiredService<IStrategyRepository>();
            var strategyEventRepository = scope.ServiceProvider.GetRequiredService<IStrategyEventRepository>();
            var rabbitMQPublisher = scope.ServiceProvider.GetService<IRabbitMQPublisher>();
            
            if (rabbitMQPublisher == null)
            {
                _logger.LogWarning("RabbitMQPublisher bulunamadı, notification gönderilemedi: StrategyId={StrategyId}", 
                    workflow.StrategyId);
                return;
            }
            
            // Strateji bilgilerini al
            var strategy = await strategyRepository.GetAsync(
                s => s.Id == workflow.StrategyId,
                cancellationToken: default);
            
            if (strategy == null)
            {
                _logger.LogWarning("Strateji bulunamadı, notification gönderilemedi: StrategyId={StrategyId}", 
                    workflow.StrategyId);
                return;
            }
            
            // Tüm event'leri al (kullanıcıya gönderilecek - alış/satışa kadar çalışan kurallar)
            var allEvents = await strategyEventRepository.GetAllAsync(
                e => e.StrategyId == workflow.StrategyId,
                cancellationToken: default);
            
            // Başarılı alım/satım event'ini bul (sadece "BUY" veya "SELL" action'ları)
            var buyEvent = successfulBuyOrSellEvents.FirstOrDefault(e => e.Action == "BUY");
            var sellEvent = successfulBuyOrSellEvents.FirstOrDefault(e => e.Action == "SELL");
            
            // Event oluştur - sadece başarılı alış/satış için
            var strategyNotificationEvent = new StrategyNotificationEvent
            {
                StrategyId = strategy.Id,
                UserId = strategy.UserId,
                StrategyName = strategy.StrategyName,
                StockSymbol = strategy.StockSymbol,
                Status = strategy.Status.ToString(),
                Action = buyEvent != null ? "BUY" : "SELL", // Sadece başarılı alış veya satış
                BuyPrice = buyEvent?.Price ?? strategy.BuyPrice,
                SellPrice = sellEvent?.Price ?? strategy.SellPrice,
                CurrentPrice = workflow.CurrentPrice,
                Timestamp = DateTime.Now,
                // Alış/satışa kadar çalışan tüm kuralları ekle
                ExecutedRules = allEvents
                    .OrderBy(e => e.Step)
                    .ThenBy(e => e.Timestamp)
                    .Select(e => new Domain.Events.RuleExecutionInfo
                    {
                        RuleName = e.RuleName,
                        Step = e.Step,
                        Action = e.Action,
                        Reason = e.Reason,
                        Price = e.Price,
                        Timestamp = e.Timestamp
                    })
                    .ToList()
            };
            
            // Kar/Zarar hesapla
            if (buyEvent != null && sellEvent != null)
            {
                strategyNotificationEvent.ProfitLoss = sellEvent.Price - buyEvent.Price;
            }
            else if (buyEvent != null)
            {
                strategyNotificationEvent.ProfitLoss = workflow.CurrentPrice - buyEvent.Price;
            }
            else if (sellEvent != null && strategy.BuyPrice.HasValue)
            {
                strategyNotificationEvent.ProfitLoss = sellEvent.Price - strategy.BuyPrice.Value;
            }
            
            // Stratejiyi güncelle (alım/satım fiyatları ve durum)
            if (buyEvent != null)
            {
                strategy.BuyPrice = buyEvent.Price;
                strategy.IsPositionOpen = true;
                strategy.TotalTransactions++;
            }
            
            if (sellEvent != null)
            {
                strategy.SellPrice = sellEvent.Price;
                strategy.IsPositionOpen = false;
                if (strategy.BuyPrice.HasValue)
                {
                    strategy.ProfitLoss = sellEvent.Price - strategy.BuyPrice.Value;
                    if (strategy.ProfitLoss > 0)
                    {
                        strategy.TotalProfit += strategy.ProfitLoss.Value;
                        strategy.SuccessfulTransactions++;
                    }
                    else
                    {
                        strategy.TotalLoss += Math.Abs(strategy.ProfitLoss.Value);
                    }
                }
            }
            
            await strategyRepository.UpdateAsync(strategy);
            
            // RabbitMQ'ya event gönder - sadece başarılı alış/satış için bir kez
            try
            {
                await rabbitMQPublisher.PublishAsync(strategyNotificationEvent, "strategy-notifications");
                
                // Notification gönderildi event'ini kaydet
                var notificationEvent = new Domain.Entities.StrategyEvent
                {
                    StrategyId = strategy.Id,
                    Step = buyEvent?.Step ?? sellEvent?.Step ?? -1,
                    RuleName = "NotificationService",
                    Action = "NOTIFICATION_SENT",
                    Reason = $"Başarılı {strategyNotificationEvent.Action} işlemi için event gönderildi. Fiyat: {buyEvent?.Price ?? sellEvent?.Price ?? 0:F2}",
                    Price = buyEvent?.Price ?? sellEvent?.Price ?? workflow.CurrentPrice,
                    Timestamp = DateTime.Now
                };
                
                await strategyEventRepository.AddAsync(notificationEvent);
                
                _logger.LogInformation("Strateji notification event'i gönderildi ve kaydedildi: StrategyId={StrategyId}, Action={Action}, UserId={UserId}, Price={Price}", 
                    strategy.Id, strategyNotificationEvent.Action, strategy.UserId, 
                    buyEvent?.Price ?? sellEvent?.Price ?? 0);
            }
            catch (Exception ex)
            {
                // Notification gönderilemedi event'ini kaydet
                var notificationFailedEvent = new Domain.Entities.StrategyEvent
                {
                    StrategyId = strategy.Id,
                    Step = buyEvent?.Step ?? sellEvent?.Step ?? -1,
                    RuleName = "NotificationService",
                    Action = "NOTIFICATION_FAILED",
                    Reason = $"Event gönderilemedi: {ex.Message}",
                    Price = buyEvent?.Price ?? sellEvent?.Price ?? workflow.CurrentPrice,
                    Timestamp = DateTime.Now
                };
                
                await strategyEventRepository.AddAsync(notificationFailedEvent);
                
                _logger.LogError(ex, "Event gönderilirken hata oluştu ve event kaydedildi: StrategyId={StrategyId}", 
                    strategy.Id);
                throw; // Hata yukarı fırlatılıyor ama event kaydedildi
            }
        }
        
        private async Task SendMarketClosedNotificationAsync(StockWorkflow workflow, Domain.Entities.StrategyEvent marketClosedEvent)
        {
            using var scope = _scopeFactory.CreateScope();
            var strategyRepository = scope.ServiceProvider.GetRequiredService<IStrategyRepository>();
            var strategyEventRepository = scope.ServiceProvider.GetRequiredService<IStrategyEventRepository>();
            var rabbitMQPublisher = scope.ServiceProvider.GetService<IRabbitMQPublisher>();
            
            if (rabbitMQPublisher == null)
            {
                _logger.LogWarning("RabbitMQPublisher bulunamadı, piyasa kapalı notification gönderilemedi: StrategyId={StrategyId}", 
                    workflow.StrategyId);
                return;
            }
            
            // Strateji bilgilerini al
            var strategy = await strategyRepository.GetAsync(
                s => s.Id == workflow.StrategyId,
                cancellationToken: default);
            
            if (strategy == null)
            {
                _logger.LogWarning("Strateji bulunamadı, piyasa kapalı notification gönderilemedi: StrategyId={StrategyId}", 
                    workflow.StrategyId);
                return;
            }
            
            // Piyasa kapalı notification event'i oluştur
            var strategyNotificationEvent = new StrategyNotificationEvent
            {
                StrategyId = strategy.Id,
                UserId = strategy.UserId,
                StrategyName = strategy.StrategyName,
                StockSymbol = workflow.Symbol,
                Status = strategy.Status.ToString(),
                Action = "MARKET_CLOSED",
                CurrentPrice = workflow.CurrentPrice,
                Timestamp = DateTime.Now,
                Reason = marketClosedEvent.Reason ?? "Piyasa kapalı - Strateji gerçekleştirilemedi. Piyasa saatleri: 10:00-17:59",
                ExecutedRules = new List<Domain.Events.RuleExecutionInfo>
                {
                    new Domain.Events.RuleExecutionInfo
                    {
                        RuleName = marketClosedEvent.RuleName,
                        Step = marketClosedEvent.Step,
                        Action = marketClosedEvent.Action,
                        Reason = marketClosedEvent.Reason,
                        Price = marketClosedEvent.Price,
                        Timestamp = marketClosedEvent.Timestamp
                    }
                }
            };
            
            // RabbitMQ'ya event gönder
            try
            {
                await rabbitMQPublisher.PublishAsync(strategyNotificationEvent, "strategy-notifications");
                
                // Notification gönderildi event'ini kaydet
                var notificationEvent = new Domain.Entities.StrategyEvent
                {
                    StrategyId = strategy.Id,
                    Step = marketClosedEvent.Step,
                    RuleName = "NotificationService",
                    Action = "MARKET_CLOSED_NOTIFICATION_SENT",
                    Reason = $"Piyasa kapalı bildirimi gönderildi. Piyasa saatleri: 10:00-17:59",
                    Price = workflow.CurrentPrice,
                    Timestamp = DateTime.Now
                };
                
                await strategyEventRepository.AddAsync(notificationEvent);
                
                _logger.LogInformation("Piyasa kapalı notification event'i gönderildi ve kaydedildi: StrategyId={StrategyId}, UserId={UserId}, Symbol={Symbol}", 
                    strategy.Id, strategy.UserId, workflow.Symbol);
            }
            catch (Exception ex)
            {
                // Notification gönderilemedi event'ini kaydet
                var notificationFailedEvent = new Domain.Entities.StrategyEvent
                {
                    StrategyId = strategy.Id,
                    Step = marketClosedEvent.Step,
                    RuleName = "NotificationService",
                    Action = "MARKET_CLOSED_NOTIFICATION_FAILED",
                    Reason = $"Piyasa kapalı bildirimi gönderilemedi: {ex.Message}",
                    Price = workflow.CurrentPrice,
                    Timestamp = DateTime.Now
                };
                
                await strategyEventRepository.AddAsync(notificationFailedEvent);
                
                _logger.LogError(ex, "Piyasa kapalı event gönderilirken hata oluştu ve event kaydedildi: StrategyId={StrategyId}", 
                    strategy.Id);
                throw; // Hata yukarı fırlatılıyor ama event kaydedildi
            }
        }
    }
}
