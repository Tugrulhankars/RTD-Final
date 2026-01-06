using Infrastructure.Services.Grpc.Dtos;
using Infrastructure.Services.Grpc.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace Api.Controllers;
[Route("api/marketdata")]
[ApiController]
[AllowAnonymous]
public class MarketDataController : ControllerBase
{
    private readonly IMarketDataService _marketDataService;
    private readonly ILogger<MarketDataController> _logger;
    public MarketDataController(
        IMarketDataService marketDataService,
        ILogger<MarketDataController> logger)
    {
        _marketDataService = marketDataService;
        _logger = logger;
    }
    [HttpGet("stockinfo/{symbol}")]
    public async Task<IActionResult> GetStockInfo(string symbol)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(symbol))
            {
                return BadRequest(new { Success = false, Message = "Hisse senedi sembolü boş olamaz." });
            }
            _logger.LogInformation("Hisse senedi bilgisi isteniyor: Symbol={Symbol}", symbol);
            var stockInfo = await _marketDataService.GetStockInfo(symbol.ToUpper());
            if (stockInfo == null)
            {
                _logger.LogWarning("Hisse senedi bilgisi bulunamadı: Symbol={Symbol}", symbol);
                return NotFound(new { Success = false, Message = $"Hisse senedi bilgisi bulunamadı: {symbol}" });
            }
            _logger.LogInformation("Hisse senedi bilgisi başarıyla alındı: Symbol={Symbol}, CurrentPrice={CurrentPrice}", 
                symbol, stockInfo.Quote?.CurrentPrice);
            return Ok(new
            {
                Success = true,
                Data = new
                {
                    Quote = stockInfo.Quote != null ? new
                    {
                        Ticker = stockInfo.Quote.Ticker,
                        CurrentPrice = stockInfo.Quote.CurrentPrice,
                        OpenPrice = stockInfo.Quote.OpenPrice,
                        HighPrice = stockInfo.Quote.HighPrice,
                        LowPrice = stockInfo.Quote.LowPrice,
                        PreviousClosePrice = stockInfo.Quote.PreviousClosePrice,
                        Change = stockInfo.Quote.Change,
                        PercentChange = stockInfo.Quote.PercentChange,
                        Timestamp = stockInfo.Quote.Timestamp
                    } : null,
                    Profile = stockInfo.Profile != null ? new
                    {
                        Ticker = stockInfo.Profile.Ticker,
                        Name = stockInfo.Profile.Name,
                        Exchange = stockInfo.Profile.Exchange,
                        Industry = stockInfo.Profile.Industry,
                        Ipo = stockInfo.Profile.Ipo,
                        Currency = stockInfo.Profile.Currency
                    } : null,
                    Metrics = stockInfo.Metrics != null ? new
                    {
                        Pe = stockInfo.Metrics.Pe,
                        Pb = stockInfo.Metrics.Pb,
                        Roe = stockInfo.Metrics.Roe,
                        NetMargin = stockInfo.Metrics.NetMargin,
                        DebtEquity = stockInfo.Metrics.DebtEquity
                    } : null
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Hisse senedi bilgisi alınırken hata oluştu: Symbol={Symbol}", symbol);
            return StatusCode(500, new
            {
                Success = false,
                Message = "Hisse senedi bilgisi alınırken bir hata oluştu.",
                Error = ex.Message
            });
        }
    }
    [HttpGet("price/{symbol}")]
    public async Task<IActionResult> GetCurrentPrice(string symbol)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(symbol))
            {
                return BadRequest(new { Success = false, Message = "Hisse senedi sembolü boş olamaz." });
            }
            var price = await _marketDataService.GetStockCurrentPrice(symbol.ToUpper());
            return Ok(new
            {
                Success = true,
                Symbol = symbol.ToUpper(),
                CurrentPrice = price
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Güncel fiyat alınırken hata oluştu: Symbol={Symbol}", symbol);
            return StatusCode(500, new
            {
                Success = false,
                Message = "Güncel fiyat alınırken bir hata oluştu.",
                Error = ex.Message
            });
        }
    }
    [HttpGet("open/{symbol}")]
    public async Task<IActionResult> GetOpeningPrice(string symbol)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(symbol))
            {
                return BadRequest(new { Success = false, Message = "Hisse senedi sembolü boş olamaz." });
            }
            var price = await _marketDataService.GetStockOpeningPrice(symbol.ToUpper());
            return Ok(new
            {
                Success = true,
                Symbol = symbol.ToUpper(),
                OpeningPrice = price
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Açılış fiyatı alınırken hata oluştu: Symbol={Symbol}", symbol);
            return StatusCode(500, new
            {
                Success = false,
                Message = "Açılış fiyatı alınırken bir hata oluştu.",
                Error = ex.Message
            });
        }
    }
}
