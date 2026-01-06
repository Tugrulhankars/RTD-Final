using MarketDataService.Dtos;
using MarketDataService.Services;
using Microsoft.AspNetCore.Mvc;

namespace MarketDataService.Controllers;

[ApiController]
[Route("api/stockinfo")] 
public class StockInfoController : ControllerBase
{
    private readonly IStockQuoteService _quoteService;
    private readonly ICompanyProfileService _profileService;
    private readonly IFinancialMetricsService _metricsService;

    public StockInfoController(
        IStockQuoteService quoteService,
        ICompanyProfileService profileService,
        IFinancialMetricsService metricsService)
    {
        _quoteService = quoteService;
        _profileService = profileService;
        _metricsService = metricsService;
    }

    [HttpGet("{ticker}")]
    public async Task<StockInfoDto> Get(string ticker)
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
            throw new InvalidOperationException($"Hisse senedi bilgisi alınamadı: {ticker}");
        }

        return new StockInfoDto
        {
            Quote = quote,
            Profile = profile,
            Metrics = metrics
        };
    }
}
