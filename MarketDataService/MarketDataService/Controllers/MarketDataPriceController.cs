using MarketDataService.Dtos;
using MarketDataService.Services;
using Microsoft.AspNetCore.Mvc;

namespace MarketDataService.Controllers;

[ApiController]
[Route("api/marketdata")]
public class MarketDataPriceController : ControllerBase
{
    private readonly IStockQuoteService _quoteService;
    private readonly IMarketDataService _marketDataService;
    private readonly ICompanyProfileService _profileService;
    private readonly IFinancialMetricsService _metricsService;

    public MarketDataPriceController(
        IStockQuoteService quoteService, 
        IMarketDataService marketDataService,
        ICompanyProfileService profileService,
        IFinancialMetricsService metricsService)
    {
        _quoteService = quoteService;
        _marketDataService = marketDataService;
        _profileService = profileService;
        _metricsService = metricsService;
    }

    [HttpGet("price/{ticker}")]
    public async Task<ActionResult<object>> GetPrice(string ticker)
    {
        try
        {
            var quote = await _quoteService.GetQuoteAsync(ticker);
            return Ok(new { success = true, currentPrice = quote.CurrentPrice });
        }
        catch (Exception ex)
        {
            return NotFound(new { success = false, message = $"Hisse senedi bilgisi bulunamadı: {ticker}. Sembol doğru mu kontrol edin.", error = ex.Message });
        }
    }

    [HttpGet("quote/{ticker}")]
    public async Task<ActionResult<StockQuoteDto>> GetQuote(string ticker)
    {
        try
        {
            var quote = await _quoteService.GetQuoteAsync(ticker);
            return Ok(quote);
        }
        catch (Exception ex)
        {
            return NotFound(new { message = $"Hisse senedi bilgisi bulunamadı: {ticker}. Sembol doğru mu kontrol edin.", error = ex.Message });
        }
    }

    [HttpGet("stockinfo/{ticker}")]
    public async Task<ActionResult<StockInfoDto>> GetStockInfo(string ticker)
    {
        try
        {
            StockQuoteDto? quote = null;
            CompanyProfileDto? profile = null;
            FinancialMetricsDto? metrics = null;

            try
            {
                quote = await _quoteService.GetQuoteAsync(ticker);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Quote alınamadı: {ticker}, Hata: {ex.Message}");
            }

            try
            {
                profile = await _profileService.GetProfileAsync(ticker);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Profile alınamadı: {ticker}, Hata: {ex.Message}");
            }

            try
            {
                metrics = await _metricsService.GetMetricsAsync(ticker);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Metrics alınamadı: {ticker}, Hata: {ex.Message}");
            }

            if (quote == null)
            {
                return NotFound(new { message = $"Hisse senedi bilgisi bulunamadı: {ticker}. Sembol doğru mu kontrol edin." });
            }

            return Ok(new StockInfoDto
            {
                Quote = quote,
                Profile = profile,
                Metrics = metrics
            });
        }
        catch (Exception ex)
        {
            return NotFound(new { message = $"Hisse senedi bilgisi bulunamadı: {ticker}. Sembol doğru mu kontrol edin.", error = ex.Message });
        }
    }
}

