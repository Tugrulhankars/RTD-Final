using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PortfolioService.Dtos.Request;
using PortfolioService.Dtos.Response;
using PortfolioService.Protos;
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

    [HttpGet("health")]
    public IActionResult Health()
    {
        return Ok(new { Status = "Healthy", Service = "PortfolioService", Timestamp = DateTime.UtcNow });
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

    [HttpGet("getPortfolioByAccount/{accountId}")]
    public async Task<IActionResult> GetPortfolioByAccount(int accountId)
    {
        try
        {
            Console.WriteLine($"[PortfolioController] GetPortfolioByAccount called - AccountId={accountId}");
            var portfolio = await _portfolioService.GetPortfolioByAccountIdAsync(accountId);
            if (portfolio == null)
            {
                Console.WriteLine($"[PortfolioController] GetPortfolioByAccount - Portfolio not found for AccountId={accountId}");
                return NotFound(new { Success = false, Message = $"Portfolio not found for AccountId={accountId}" });
            }
            Console.WriteLine($"[PortfolioController] GetPortfolioByAccount - Portfolio found: Id={portfolio.Id}, UserId={portfolio.UserId}, AccountId={portfolio.AccountId}");
            return Ok(portfolio);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[PortfolioController] GetPortfolioByAccount - Exception: {ex.Message}, StackTrace: {ex.StackTrace}");
            return StatusCode(500, new { Success = false, Message = $"Error getting portfolio: {ex.Message}" });
        }
    }

    [HttpPost("addStock")]
    public async Task<IActionResult> AddStock([FromBody] AddStockRequest request)
    {
        try
        {
            var protoRequest = new AddStockCertificateToPortfolioRequest
            {
                PortfolioId = request.PortfolioId,
                Symbol = request.Symbol,
                Lot = (int)request.Lot,
                PricePerShare = request.PricePerShare,
                StockCertificateId = 0
            };

            await _portfolioService.AddStockCertificateToPortfolio(protoRequest);
            return Ok(new { Success = true, Message = "Hisse senedi başarıyla portföye eklendi." });
        }
        catch (Exception ex)
        {
            return BadRequest(new { Success = false, Message = $"Hisse senedi eklenirken bir hata oluştu: {ex.Message}" });
        }
    }

    [HttpPost("sellStock")]
    public async Task<IActionResult> SellStock([FromBody] PortfolioService.Dtos.Request.SellStockRequest request)
    {
        try
        {
            var protoRequest = new Protos.SellStockRequest
            {
                PortfolioId = request.PortfolioId,
                Symbol = request.Symbol,
                Lot = (int)request.Lot,
                PricePerShare = request.PricePerShare,
                StockCertificateId = 0
            };

            await _portfolioService.SellStockCertificateToPortfolio(protoRequest);
            return Ok(new { Success = true, Message = "Hisse senedi başarıyla satıldı." });
        }
        catch (Exception ex)
        {
            return BadRequest(new { Success = false, Message = $"Hisse senedi satılırken bir hata oluştu: {ex.Message}" });
        }
    }

    [HttpPost("createPortfolio")]
    public async Task<IActionResult> CreatePortfolio([FromBody] CreatePortfolioRequest request)
    {
        try
        {
            Console.WriteLine($"[PortfolioController] CreatePortfolio called - Request: UserId={request?.UserId}, AccountId={request?.AccountId}");
            
            if (request == null)
            {
                Console.WriteLine("[PortfolioController] CreatePortfolio - Request body is null");
                return BadRequest(new { Success = false, Message = "Request body is null" });
            }

            if (request.UserId <= 0 || request.AccountId <= 0)
            {
                Console.WriteLine($"[PortfolioController] CreatePortfolio - Invalid request: UserId={request.UserId}, AccountId={request.AccountId}");
                return BadRequest(new { Success = false, Message = $"Invalid request: UserId={request.UserId}, AccountId={request.AccountId}" });
            }

            Console.WriteLine($"[PortfolioController] CreatePortfolio - Calling service with UserId={request.UserId}, AccountId={request.AccountId}");
            await _portfolioService.CreatePortfolio(request);
            Console.WriteLine($"[PortfolioController] CreatePortfolio - Portfolio created successfully for UserId={request.UserId}, AccountId={request.AccountId}");
            
            return Ok(new { Success = true, Message = "Portföy başarıyla oluşturuldu.", UserId = request.UserId, AccountId = request.AccountId });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[PortfolioController] CreatePortfolio - Exception: {ex.Message}, StackTrace: {ex.StackTrace}");
            return StatusCode(500, new { Success = false, Message = $"Portföy oluşturulurken bir hata oluştu: {ex.Message}" });
        }
    }
}
