using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Application.Services;

public class StrategyProcessingHostedService : BackgroundService
{
    private readonly INRulesService _rulesService;
    private readonly ILogger<StrategyProcessingHostedService> _logger;
    private readonly TimeSpan _interval;

    public StrategyProcessingHostedService(
        INRulesService rulesService,
        ILogger<StrategyProcessingHostedService> logger,
        IConfiguration configuration)
    {
        _rulesService = rulesService;
        _logger = logger;

        var seconds = configuration.GetValue<int?>("StrategyProcessing:IntervalSeconds") ?? 5;
        if (seconds < 1) seconds = 1;
        _interval = TimeSpan.FromSeconds(seconds);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("StrategyProcessingHostedService started. Interval={Interval}s", _interval.TotalSeconds);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await _rulesService.ProcessRulesAsync();
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
}

