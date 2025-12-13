using AccountService.Dtos.Request;
using AccountService.Dtos.Response;
using AccountService.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AccountService.Controllers;

[Route("api/[controller]")]
[ApiController]
public class AccountController : ControllerBase
{
    private readonly IAccountService _accountService;

    public AccountController(IAccountService accountService)
    {
        _accountService = accountService;
    }

    [HttpPost("create")]
    public async Task<IActionResult> CreateAccount([FromBody] CreateAccountRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var response = await _accountService.CreateAccount(request);
        return Ok(response);
    }

    [HttpGet("getAccountByUser/{userId}")]
    public async Task<IActionResult> GetAccountByUser(int userId)
    {
        var response = await _accountService.GetAccountByUser(userId);
        if (response == null)
        {
            return NotFound("Kullanıcı hesabı bulunamadı.");
        }
        return Ok(response);
    }

    [HttpPut("updateBalance")]
    public async Task<IActionResult> UpdateBalance([FromBody] UpdateBalanceRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var response = await _accountService.UpdateBalance(request);
        return Ok(response);
    }
}
