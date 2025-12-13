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
        var quoteTask = _quoteService.GetQuoteAsync(ticker);
        var profileTask = _profileService.GetProfileAsync(ticker);
        var metricsTask = _metricsService.GetMetricsAsync(ticker);

        await Task.WhenAll(quoteTask, profileTask, metricsTask);

        return new StockInfoDto
        {
            Quote = quoteTask.Result,
            Profile = profileTask.Result,
            Metrics = metricsTask.Result
        };
    }
}



