using MarketDataService.Dtos;
using MarketDataService.Services;
using Microsoft.AspNetCore.Mvc;

namespace MarketDataService.Controllers;

[ApiController]
[Route("api/metrics")]
public class FinancialMetricsController : ControllerBase
{
    private readonly IFinancialMetricsService _service;
    public FinancialMetricsController(IFinancialMetricsService service) => _service = service;

    [HttpGet("{ticker}")]
    public async Task<FinancialMetricsDto> Get(string ticker) => await _service.GetMetricsAsync(ticker);
}
