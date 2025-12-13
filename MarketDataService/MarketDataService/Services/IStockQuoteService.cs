using MarketDataService.Dtos;

namespace MarketDataService.Services;

public interface IStockQuoteService
{
    Task<StockQuoteDto> GetQuoteAsync(string ticker);

}
