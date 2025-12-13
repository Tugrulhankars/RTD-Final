using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PortfolioService.Dtos.Response;
using PortfolioService.Services.Abstracts;

namespace PortfolioService.Controllers;

[Route("api/[controller]")]
[ApiController]
public class PortfolioController : ControllerBase
{
    private readonly IPortfolioService _portfolioService;
    public PortfolioController(IPortfolioService portfolioService)
    {
        _portfolioService = portfolioService;
    }

    [HttpGet("getPortfolioByUser")]
    public async Task<IActionResult> GetPortfolioByUser(int userId)
     {
         List<GetAllPortfolioResponse> portfolios = await _portfolioService.GetAllPortfolioByUserAsync(userId);
         return Ok(portfolios);
    }

    [HttpGet("getActiveStocksByUser/{userId}")]
    public async Task<IActionResult> GetActiveStocksByUser(int userId)
    {
        List<GetAllPortfolioResponse> activeStocks = await _portfolioService.GetPortfoliosWithActiveStockCertificates(userId);
        return Ok(activeStocks);
    }
}
