using Application.Services;
using Application.Features.Strategies.Rules;
using Application.Features.Strategies.Dtos;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.Services.Grpc.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
namespace StrategyRuleService.Worker
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly List<Strategy> _activeStrategies;
        private readonly SemaphoreSlim _semaphore;
        public Worker(ILogger<Worker> logger, IServiceProvider serviceProvider)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
            _activeStrategies = new List<Strategy>();
            _semaphore = new SemaphoreSlim(1, 1);
        }
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("NRules Worker başlatıldı - Kurallar sürekli çalışacak");
            await LoadActiveStrategiesFromDatabaseAsync(stoppingToken);
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await _semaphore.WaitAsync(stoppingToken);
                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var nRulesService = scope.ServiceProvider.GetRequiredService<INRulesService>();
                        await nRulesService.ProcessRulesAsync();
                        _logger.LogDebug("Kurallar işlendi - {Time}", DateTime.Now);
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Worker döngüsünde hata oluştu");
                }
                finally
                {
                    _semaphore.Release();
                }
                await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
            }
            _logger.LogInformation("NRules Worker durduruldu");
        }
        private async Task LoadActiveStrategiesFromDatabaseAsync(CancellationToken cancellationToken)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var strategyRepository = scope.ServiceProvider.GetRequiredService<IStrategyRepository>();
                var nRulesService = scope.ServiceProvider.GetRequiredService<INRulesService>();
                var marketDataService = scope.ServiceProvider.GetService<IMarketDataService>();
                await ExpireOldDailyStrategiesAsync(scope, cancellationToken);
                var today = DateTime.Today;
                var tomorrow = today.AddDays(1);
                var activeStrategies = await strategyRepository.GetAllAsync(
                    predicate: s => s.Status == StrategyStatus.Active
                                    && s.FinishTime == null
                                    && s.StartDate >= today
                                    && s.StartDate < tomorrow,
                    cancellationToken: cancellationToken);
                _logger.LogInformation("Veritabanından {Count} adet aktif strateji bulundu", activeStrategies.Count);
                foreach (var strategy in activeStrategies)
                {
                    try
                    {
                        decimal openingPrice = 0;
                        decimal currentPrice = 0;
                        decimal highPrice = 0;
                        decimal lowPrice = 0;
                        decimal previousClosePrice = 0;
                        decimal change = 0;
                        decimal percentChange = 0;
                        string companyName = null;
                        string currency = null;
                        if (marketDataService != null && !string.IsNullOrEmpty(strategy.StockSymbol))
                        {
                            try
                            {
                                var stockInfo = await marketDataService.GetStockInfo(strategy.StockSymbol);
                                if (stockInfo?.Quote != null)
                                {
                                    openingPrice = stockInfo.Quote.OpenPrice;
                                    currentPrice = stockInfo.Quote.CurrentPrice;
                                    highPrice = stockInfo.Quote.HighPrice;
                                    lowPrice = stockInfo.Quote.LowPrice;
                                    previousClosePrice = stockInfo.Quote.PreviousClosePrice;
                                    change = stockInfo.Quote.Change;
                                    percentChange = stockInfo.Quote.PercentChange;
                                }
                                if (stockInfo?.Profile != null)
                                {
                                    companyName = stockInfo.Profile.Name;
                                    currency = stockInfo.Profile.Currency;
                                }
                            }
                            catch (Exception ex)
                            {
                                _logger.LogWarning(ex, "MarketDataService'den fiyat bilgisi alınamadı: {Symbol}", strategy.StockSymbol);
                                try
                                {
                                    openingPrice = (decimal)await marketDataService.GetStockOpeningPrice(strategy.StockSymbol);
                                    currentPrice = (decimal)await marketDataService.GetStockCurrentPrice(strategy.StockSymbol);
                                }
                                catch { }
                            }
                        }
                        var strategyContext = new StockWorkflow
                        {
                            StrategyId = strategy.Id,
                            UserId = strategy.UserId,
                            Symbol = strategy.StockSymbol,
                            OpeningPrice = openingPrice,
                            CurrentPrice = currentPrice,
                            HighPrice = highPrice,
                            LowPrice = lowPrice,
                            PreviousClosePrice = previousClosePrice,
                            Change = change,
                            PercentChange = percentChange,
                            CompanyName = companyName,
                            Currency = currency,
                            InPortfolio = strategy.IsPositionOpen,
                            TotalLossPercent = strategy.StopLossPercent,
                            StopLossPercent = strategy.StopLossPercent,
                            ProfitTargetPercent = strategy.ProfitTargetPercent,
                            MaxTotalLoss = strategy.MaxTotalLoss,
                            BuyPrice = strategy.BuyPrice,
                            Now = DateTime.Now,
                            TransactionAmount = strategy.TransactionAmount,
                            AccountId = 0,
                            PortfolioId = 0,
                            Step = 0
                        };
                        var strategyKey = $"Strategy_{strategy.Id}";
                        await nRulesService.AddStrategyAsync(strategyKey, strategyContext);
                        _logger.LogInformation("Aktif strateji yüklendi: StrategyId={StrategyId}, Name={StrategyName}, Symbol={Symbol}", 
                            strategy.Id, strategy.StrategyName, strategy.StockSymbol);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Strateji yüklenirken hata oluştu: StrategyId={StrategyId}, Name={StrategyName}", 
                            strategy.Id, strategy.StrategyName);
                    }
                }
                _logger.LogInformation("Tüm aktif stratejiler yüklendi");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Aktif stratejiler veritabanından yüklenirken hata oluştu");
            }
        }
        private static async Task ExpireOldDailyStrategiesAsync(IServiceScope scope, CancellationToken cancellationToken)
        {
            var strategyRepository = scope.ServiceProvider.GetRequiredService<IStrategyRepository>();
            var strategyEventRepository = scope.ServiceProvider.GetRequiredService<IStrategyEventRepository>();
            var today = DateTime.Today;
            var oldStrategies = await strategyRepository.GetAllAsync(
                s => s.Status == StrategyStatus.Active
                     && s.FinishTime == null
                     && s.StartDate < today,
                cancellationToken: cancellationToken);
            foreach (var strategy in oldStrategies)
            {
                bool hasTrade = await strategyEventRepository.AnyAsync(
                    e => e.StrategyId == strategy.Id
                         && (e.Action == "BUY"
                             || e.Action == "SELL"
                             || e.Action == "BUY_SIMULATED"
                             || e.Action == "SELL_SIMULATED"),
                    cancellationToken);
                strategy.Status = StrategyStatus.Inactive;
                strategy.FinishTime = DateTime.Now;
                await strategyRepository.UpdateAsync(strategy, cancellationToken);
                var reason = hasTrade
                    ? "Günlük strateji süresi doldu."
                    : "Günlük strateji süresi doldu, gün içinde hiç alım/satım yapılmadı.";
                var expiredEvent = new StrategyEvent
                {
                    StrategyId = strategy.Id,
                    Step = -1,
                    RuleName = "DailyExpiration",
                    Action = hasTrade ? "EXPIRED" : "EXPIRED_NO_TRADE",
                    Reason = reason,
                    Price = 0,
                    Timestamp = DateTime.Now
                };
                await strategyEventRepository.AddAsync(expiredEvent, cancellationToken);
            }
        }
        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Strateji Worker durduruluyor...");
            await _semaphore.WaitAsync();
            try
            {
                _activeStrategies.Clear();
            }
            finally
            {
                _semaphore.Release();
            }
            _semaphore.Dispose();
            await base.StopAsync(cancellationToken);
        }
    }
}
