using MarketDataService.Dtos;
using MarketDataService.Helpers;

namespace MarketDataService.Services.Impl
{
    public class FinancialMetricsService : IFinancialMetricsService
    {
        private readonly FinnhubClient _client;
        public FinancialMetricsService(FinnhubClient client) => _client = client;

        public async Task<FinancialMetricsDto> GetMetricsAsync(string ticker)
        {
            return await _client.GetFinancialMetricsAsync(ticker);
        }
    }
}
