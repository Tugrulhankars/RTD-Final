using Domain.Enums;
using Domain.Events;
using Infrastructure.Services.RabbitMQ;
using Infrastructure.Services.Grpc.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Data.SqlClient;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
namespace Application.Services;
public class StrategyProcessingHostedService : BackgroundService
{
    private readonly INRulesService _rulesService;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<StrategyProcessingHostedService> _logger;
    private readonly TimeSpan _interval;
    public StrategyProcessingHostedService(
        INRulesService rulesService,
        IServiceProvider serviceProvider,
        ILogger<StrategyProcessingHostedService> logger,
        IConfiguration configuration)
    {
        _rulesService = rulesService;
        _serviceProvider = serviceProvider;
        _logger = logger;
        var seconds = configuration.GetValue<int?>("StrategyProcessing:IntervalSeconds") ?? 5;
        if (seconds < 1) seconds = 1;
        _interval = TimeSpan.FromSeconds(seconds);
    }
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("StrategyProcessingHostedService started. Interval={Interval}s", _interval.TotalSeconds);
        await LoadActiveStrategiesFromDatabase(stoppingToken);
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await _rulesService.ProcessRulesAsync();
                await CheckAndExpireStrategies(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Strategy processing loop failure");
            }
            try
            {
                await Task.Delay(_interval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
        _logger.LogInformation("StrategyProcessingHostedService stopped.");
    }
    private async Task LoadActiveStrategiesFromDatabase(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var strategyRepository = scope.ServiceProvider.GetRequiredService<IStrategyRepository>();
            var nRulesService = scope.ServiceProvider.GetRequiredService<INRulesService>();
            var now = DateTime.Now;
            var activeStrategies = await strategyRepository.GetAllAsync(
                predicate: s => s.IsActive && 
                               s.Status == StrategyStatus.Active &&
                               (!s.ExpiryDate.HasValue || s.ExpiryDate.Value > now),
                cancellationToken: cancellationToken);
            if (activeStrategies == null || !activeStrategies.Any())
            {
                _logger.LogInformation("Veritabanında aktif strateji bulunamadı");
                return;
            }
            _logger.LogInformation("Veritabanından {Count} aktif strateji bulundu, yükleniyor...", activeStrategies.Count());
            foreach (var strategy in activeStrategies)
            {
                try
                {
                    var strategyContext = new Application.Features.Strategies.Dtos.StockWorkflow
                    {
                        StrategyId = strategy.Id,
                        UserId = strategy.UserId,
                        Symbol = strategy.StockSymbol.ToUpper().Trim(),
                        OpeningPrice = 0,
                        CurrentPrice = 0,
                        InPortfolio = false,
                        TotalLossPercent = strategy.MaxTotalLoss,
                        StopLossPercent = strategy.StopLossPercent,
                        ProfitTargetPercent = strategy.ProfitTargetPercent,
                        EntryThresholdPercent = strategy.EntryThresholdPercentage,
                        MaxTotalLoss = strategy.MaxTotalLoss,
                        Now = DateTime.Now,
                        TransactionAmount = strategy.TransactionAmount,
                        AccountId = strategy.AccountId ?? 0,
                        PortfolioId = strategy.PortfolioId ?? 0,
                        Step = strategy.CurrentStep ?? 0
                    };
                    var strategyKey = $"Strategy_{strategy.Id}";
                    await nRulesService.AddStrategyAsync(strategyKey, strategyContext);
                    _logger.LogInformation("Aktif strateji yüklendi: StrategyId={StrategyId}, StrategyName={StrategyName}, CurrentStep={CurrentStep}", 
                        strategy.Id, strategy.StrategyName, strategy.CurrentStep);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Strateji yüklenirken hata oluştu: StrategyId={StrategyId}", strategy.Id);
                }
            }
            _logger.LogInformation("Aktif stratejiler yükleme tamamlandı");
        }
        catch (Exception ex)
        {
            // Veritabanı bağlantı hatası durumunda sadece debug seviyesinde log
            if (ex is SqlException || 
                (ex.InnerException is SqlException) ||
                ex.Message.Contains("network-related", StringComparison.OrdinalIgnoreCase) ||
                ex.Message.Contains("instance-specific", StringComparison.OrdinalIgnoreCase) ||
                ex.Message.Contains("server was not found", StringComparison.OrdinalIgnoreCase) ||
                ex.Message.Contains("could not open a connection", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogDebug("Veritabanı bağlantı hatası nedeniyle aktif stratejiler yüklenemedi. Servis veritabanı olmadan devam edecek.");
            }
            else
            {
                _logger.LogError(ex, "Aktif stratejiler yüklenirken hata oluştu");
            }
        }
    }
    private async Task CheckAndExpireStrategies(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var strategyRepository = scope.ServiceProvider.GetRequiredService<IStrategyRepository>();
            var strategyEventRepository = scope.ServiceProvider.GetRequiredService<IStrategyEventRepository>();
            var rabbitMQPublisher = scope.ServiceProvider.GetRequiredService<IRabbitMQPublisher>();
            var userService = scope.ServiceProvider.GetService<IUserService>();
            var now = DateTime.Now;
            var expiredStrategies = await strategyRepository.GetAllAsync(
                predicate: s => s.ExpiryDate.HasValue && 
                               s.ExpiryDate.Value < now && 
                               s.Status == StrategyStatus.Active,
                cancellationToken: cancellationToken);
            if (expiredStrategies != null && expiredStrategies.Any())
            {
                _logger.LogInformation("Süresi dolmuş {Count} strateji bulundu", expiredStrategies.Count());
                foreach (var strategy in expiredStrategies)
                {
                    try
                    {
                        _logger.LogInformation(
                            "Süresi dolmuş strateji işleniyor: StrategyId={StrategyId}, StrategyName={StrategyName}, ExpiryDate={ExpiryDate}",
                            strategy.Id, strategy.StrategyName, strategy.ExpiryDate);
                        var strategyEvents = await strategyEventRepository.GetAllAsync(
                            predicate: e => e.StrategyId == strategy.Id,
                            orderBy: q => q.OrderBy(e => e.Timestamp),
                            cancellationToken: cancellationToken) ?? new List<Domain.Entities.StrategyEvent>();
                        _logger.LogInformation(
                            "Strateji event'leri alındı: StrategyId={StrategyId}, EventCount={EventCount}",
                            strategy.Id, strategyEvents.Count);
                        string? userEmail = null;
                        if (userService != null)
                        {
                            try
                            {
                                _logger.LogDebug("Kullanıcı email'i alınıyor: UserId={UserId}", strategy.UserId);
                                userEmail = await userService.GetUserEmailByIdAsync(strategy.UserId);
                                if (!string.IsNullOrEmpty(userEmail))
                                {
                                    _logger.LogInformation("Kullanıcı email'i alındı: UserId={UserId}, Email={Email}", strategy.UserId, userEmail);
                                }
                                else
                                {
                                    _logger.LogWarning("Kullanıcı email'i alınamadı (null veya boş): UserId={UserId}", strategy.UserId);
                                }
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "Kullanıcı email'i alınırken hata: UserId={UserId}", strategy.UserId);
                            }
                        }
                        else
                        {
                            _logger.LogWarning("IUserService bulunamadı, email alınamıyor: StrategyId={StrategyId}", strategy.Id);
                        }
                        if (string.IsNullOrEmpty(userEmail))
                        {
                            userEmail = $"user{strategy.UserId}@example.com";
                            _logger.LogWarning("Fallback email kullanılıyor: UserId={UserId}, Email={Email}", strategy.UserId, userEmail);
                        }
                        var executedRules = strategyEvents.Select(e => new RuleExecutionInfo
                        {
                            RuleName = e.RuleName ?? "Unknown",
                            Step = e.Step,
                            Action = e.Action ?? "Unknown",
                            Reason = e.Reason ?? string.Empty,
                            Price = e.Price,
                            Timestamp = e.Timestamp
                        }).ToList();
                        _logger.LogInformation(
                            "ExecutedRules oluşturuldu: StrategyId={StrategyId}, RuleCount={RuleCount}",
                            strategy.Id, executedRules.Count);
                        var notificationEvent = new StrategyNotificationEvent
                        {
                            StrategyId = strategy.Id,
                            UserId = strategy.UserId,
                            UserEmail = userEmail,
                            StrategyName = strategy.StrategyName ?? "Bilinmeyen Strateji",
                            StockSymbol = strategy.StockSymbol ?? "N/A",
                            Status = "Inactive",
                            Action = "STRATEGY_EXPIRED",
                            BuyPrice = strategy.BuyPrice,
                            SellPrice = strategy.SellPrice,
                            ProfitLoss = strategy.ProfitLoss,
                            CurrentPrice = 0,
                            Timestamp = DateTime.Now,
                            Reason = $"Strateji süresi doldu. Başlangıç: {strategy.StartDate:dd.MM.yyyy HH:mm}, Bitiş: {strategy.ExpiryDate:dd.MM.yyyy HH:mm}. " +
                                     $"Toplam {strategyEvents.Count} adım gerçekleştirildi.",
                            ExecutedRules = executedRules
                        };
                        _logger.LogInformation(
                            "NotificationEvent oluşturuldu: StrategyId={StrategyId}, UserId={UserId}, Email={Email}, Action={Action}",
                            strategy.Id, strategy.UserId, userEmail, notificationEvent.Action);
                        try
                        {
                            _logger.LogInformation(
                                "🚀 RabbitMQ'ya strateji süresi doldu bildirimi gönderiliyor: StrategyId={StrategyId}, UserId={UserId}, Email={Email}, Action={Action}, Queue=strategy-notifications",
                                strategy.Id, strategy.UserId, userEmail, notificationEvent.Action);
                            await rabbitMQPublisher.PublishAsync(notificationEvent, "strategy-notifications");
                            _logger.LogInformation(
                                "✅ Strateji süresi doldu bildirimi başarıyla RabbitMQ'ya gönderildi: StrategyId={StrategyId}, UserId={UserId}, Email={Email}, Action={Action}, EventCount={EventCount}",
                                strategy.Id, strategy.UserId, userEmail, notificationEvent.Action, strategyEvents.Count);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, 
                                "❌ RabbitMQ'ya strateji süresi doldu bildirimi gönderilirken hata: StrategyId={StrategyId}, UserId={UserId}, Email={Email}, Action={Action}, Error={Error}",
                                strategy.Id, strategy.UserId, userEmail, notificationEvent.Action, ex.Message);
                        }
                        strategy.Status = StrategyStatus.Inactive;
                        strategy.IsActive = false;
                        await strategyRepository.UpdateAsync(strategy, cancellationToken);
                        _logger.LogInformation(
                            "✅ Strateji süresi doldu ve pasif yapıldı: StrategyId={StrategyId}, StrategyName={StrategyName}, ExpiryDate={ExpiryDate}, IsActive=false",
                            strategy.Id, strategy.StrategyName, strategy.ExpiryDate);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "❌ Strateji süresi doldu işlemi sırasında hata: StrategyId={StrategyId}, Error={Error}", 
                            strategy.Id, ex.Message);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            // Veritabanı bağlantı hatası durumunda sadece debug seviyesinde log
            if (ex is SqlException || 
                (ex.InnerException is SqlException) ||
                ex.Message.Contains("network-related", StringComparison.OrdinalIgnoreCase) ||
                ex.Message.Contains("instance-specific", StringComparison.OrdinalIgnoreCase) ||
                ex.Message.Contains("server was not found", StringComparison.OrdinalIgnoreCase) ||
                ex.Message.Contains("could not open a connection", StringComparison.OrdinalIgnoreCase))
            {
                // Veritabanı bağlantı hatası durumunda sessizce devam et (log spam'ini önle)
                // Sadece ilk birkaç hatada debug log, sonrasında sessizce devam et
            }
            else
            {
                _logger.LogError(ex, "Süresi dolan stratejiler kontrol edilirken hata oluştu");
            }
        }
    }
}
