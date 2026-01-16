using MarketDataService.Dtos;
using MarketDataService.Services;
using Microsoft.AspNetCore.Mvc;

namespace MarketDataService.Controllers;

[ApiController]
[Route("api/quotes")]
public class StockQuoteController : ControllerBase
{
    private readonly IStockQuoteService _service;
    public StockQuoteController(IStockQuoteService service) => _service = service;

    [HttpGet("{ticker}")]
    public async Task<StockQuoteDto> Get(string ticker) => await _service.GetQuoteAsync(ticker);

    [HttpGet("{ticker}/current")]
    public async Task<decimal> GetCurrent(string ticker)
    {
        var q = await _service.GetQuoteAsync(ticker);
        return q.CurrentPrice;
    }

    [HttpGet("{ticker}/open")]
    public async Task<decimal> GetOpen(string ticker)
    {
        var q = await _service.GetQuoteAsync(ticker);
        return q.OpenPrice;
    }

    [HttpGet("{ticker}/close")]
    public async Task<decimal> GetClose(string ticker)
    {
        var q = await _service.GetQuoteAsync(ticker);
        return q.PreviousClosePrice;
    }
}





