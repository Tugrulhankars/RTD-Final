using MarketDataService.Dtos;
using MarketDataService.Services;
using Microsoft.AspNetCore.Mvc;

namespace MarketDataService.Controllers;

[ApiController]
[Route("api/profiles")]
public class CompanyProfileController : ControllerBase
{
    private readonly ICompanyProfileService _service;
    public CompanyProfileController(ICompanyProfileService service) => _service = service;

    [HttpGet("{ticker}")]
    public async Task<CompanyProfileDto> Get(string ticker) => await _service.GetProfileAsync(ticker);
}
