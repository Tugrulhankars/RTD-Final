using Microsoft.AspNetCore.Mvc;
using MarketDataService.Services;

namespace MarketDataService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TestController : ControllerBase
{
    private readonly IStockQuoteService _stockQuoteService;
    private readonly ILogger<TestController> _logger;

    public TestController(IStockQuoteService stockQuoteService, ILogger<TestController> logger)
    {
        _stockQuoteService = stockQuoteService;
        _logger = logger;
    }

    [HttpGet("health")]
    public ActionResult<string> HealthCheck()
    {
        return Ok("MarketDataService is running!");
    }

    [HttpGet("quote/{ticker}")]
    public async Task<ActionResult> TestQuote(string ticker)
    {
        try
        {
            var quote = await _stockQuoteService.GetQuoteAsync(ticker);
            return Ok(new
            {
                Success = true,
                Ticker = ticker,
                Quote = quote,
                Message = "Hisse senedi verisi başarıyla alındı"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Hisse senedi verisi alma hatası: {Ticker}", ticker);
            return StatusCode(500, new
            {
                Success = false,
                Ticker = ticker,
                Error = ex.Message,
                Message = "Hisse senedi verisi alınamadı"
            });
        }
    }

    [HttpGet("websocket/{ticker}")]
    public ActionResult TestWebSocket(string ticker)
    {
        return Ok(new
        {
            Success = true,
            Ticker = ticker,
            WebSocketUrl = $"ws://localhost:5275/ws/marketdata/{ticker}",
            Message = "WebSocket endpoint'i hazır"
        });
    }
}
