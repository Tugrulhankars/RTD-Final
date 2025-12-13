using MarketDataService.Dtos;

namespace MarketDataService.Services;

public interface IFinancialMetricsService
{
    Task<FinancialMetricsDto> GetMetricsAsync(string ticker);

}
