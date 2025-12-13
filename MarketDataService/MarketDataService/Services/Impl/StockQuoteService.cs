using MarketDataService.Dtos;
using MarketDataService.Helpers;

namespace MarketDataService.Services.Impl
{
    public class StockQuoteService : IStockQuoteService
    {
        private readonly FinnhubClient _client;
        public StockQuoteService(FinnhubClient client) => _client = client;
        public async Task<StockQuoteDto> GetQuoteAsync(string ticker) => await _client.GetQuoteAsync(ticker);
    }
}
