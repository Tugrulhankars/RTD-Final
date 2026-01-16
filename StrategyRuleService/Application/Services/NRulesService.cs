using Application.Features.Strategies.Rules;
using Application.Features.Strategies.Dtos;
using Application.Services;
using Infrastructure.Services.Grpc.Services;
using Infrastructure.Services.RabbitMQ;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using System.Net.Http;
using Grpc.Core;
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
        private readonly FlowchartLogger _flowchartLogger;
        public NRulesService(
            ILogger<NRulesService> logger, 
            IServiceScopeFactory scopeFactory,
            FlowchartLogger flowchartLogger)
        {
            _logger = logger;
            _scopeFactory = scopeFactory;
            _flowchartLogger = flowchartLogger;
            _strategySessions = new ConcurrentDictionary<string, ISession>();
            _logger.LogInformation("NRulesService instance oluşturuldu: {Hash}", GetHashCode());
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
                var tasks = new List<Task>();
                foreach (var kvp in _strategySessions)
                {
                    var strategyName = kvp.Key;
                    var session = kvp.Value;
                    var task = Task.Run(async () =>
                    {
                        try
                        {
                            await ProcessStrategyAsync(strategyName, session);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Strateji işlenirken hata oluştu: {StrategyName}", strategyName);
                        }
                    });
                    tasks.Add(task);
                }
                await Task.WhenAll(tasks);
                _logger.LogInformation("ProcessRulesAsync tamamlandı - {Count} strateji işlendi", tasks.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Kurallar işlenirken hata oluştu");
            }
        }
        private async Task ProcessStrategyAsync(string strategyName, ISession session)
        {
            _logger.LogInformation("Strateji işleniyor: {StrategyName}", strategyName);
            await UpdateContextAsync(session);
            
            // Strateji durdurulmuş mu kontrol et
            var workflow = session.Query<StockWorkflow>().FirstOrDefault();
            if (workflow != null && workflow.Cancelled)
            {
                _logger.LogInformation("Strateji durdurulmuş, işlenmeyecek: {StrategyName}, StrategyId={StrategyId}", 
                    strategyName, workflow.StrategyId);
                return;
            }
            
            int maxIterations = 10;
            int iteration = 0;
            int previousFiredCount = 0;
            while (iteration < maxIterations)
            {
                _logger.LogDebug("Kurallar tetikleniyor: {StrategyName}, Iteration: {Iteration}", strategyName, iteration + 1);
                
                // Her iterasyonda strateji durdurulmuş mu kontrol et
                workflow = session.Query<StockWorkflow>().FirstOrDefault();
                if (workflow != null && workflow.Cancelled)
                {
                    _logger.LogInformation("Strateji durdurulmuş, döngü sonlandırılıyor: {StrategyName}, StrategyId={StrategyId}", 
                        strategyName, workflow.StrategyId);
                    break;
                }
                
                var factsBefore = session.Query<StockWorkflow>().ToList();
                var currentStepBefore = factsBefore.FirstOrDefault()?.Step ?? 0;
                
                // Fact'leri session'a Update et ki rule'lar değişiklikleri görebilsin
                foreach (var fact in factsBefore)
                {
                    session.Update(fact);
                }
                
                var firedCount = session.Fire();
                _logger.LogInformation("🔥 İlk Fire() - Tetiklenen kural sayısı: {Count}, Step öncesi: {StepBefore}", firedCount, currentStepBefore);
                
                // Rule'lar çalıştıktan sonra fact'leri tekrar al ve Update et
                var factsAfter = session.Query<StockWorkflow>().ToList();
                var currentStepAfter = factsAfter.FirstOrDefault()?.Step ?? 0;
                
                _logger.LogInformation("📊 Step durumu - Önce: {StepBefore}, Sonra: {StepAfter}", currentStepBefore, currentStepAfter);
                
                // Fact'leri Update et - Step değişikliğini session'a bildir
                foreach (var fact in factsAfter)
                {
                    session.Update(fact);
                    _logger.LogInformation("🔄 Fact Update edildi: {Symbol}, Step={Step} (önceki Step={StepBefore})", 
                        fact.Symbol, fact.Step, currentStepBefore);
                    
                    var currentRule = GetCurrentRuleName(fact.Step, currentStepBefore);
                    var decision = GetDecision(fact, currentStepBefore);
                    var reason = GetReason(fact, currentRule);
                    if (iteration % 5 == 0 || fact.Step != currentStepBefore)
                    {
                        _flowchartLogger.LogFlowchart(fact, currentRule, decision.Item1, decision.Item2, reason);
                    }
                    else
                    {
                        _flowchartLogger.LogSimpleFlowchart(fact, currentRule, decision.Item1, reason);
                    }
                    await UpdateStrategyCurrentStepAsync(fact.StrategyId, fact.Step);
                }
                
                // Eğer Step değiştiyse, yeni rule'ları hemen tetiklemek için tekrar Fire et
                if (currentStepAfter != currentStepBefore)
                {
                    _logger.LogWarning("⚠️⚠️⚠️ Step değişti ({StepBefore} -> {StepAfter}), BuyRule tetiklenmeli! Tekrar Fire ediliyor...", 
                        currentStepBefore, currentStepAfter);
                    
                    // AGGRESIF YAKLAŞIM: Fact'leri Retract edip tekrar Insert et - NRules'ın değişikliği kesin algılaması için
                    foreach (var fact in factsAfter)
                    {
                        _logger.LogInformation("🔄🔄🔄 Fact Retract ediliyor ve tekrar Insert ediliyor (Step değişikliği): {Symbol}, Step={Step}, Cancelled={Cancelled}", 
                            fact.Symbol, fact.Step, fact.Cancelled);
                        
                        // Fact'i Retract et
                        session.Retract(fact);
                        
                        // Fact'i tekrar Insert et - Bu, NRules'ın fact değişikliğini kesin algılamasını sağlar
                        session.Insert(fact);
                        
                        _logger.LogInformation("✅ Fact Retract/Insert tamamlandı: {Symbol}, Step={Step}", fact.Symbol, fact.Step);
                    }
                    
                    // İkinci Fire - BuyRule burada tetiklenmeli
                    var additionalFiredCount = session.Fire();
                    _logger.LogWarning("🔥🔥🔥 İkinci Fire() (Retract/Insert sonrası) - Ek rule'lar tetiklendi: {Count} (Step {StepBefore} -> {StepAfter})", 
                        additionalFiredCount, currentStepBefore, currentStepAfter);
                    
                    if (additionalFiredCount == 0 && currentStepAfter == 3)
                    {
                        _logger.LogError("❌❌❌ HATA: Step 3'e geçildi ama BuyRule tetiklenmedi! Fact durumu kontrol ediliyor...");
                        var factForDebug = factsAfter.FirstOrDefault();
                        if (factForDebug != null)
                        {
                            _logger.LogError("🔍 Fact detayları: Step={Step}, Cancelled={Cancelled}, Symbol={Symbol}, AccountId={AccountId}, OpeningPrice={OpeningPrice}, CurrentPrice={CurrentPrice}",
                                factForDebug.Step, factForDebug.Cancelled, factForDebug.Symbol, factForDebug.AccountId, 
                                factForDebug.OpeningPrice, factForDebug.CurrentPrice);
                        }
                        
                        // Son çare: Fact'i tekrar Update et ve Fire et
                        _logger.LogWarning("🔄🔄🔄 Son çare: Fact Update ediliyor ve tekrar Fire ediliyor...");
                        foreach (var fact in factsAfter)
                        {
                            session.Update(fact);
                        }
                        var lastFiredCount = session.Fire();
                        _logger.LogWarning("🔥🔥🔥 Son Fire() - Tetiklenen kural sayısı: {Count}", lastFiredCount);
                        firedCount += lastFiredCount;
                    }
                    else
                    {
                        firedCount += additionalFiredCount;
                    }
                    
                    // İkinci Fire sonrası da fact'leri güncelle
                    var factsAfterSecondFire = session.Query<StockWorkflow>().ToList();
                    foreach (var fact in factsAfterSecondFire)
                    {
                        session.Update(fact);
                        await UpdateStrategyCurrentStepAsync(fact.StrategyId, fact.Step);
                    }
                }
                
                if (firedCount == 0)
                {
                    _logger.LogDebug("Hiç kural tetiklenmedi, döngü sonlandı");
                    break;
                }
                
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
            _logger.LogDebug("Event'ler kaydediliyor: {StrategyName}", strategyName);
            await SaveStrategyEventsAsync(session);
        }
        private async Task UpdateStrategyCurrentStepAsync(int strategyId, int currentStep)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var strategyRepository = scope.ServiceProvider.GetRequiredService<IStrategyRepository>();
                var strategy = await strategyRepository.GetAsync(
                    s => s.Id == strategyId,
                    cancellationToken: default);
                if (strategy != null && strategy.CurrentStep != currentStep)
                {
                    strategy.CurrentStep = currentStep;
                    if (strategy.Status == Domain.Enums.StrategyStatus.Waiting)
                    {
                        strategy.Status = Domain.Enums.StrategyStatus.Active;
                    }
                    await strategyRepository.UpdateAsync(strategy);
                    _logger.LogDebug("Strategy CurrentStep güncellendi: StrategyId={StrategyId}, CurrentStep={CurrentStep}", 
                        strategyId, currentStep);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Strategy CurrentStep güncellenirken hata oluştu: StrategyId={StrategyId}, CurrentStep={CurrentStep}", 
                    strategyId, currentStep);
            }
        }
        public async Task AddStrategyAsync(string strategyName, object context)
        {
            try
            {
                var session = _sessionFactory.CreateSession();
                if (context is StockWorkflow workflow)
                {
                    // Strateji eklendiğinde Cancelled durumunu sıfırla
                    workflow.Cancelled = false;
                    workflow.Symbol = workflow.Symbol.ToUpper().Trim();
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
                        {
                            _logger.LogWarning("⚠️ PortfolioId veya Symbol geçersiz - PortfolioId={PortfolioId}, Symbol={Symbol}", portfolioId, symbol);
                            return false;
                        }
                        using var scope = _scopeFactory.CreateScope();
                        var portfolioService = scope.ServiceProvider.GetService<IPortfolioService>();
                        if (portfolioService == null)
                        {
                            _logger.LogError("❌ PortfolioService çözümlenemedi. Symbol={Symbol}, PortfolioId={PortfolioId}", symbol, portfolioId);
                            return false;
                        }
                        try
                        {
                            _logger.LogInformation("📦 PortfolioService.IsInPortfolio çağrılıyor - PortfolioId={PortfolioId}, Symbol={Symbol}", portfolioId, symbol);
                            var result = await portfolioService.IsInPortfolio(portfolioId, symbol);
                            _logger.LogWarning("📦📦📦 PortfolioService.IsInPortfolio sonucu - PortfolioId={PortfolioId}, Symbol={Symbol}, Result={Result}", portfolioId, symbol, result);
                            return result;
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "❌❌❌ PortfolioService.IsInPortfolio hatası - PortfolioId={PortfolioId}, Symbol={Symbol}, Error={Error}", portfolioId, symbol, ex.Message);
                            return false;
                        }
                    };
                    workflow.AccountService = async (accountId) =>
                    {
                        int actualAccountId = accountId;
                        
                        // Eğer AccountId 0 veya geçersizse, UserId'den AccountId'yi al
                        if (accountId <= 0 && workflow.UserId > 0)
                        {
                            _logger.LogWarning("⚠️ AccountId geçersiz ({AccountId}), UserId'den AccountId alınmaya çalışılıyor - UserId={UserId}", accountId, workflow.UserId);
                            Console.WriteLine($"[NRulesService] ⚠️ AccountId geçersiz ({accountId}), UserId'den AccountId alınmaya çalışılıyor - UserId={workflow.UserId}");
                            
                            try
                            {
                                using var httpScope = _scopeFactory.CreateScope();
                                var configuration = httpScope.ServiceProvider.GetService<IConfiguration>();
                                
                                if (configuration != null)
                                {
                                    var accountServiceBaseUrl = configuration["AccountService:BaseUrl"] ?? "https://localhost:5001";
                                    using var httpClient = new HttpClient();
                                    httpClient.BaseAddress = new Uri(accountServiceBaseUrl);
                                    
                                    var response = await httpClient.GetAsync($"/api/account/getAccountByUser/{workflow.UserId}");
                                    if (response.IsSuccessStatusCode)
                                    {
                                        var jsonContent = await response.Content.ReadAsStringAsync();
                                        _logger.LogInformation("📥 AccountService HTTP response alındı - UserId={UserId}, Response={Response}", workflow.UserId, jsonContent);
                                        Console.WriteLine($"[NRulesService] 📥 AccountService HTTP response alındı - UserId={workflow.UserId}, Response={jsonContent}");
                                        
                                        // JSON'dan AccountId'yi parse et
                                        using var jsonDoc = System.Text.Json.JsonDocument.Parse(jsonContent);
                                        var root = jsonDoc.RootElement;
                                        
                                        if (root.TryGetProperty("accountId", out var accountIdElement) || 
                                            root.TryGetProperty("AccountId", out accountIdElement) ||
                                            root.TryGetProperty("id", out accountIdElement) ||
                                            root.TryGetProperty("Id", out accountIdElement))
                                        {
                                            if (accountIdElement.ValueKind == System.Text.Json.JsonValueKind.Number && 
                                                accountIdElement.TryGetInt32(out var parsedAccountId))
                                            {
                                                actualAccountId = parsedAccountId;
                                                _logger.LogInformation("✅✅✅ UserId'den AccountId alındı - UserId={UserId}, AccountId={AccountId}", workflow.UserId, actualAccountId);
                                                Console.WriteLine($"[NRulesService] ✅✅✅ UserId'den AccountId alındı - UserId={workflow.UserId}, AccountId={actualAccountId}");
                                                
                                                // Workflow'a da set et
                                                workflow.AccountId = actualAccountId;
                                            }
                                        }
                                    }
                                    else
                                    {
                                        var errorContent = await response.Content.ReadAsStringAsync();
                                        _logger.LogWarning("⚠️ UserId'den AccountId alınamadı - HTTP {StatusCode}: {ReasonPhrase}, Response={Response}", 
                                            response.StatusCode, response.ReasonPhrase, errorContent);
                                        Console.WriteLine($"[NRulesService] ⚠️ UserId'den AccountId alınamadı - HTTP {response.StatusCode}: {response.ReasonPhrase}, Response={errorContent}");
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "❌ UserId'den AccountId alınırken hata - UserId={UserId}, Error={Error}", workflow.UserId, ex.Message);
                                Console.WriteLine($"[NRulesService] ❌ UserId'den AccountId alınırken hata - UserId={workflow.UserId}, Error={ex.Message}");
                            }
                        }
                        
                        if (actualAccountId <= 0)
                        {
                            _logger.LogWarning("⚠️ AccountId hala geçersiz: {AccountId}, UserId={UserId}", actualAccountId, workflow.UserId);
                            Console.WriteLine($"[NRulesService] ⚠️ AccountId hala geçersiz: {actualAccountId}, UserId={workflow.UserId}");
                            return 0;
                        }
                        
                        using var scope = _scopeFactory.CreateScope();
                        var accountService = scope.ServiceProvider.GetService<IAccountService>();
                        if (accountService == null)
                        {
                            _logger.LogError("❌ AccountService çözümlenemedi. AccountId={AccountId}", actualAccountId);
                            Console.WriteLine($"[NRulesService] ❌ AccountService çözümlenemedi. AccountId={actualAccountId}");
                            return 0;
                        }
                        try
                        {
                            _logger.LogInformation("💰💰💰 AccountService.GetAccountBalanceAsync çağrılıyor - AccountId={AccountId}, UserId={UserId}, AccountService Type={AccountServiceType}", 
                                actualAccountId, workflow.UserId, accountService.GetType().Name);
                            Console.WriteLine($"[NRulesService] AccountService.GetAccountBalanceAsync çağrılıyor - AccountId={actualAccountId}, UserId={workflow.UserId}, AccountService Type={accountService.GetType().Name}");
                            
                            var balance = await accountService.GetAccountBalanceAsync(actualAccountId);
                            
                            _logger.LogWarning("💰💰💰 BAKİYE ALINDI - AccountId={AccountId}, Balance={Balance} TL, UserId={UserId}", actualAccountId, balance, workflow.UserId);
                            Console.WriteLine($"[NRulesService] ✅✅✅ BAKİYE ALINDI - AccountId={actualAccountId}, Balance={balance} TL, UserId={workflow.UserId}");
                            
                            if (balance <= 0)
                            {
                                _logger.LogWarning("⚠️⚠️⚠️ Bakiye 0 veya negatif! AccountId={AccountId}, Balance={Balance}, UserId={UserId}", actualAccountId, balance, workflow.UserId);
                                Console.WriteLine($"[NRulesService] ⚠️⚠️⚠️ Bakiye 0 veya negatif! AccountId={actualAccountId}, Balance={balance}, UserId={workflow.UserId}");
                            }
                            
                            return (decimal)balance;
                        }
                        catch (Grpc.Core.RpcException rpcEx)
                        {
                            _logger.LogError(rpcEx, "❌❌❌ AccountService gRPC hatası. AccountId={AccountId}, UserId={UserId}, StatusCode={StatusCode}, Detail={Detail}, Error={Error}", 
                                actualAccountId, workflow.UserId, rpcEx.StatusCode, rpcEx.Status.Detail, rpcEx.Message);
                            Console.WriteLine($"[NRulesService] ❌❌❌ AccountService gRPC hatası - AccountId={actualAccountId}, UserId={workflow.UserId}, StatusCode={rpcEx.StatusCode}, Detail={rpcEx.Status.Detail}, Error={rpcEx.Message}");
                            return 0;
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "❌❌❌ AccountService'den bakiye alınamadı. AccountId={AccountId}, UserId={UserId}, Error={Error}, InnerException={InnerException}, StackTrace={StackTrace}", 
                                actualAccountId, workflow.UserId, ex.Message, ex.InnerException?.Message, ex.StackTrace);
                            Console.WriteLine($"[NRulesService] ❌❌❌ AccountService'den bakiye alınamadı - AccountId={actualAccountId}, UserId={workflow.UserId}, Error={ex.Message}, InnerException={ex.InnerException?.Message}, StackTrace={ex.StackTrace}");
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
                    var userPreference = new UserPreference
                    {
                        StrategyId = workflow.StrategyId,
                        UserId = workflow.UserId,
                        Ticker = workflow.Symbol.ToUpper().Trim(),
                        StopLossPercentage = workflow.StopLossPercent,
                        TakeProfitPercentage = workflow.ProfitTargetPercent,
                        EntryThresholdPercentage = workflow.EntryThresholdPercent,
                        MaxLossLimitPercentage = workflow.MaxTotalLoss
                    };
                    workflow.UserPreference = userPreference;
                    
                    // Strateji tamamlandığında (alım/satım emri gönderildiğinde) çağrılacak callback
                    workflow.OnStrategyCompleted = async () =>
                    {
                        try
                        {
                            using var scope = _scopeFactory.CreateScope();
                            var strategyRepository = scope.ServiceProvider.GetRequiredService<IStrategyRepository>();
                            var strategy = await strategyRepository.GetAsync(
                                s => s.Id == workflow.StrategyId,
                                cancellationToken: default);
                            
                            if (strategy != null)
                            {
                                strategy.IsActive = false;
                                strategy.Status = StrategyStatus.Completed;
                                strategy.FinishTime = DateTime.Now;
                                await strategyRepository.UpdateAsync(strategy);
                                _logger.LogInformation("✅ Strateji tamamlandı olarak işaretlendi: StrategyId={StrategyId}, Symbol={Symbol}", 
                                    strategy.Id, workflow.Symbol);
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Strateji tamamlandı olarak işaretlenirken hata: StrategyId={StrategyId}", 
                                workflow.StrategyId);
                        }
                    };
                    
                    session.Insert(workflow);
                    session.Insert(userPreference);
                }
                else
                {
                    session.Insert(context);
                }
                if (_strategySessions.TryAdd(strategyName, session))
                {
                    _logger.LogInformation("Strateji başarıyla eklendi: {StrategyName}", strategyName);
                }
                else
                {
                    // Strateji zaten mevcut, mevcut session'daki workflow'u güncelle
                    _logger.LogWarning("Strateji zaten mevcut, güncelleniyor: {StrategyName}", strategyName);
                    if (_strategySessions.TryGetValue(strategyName, out var existingSession))
                    {
                        var existingWorkflow = existingSession.Query<StockWorkflow>().FirstOrDefault();
                        if (existingWorkflow != null && context is StockWorkflow newWorkflow)
                        {
                            // Mevcut workflow'u yeni değerlerle güncelle
                            existingWorkflow.Cancelled = false; // Cancelled durumunu sıfırla
                            existingWorkflow.Symbol = newWorkflow.Symbol;
                            existingWorkflow.StrategyId = newWorkflow.StrategyId;
                            existingWorkflow.UserId = newWorkflow.UserId;
                            existingWorkflow.AccountId = newWorkflow.AccountId;
                            existingWorkflow.PortfolioId = newWorkflow.PortfolioId;
                            existingWorkflow.TransactionAmount = newWorkflow.TransactionAmount;
                            existingWorkflow.Step = newWorkflow.Step;
                            existingWorkflow.StopLossPercent = newWorkflow.StopLossPercent;
                            existingWorkflow.ProfitTargetPercent = newWorkflow.ProfitTargetPercent;
                            existingWorkflow.EntryThresholdPercent = newWorkflow.EntryThresholdPercent;
                            existingWorkflow.MaxTotalLoss = newWorkflow.MaxTotalLoss;
                            existingSession.Update(existingWorkflow);
                            _logger.LogInformation("Mevcut strateji güncellendi: {StrategyName}, Cancelled=false", strategyName);
                        }
                    }
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
                    _logger.LogInformation("Strateji kaldırıldı: {StrategyName}", strategyName);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Strateji kaldırılırken hata oluştu: {StrategyName}", strategyName);
            }
        }
        public async Task UpdateStrategyPreferencesAsync(int strategyId, Application.Features.Strategies.Dtos.UserPreference userPreference)
        {
            try
            {
                var strategyKey = $"Strategy_{strategyId}";
                if (_strategySessions.TryGetValue(strategyKey, out var session))
                {
                    userPreference.Ticker = userPreference.Ticker.ToUpper().Trim();
                    string normalizedTicker = userPreference.Ticker;
                    var allPreferences = session.Query<UserPreference>()
                        .Where(pref => pref.StrategyId == strategyId)
                        .ToList();
                    var existingPreferences = allPreferences
                        .Where(pref => pref.Ticker.ToUpper().Trim() == normalizedTicker)
                        .ToList();
                    if (existingPreferences.Any())
                    {
                        var existingPref = existingPreferences.First();
                        existingPref.StopLossPercentage = userPreference.StopLossPercentage;
                        existingPref.TakeProfitPercentage = userPreference.TakeProfitPercentage;
                        existingPref.EntryThresholdPercentage = userPreference.EntryThresholdPercentage;
                        existingPref.MaxLossLimitPercentage = userPreference.MaxLossLimitPercentage;
                        session.Update(existingPref);
                        _logger.LogInformation("UserPreference güncellendi: StrategyId={StrategyId}, Ticker={Ticker}", strategyId, userPreference.Ticker);
                    }
                    else
                    {
                        session.Insert(userPreference);
                        _logger.LogInformation("Yeni UserPreference eklendi: StrategyId={StrategyId}, Ticker={Ticker}", strategyId, userPreference.Ticker);
                    }
                    var allWorkflows = session.Query<StockWorkflow>()
                        .Where(w => w.StrategyId == strategyId)
                        .ToList();
                    var workflows = allWorkflows
                        .Where(w => w.Symbol.ToUpper().Trim() == normalizedTicker)
                        .ToList();
                    var updatedPreference = existingPreferences.Any() ? existingPreferences.First() : userPreference;
                    foreach (var workflow in workflows)
                    {
                        workflow.StopLossPercent = userPreference.StopLossPercentage;
                        workflow.ProfitTargetPercent = userPreference.TakeProfitPercentage;
                        workflow.EntryThresholdPercent = userPreference.EntryThresholdPercentage;
                        workflow.MaxTotalLoss = userPreference.MaxLossLimitPercentage;
                        workflow.UserPreference = updatedPreference;
                        session.Update(workflow);
                    }
                }
                else
                {
                    _logger.LogWarning("Strateji session'ı bulunamadı: {StrategyKey}", strategyKey);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UserPreference güncellenirken hata oluştu: StrategyId={StrategyId}", strategyId);
                throw;
            }
        }
        private string GetCurrentRuleName(int currentStep, int previousStep)
        {
            return currentStep switch
            {
                0 => "TimeCheckRule",
                1 => "PortfolioCheckRule",
                2 => "SellRule",
                3 => "BuyRule",
                -1 => "Completed",
                _ => "Unknown"
            };
        }
        private (string, bool?) GetDecision(StockWorkflow ctx, int previousStep)
        {
            if (ctx.Step == -1)
            {
                return ("SONLANDI", false);
            }
            if (ctx.Step > previousStep)
            {
                return ("EVET", true);
            }
            if (ctx.Step == previousStep && (ctx.Step == 2 || ctx.Step == 3))
            {
                return ("BEKLE", null);
            }
            return ("DEVAM", null);
        }
        private string GetReason(StockWorkflow ctx, string currentRule)
        {
            return currentRule switch
            {
                "TimeCheckRule" => ctx.MarketOpen 
                    ? "Piyasa açık (10:00-17:59) - Portföy kontrolüne geçiliyor" 
                    : "Piyasa kapalı - Strateji sonlandırılıyor",
                "PortfolioCheckRule" => ctx.InPortfolio 
                    ? "Hisse portföyde var - Satış kontrolüne geçiliyor" 
                    : "Hisse portföyde yok - Alım kontrolüne geçiliyor",
                "SellRule" => ctx.BuyPrice.HasValue 
                    ? $"Take Profit/Stop Loss kontrol ediliyor (Alış: ₺{ctx.BuyPrice.Value:F2}, Mevcut: ₺{ctx.CurrentPrice:F2})" 
                    : "Alış fiyatı yok - Satış yapılamıyor",
                "BuyRule" => ctx.OpeningPrice > 0 
                    ? $"Entry fiyatı kontrol ediliyor (Açılış: ₺{ctx.OpeningPrice:F2}, Mevcut: ₺{ctx.CurrentPrice:F2})" 
                    : "Açılış fiyatı yok - Alım yapılamıyor",
                "Completed" => "Strateji başarıyla tamamlandı",
                _ => "İşlem devam ediyor"
            };
        }
        private async Task UpdateContextAsync(ISession session)
        {
            var currentTime = DateTime.Now;
            using var scope = _scopeFactory.CreateScope();
            var marketDataService = scope.ServiceProvider.GetService<IMarketDataService>();
            var facts = session.Query<StockWorkflow>().ToList();
            foreach (var fact in facts)
            {
                try
                {
                    if (marketDataService != null && !string.IsNullOrEmpty(fact.Symbol))
                    {
                        try
                        {
                            var stockInfo = await marketDataService.GetStockInfo(fact.Symbol);
                            if (stockInfo?.Quote != null)
                            {
                                var quote = stockInfo.Quote;
                                if (fact.OpeningPrice <= 0)
                                {
                                    fact.OpeningPrice = quote.OpenPrice;
                                }
                                fact.CurrentPrice = quote.CurrentPrice;
                                fact.HighPrice = quote.HighPrice;
                                fact.LowPrice = quote.LowPrice;
                                fact.PreviousClosePrice = quote.PreviousClosePrice;
                                fact.Change = quote.Change;
                                fact.PercentChange = quote.PercentChange;
                                if (stockInfo.Profile != null)
                                {
                                    fact.CompanyName = stockInfo.Profile.Name;
                                    fact.Exchange = stockInfo.Profile.Exchange;
                                    fact.Industry = stockInfo.Profile.Industry;
                                    fact.Currency = stockInfo.Profile.Currency;
                                }
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
                        await UpdateWithSimulatedData(fact, currentTime);
                    }
                    if (fact.OpeningPrice > 0)
                    {
                        fact.TotalLossPercent = ((fact.CurrentPrice - fact.OpeningPrice) / fact.OpeningPrice) * 100;
                    }
                    fact.Now = currentTime;
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
            var priceChange = (decimal)(random.NextDouble() - 0.5) * 4m;
            fact.CurrentPrice = Math.Max(1, fact.CurrentPrice + priceChange);
            if (fact.OpeningPrice == 0)
            {
                fact.OpeningPrice = fact.CurrentPrice;
            }
        }
        private async Task SaveStrategyEventsAsync(ISession session)
        {
            try
            {
                var workflows = session.Query<StockWorkflow>().ToList();
                _logger.LogDebug("SaveStrategyEventsAsync: {Count} workflow bulundu", workflows.Count);
                foreach (var workflow in workflows)
                {
                    if (workflow.StrategyEvents != null && workflow.StrategyEvents.Any())
                    {
                        _logger.LogInformation("[{Symbol}] StrategyId={StrategyId}, Step={Step} - {Count} adet StrategyEvent bulundu", 
                            workflow.Symbol, workflow.StrategyId, workflow.Step, workflow.StrategyEvents.Count);
                        foreach (var strategyEvent in workflow.StrategyEvents)
                        {
                            try
                            {
                                if (strategyEvent.StrategyId <= 0 && workflow.StrategyId > 0)
                                {
                                    strategyEvent.StrategyId = workflow.StrategyId;
                                    _logger.LogDebug("StrategyEvent StrategyId güncellendi: {OldId} -> {NewId}", 
                                        strategyEvent.StrategyId, workflow.StrategyId);
                                }
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
                        var successfulBuyOrSellEvents = workflow.StrategyEvents
                            .Where(e => e.Action == "BUY" || e.Action == "SELL")
                            .ToList();
                        var marketClosedEvents = workflow.StrategyEvents
                            .Where(e => e.Action == "MARKET_CLOSED")
                            .ToList();
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
                            }
                        }
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
                            }
                        }
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
            var strategy = await strategyRepository.GetAsync(
                s => s.Id == workflow.StrategyId,
                cancellationToken: default);
            if (strategy == null)
            {
                _logger.LogWarning("Strateji bulunamadı, notification gönderilemedi: StrategyId={StrategyId}", 
                    workflow.StrategyId);
                return;
            }
            var allEvents = await strategyEventRepository.GetAllAsync(
                e => e.StrategyId == workflow.StrategyId,
                cancellationToken: default);
            var buyEvent = successfulBuyOrSellEvents.FirstOrDefault(e => e.Action == "BUY");
            var sellEvent = successfulBuyOrSellEvents.FirstOrDefault(e => e.Action == "SELL");
            _logger.LogInformation("📋 Creating StrategyNotificationEvent - Source Data: Strategy.Id={StrategyId}, Strategy.UserId={UserId}, Strategy.StrategyName={StrategyName}, Strategy.StockSymbol={StockSymbol}, Workflow.Symbol={WorkflowSymbol}, Workflow.CurrentPrice={CurrentPrice}", 
                strategy.Id, strategy.UserId, strategy.StrategyName, strategy.StockSymbol, workflow.Symbol, workflow.CurrentPrice);
            string userEmail = null;
            try
            {
                using var userScope = _scopeFactory.CreateScope();
                var userService = userScope.ServiceProvider.GetService<IUserService>();
                if (userService == null)
                {
                    _logger.LogWarning("⚠️ IUserService is not registered in DI container. Using fallback email.");
                }
                else
                {
                    _logger.LogDebug("Attempting to get user email from AuthUserService: UserId={UserId}", strategy.UserId);
                    userEmail = await userService.GetUserEmailByIdAsync(strategy.UserId);
                    if (!string.IsNullOrEmpty(userEmail))
                    {
                        _logger.LogInformation("✅ User Email retrieved from AuthUserService: UserId={UserId}, Email={Email}", 
                            strategy.UserId, userEmail);
                    }
                    else
                    {
                        _logger.LogWarning("⚠️ User email is null or empty from AuthUserService: UserId={UserId}", strategy.UserId);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Failed to get user email from AuthUserService: UserId={UserId}", strategy.UserId);
            }
            if (string.IsNullOrEmpty(userEmail))
            {
                userEmail = $"user{strategy.UserId}@example.com";
                _logger.LogWarning("⚠️ Using fallback email: UserId={UserId}, Email={Email}", strategy.UserId, userEmail);
            }
            var action = buyEvent != null ? "BUY" : "SELL";
            _logger.LogInformation("🎯 Action determined: {Action} (BuyEvent={HasBuyEvent}, SellEvent={HasSellEvent})", 
                action, buyEvent != null, sellEvent != null);
            var strategyNotificationEvent = new StrategyNotificationEvent
            {
                StrategyId = strategy.Id,
                UserId = strategy.UserId,
                UserEmail = userEmail ?? $"user{strategy.UserId}@example.com",
                StrategyName = strategy.StrategyName ?? "Unknown Strategy",
                StockSymbol = strategy.StockSymbol ?? workflow.Symbol ?? "UNKNOWN",
                Status = strategy.Status.ToString(),
                Action = action,
                BuyPrice = buyEvent?.Price ?? strategy.BuyPrice,
                SellPrice = sellEvent?.Price ?? strategy.SellPrice,
                CurrentPrice = workflow.CurrentPrice,
                Timestamp = DateTime.Now,
                ExecutedRules = allEvents
                    .OrderBy(e => e.Step)
                    .ThenBy(e => e.Timestamp)
                    .Select(e => new Domain.Events.RuleExecutionInfo
                    {
                        RuleName = e.RuleName ?? "Unknown",
                        Step = e.Step,
                        Action = e.Action ?? "UNKNOWN",
                        Reason = e.Reason ?? string.Empty,
                        Price = e.Price,
                        Timestamp = e.Timestamp
                    })
                    .ToList()
            };
            _logger.LogInformation("✅ StrategyNotificationEvent created successfully: StrategyId={StrategyId}, UserId={UserId}, UserEmail={UserEmail}, Action={Action}, StrategyName={StrategyName}, StockSymbol={StockSymbol}, Status={Status}, CurrentPrice={CurrentPrice}, Timestamp={Timestamp}", 
                strategyNotificationEvent.StrategyId, 
                strategyNotificationEvent.UserId, 
                strategyNotificationEvent.UserEmail ?? "NULL",
                strategyNotificationEvent.Action ?? "NULL",
                strategyNotificationEvent.StrategyName ?? "NULL",
                strategyNotificationEvent.StockSymbol ?? "NULL",
                strategyNotificationEvent.Status ?? "NULL",
                strategyNotificationEvent.CurrentPrice,
                strategyNotificationEvent.Timestamp);
            if (strategyNotificationEvent.StrategyId == 0)
            {
                _logger.LogError("❌ StrategyNotificationEvent StrategyId is 0! Event will not be sent.");
                throw new InvalidOperationException("StrategyNotificationEvent StrategyId cannot be 0");
            }
            if (strategyNotificationEvent.UserId == 0)
            {
                _logger.LogError("❌ StrategyNotificationEvent UserId is 0! Event will not be sent. StrategyId={StrategyId}", 
                    strategyNotificationEvent.StrategyId);
                throw new InvalidOperationException("StrategyNotificationEvent UserId cannot be 0");
            }
            if (string.IsNullOrEmpty(strategyNotificationEvent.Action))
            {
                _logger.LogError("❌ StrategyNotificationEvent Action is null or empty! Event will not be sent. StrategyId={StrategyId}, UserId={UserId}", 
                    strategyNotificationEvent.StrategyId, strategyNotificationEvent.UserId);
                throw new InvalidOperationException("StrategyNotificationEvent Action cannot be null or empty");
            }
            if (string.IsNullOrEmpty(strategyNotificationEvent.UserEmail))
            {
                _logger.LogWarning("⚠️ StrategyNotificationEvent UserEmail is null or empty. Using fallback. StrategyId={StrategyId}, UserId={UserId}", 
                    strategyNotificationEvent.StrategyId, strategyNotificationEvent.UserId);
                strategyNotificationEvent.UserEmail = $"user{strategyNotificationEvent.UserId}@example.com";
            }
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
            try
            {
                _logger.LogInformation("🚀 Publishing StrategyNotificationEvent to RabbitMQ: StrategyId={StrategyId}, UserId={UserId}, UserEmail={UserEmail}, Action={Action}", 
                    strategyNotificationEvent.StrategyId, 
                    strategyNotificationEvent.UserId, 
                    strategyNotificationEvent.UserEmail,
                    strategyNotificationEvent.Action);
                await rabbitMQPublisher.PublishAsync(strategyNotificationEvent, "strategy-notifications");
                _logger.LogInformation("✅ StrategyNotificationEvent successfully published to RabbitMQ: StrategyId={StrategyId}, UserId={UserId}, Action={Action}", 
                    strategyNotificationEvent.StrategyId, 
                    strategyNotificationEvent.UserId, 
                    strategyNotificationEvent.Action);
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
                throw;
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
            var strategy = await strategyRepository.GetAsync(
                s => s.Id == workflow.StrategyId,
                cancellationToken: default);
            if (strategy == null)
            {
                _logger.LogWarning("Strateji bulunamadı, piyasa kapalı notification gönderilemedi: StrategyId={StrategyId}", 
                    workflow.StrategyId);
                return;
            }
            if (strategy.Status == Domain.Enums.StrategyStatus.Active)
            {
                strategy.Status = Domain.Enums.StrategyStatus.Inactive;
                strategy.IsActive = false;
                strategy.FinishTime = DateTime.Now;
                await strategyRepository.UpdateAsync(strategy);
                var strategyKey = $"Strategy_{strategy.Id}";
                await RemoveStrategyAsync(strategyKey);
                _logger.LogInformation("Piyasa kapalı olduğu için strateji durduruldu: StrategyId={StrategyId}, StrategyName={StrategyName}", 
                    strategy.Id, strategy.StrategyName);
            }
            _logger.LogInformation("📋 Creating MarketClosed StrategyNotificationEvent - Source Data: Strategy.Id={StrategyId}, Strategy.UserId={UserId}, Strategy.StrategyName={StrategyName}, Strategy.StockSymbol={StockSymbol}, Workflow.Symbol={WorkflowSymbol}, Workflow.CurrentPrice={CurrentPrice}", 
                strategy.Id, strategy.UserId, strategy.StrategyName, strategy.StockSymbol, workflow.Symbol, workflow.CurrentPrice);
            string userEmail = null;
            try
            {
                using var userScope = _scopeFactory.CreateScope();
                var userService = userScope.ServiceProvider.GetService<IUserService>();
                if (userService == null)
                {
                    _logger.LogWarning("⚠️ IUserService is not registered in DI container. Using fallback email.");
                }
                else
                {
                    _logger.LogDebug("Attempting to get user email from AuthUserService: UserId={UserId}", strategy.UserId);
                    userEmail = await userService.GetUserEmailByIdAsync(strategy.UserId);
                    if (!string.IsNullOrEmpty(userEmail))
                    {
                        _logger.LogInformation("✅ User Email retrieved from AuthUserService: UserId={UserId}, Email={Email}", 
                            strategy.UserId, userEmail);
                    }
                    else
                    {
                        _logger.LogWarning("⚠️ User email is null or empty from AuthUserService: UserId={UserId}", strategy.UserId);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Failed to get user email from AuthUserService: UserId={UserId}", strategy.UserId);
            }
            if (string.IsNullOrEmpty(userEmail))
            {
                userEmail = $"user{strategy.UserId}@example.com";
                _logger.LogWarning("⚠️ Using fallback email: UserId={UserId}, Email={Email}", strategy.UserId, userEmail);
            }
            var strategyNotificationEvent = new StrategyNotificationEvent
            {
                StrategyId = strategy.Id,
                UserId = strategy.UserId,
                UserEmail = userEmail ?? $"user{strategy.UserId}@example.com",
                StrategyName = strategy.StrategyName ?? "Unknown Strategy",
                StockSymbol = workflow.Symbol ?? strategy.StockSymbol ?? "UNKNOWN",
                Status = strategy.Status.ToString(),
                Action = "MARKET_CLOSED",
                CurrentPrice = workflow.CurrentPrice,
                Timestamp = DateTime.Now,
                Reason = marketClosedEvent.Reason ?? "Piyasa kapalı - Strateji gerçekleştirilemedi. Piyasa saatleri: 10:00-17:59",
                ExecutedRules = new List<Domain.Events.RuleExecutionInfo>
                {
                    new Domain.Events.RuleExecutionInfo
                    {
                        RuleName = marketClosedEvent.RuleName ?? "TimeCheckRule",
                        Step = marketClosedEvent.Step,
                        Action = marketClosedEvent.Action ?? "MARKET_CLOSED",
                        Reason = marketClosedEvent.Reason ?? string.Empty,
                        Price = marketClosedEvent.Price,
                        Timestamp = marketClosedEvent.Timestamp
                    }
                }
            };
            _logger.LogInformation("✅ MarketClosed StrategyNotificationEvent created successfully: StrategyId={StrategyId}, UserId={UserId}, UserEmail={UserEmail}, Action={Action}, StrategyName={StrategyName}, StockSymbol={StockSymbol}, Status={Status}, CurrentPrice={CurrentPrice}, Timestamp={Timestamp}", 
                strategyNotificationEvent.StrategyId, 
                strategyNotificationEvent.UserId, 
                strategyNotificationEvent.UserEmail ?? "NULL",
                strategyNotificationEvent.Action ?? "NULL",
                strategyNotificationEvent.StrategyName ?? "NULL",
                strategyNotificationEvent.StockSymbol ?? "NULL",
                strategyNotificationEvent.Status ?? "NULL",
                strategyNotificationEvent.CurrentPrice,
                strategyNotificationEvent.Timestamp);
            if (strategyNotificationEvent.StrategyId == 0)
            {
                _logger.LogError("❌ MarketClosed StrategyNotificationEvent StrategyId is 0! Event will not be sent.");
                throw new InvalidOperationException("StrategyNotificationEvent StrategyId cannot be 0");
            }
            if (strategyNotificationEvent.UserId == 0)
            {
                _logger.LogError("❌ MarketClosed StrategyNotificationEvent UserId is 0! Event will not be sent. StrategyId={StrategyId}", 
                    strategyNotificationEvent.StrategyId);
                throw new InvalidOperationException("StrategyNotificationEvent UserId cannot be 0");
            }
            if (string.IsNullOrEmpty(strategyNotificationEvent.Action))
            {
                _logger.LogError("❌ MarketClosed StrategyNotificationEvent Action is null or empty! Event will not be sent. StrategyId={StrategyId}, UserId={UserId}", 
                    strategyNotificationEvent.StrategyId, strategyNotificationEvent.UserId);
                throw new InvalidOperationException("StrategyNotificationEvent Action cannot be null or empty");
            }
            if (string.IsNullOrEmpty(strategyNotificationEvent.UserEmail))
            {
                _logger.LogWarning("⚠️ MarketClosed StrategyNotificationEvent UserEmail is null or empty. Using fallback. StrategyId={StrategyId}, UserId={UserId}", 
                    strategyNotificationEvent.StrategyId, strategyNotificationEvent.UserId);
                strategyNotificationEvent.UserEmail = $"user{strategyNotificationEvent.UserId}@example.com";
            }
            try
            {
                _logger.LogInformation("🚀 Publishing MarketClosed StrategyNotificationEvent to RabbitMQ: StrategyId={StrategyId}, UserId={UserId}, UserEmail={UserEmail}, Action={Action}", 
                    strategyNotificationEvent.StrategyId, 
                    strategyNotificationEvent.UserId, 
                    strategyNotificationEvent.UserEmail,
                    strategyNotificationEvent.Action);
                await rabbitMQPublisher.PublishAsync(strategyNotificationEvent, "strategy-notifications");
                _logger.LogInformation("✅ MarketClosed StrategyNotificationEvent successfully published to RabbitMQ: StrategyId={StrategyId}, UserId={UserId}, Action={Action}", 
                    strategyNotificationEvent.StrategyId, 
                    strategyNotificationEvent.UserId, 
                    strategyNotificationEvent.Action);
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
                throw;
            }
        }
    }
}
