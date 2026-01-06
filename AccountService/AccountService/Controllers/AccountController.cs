using AccountService.Dtos.Request;
using AccountService.Dtos.Response;
using AccountService.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace AccountService.Controllers;

[Route("api/account")]
[ApiController]
public class AccountController : ControllerBase
{
    private readonly IAccountService _accountService;
    private readonly ILogger<AccountController> _logger;

    public AccountController(IAccountService accountService, ILogger<AccountController> logger)
    {
        _accountService = accountService;
        _logger = logger;
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
        try
        {
            var response = await _accountService.GetAccountByUser(userId);
            if (response == null)
            {
                _logger.LogWarning("Kullanıcı hesabı bulunamadı: UserId={UserId}", userId);
                return NotFound(new { 
                    Success = false, 
                    Message = "Kullanıcı hesabı bulunamadı. Lütfen önce hesap oluşturun." 
                });
            }
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Hesap bilgisi alınırken beklenmeyen hata oluştu: UserId={UserId}", userId);
            return NotFound(new { 
                Success = false, 
                Message = "Kullanıcı hesabı bulunamadı." 
            });
        }
    }

    [HttpPut("updateBalance")]
    [Obsolete("Bakiye güncelleme artık Payment Service üzerinden yapılmalıdır. Bu endpoint deprecated'dir.")]
    public async Task<IActionResult> UpdateBalance([FromBody] UpdateBalanceRequest request)
    {
        _logger.LogWarning("DEPRECATED UpdateBalance endpoint'i kullanıldı. Payment Service üzerinden ödeme yapılmalıdır.");
        
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var response = await _accountService.UpdateBalance(request);
        return Ok(response);
    }

    [HttpGet("transactionHistory/{userId}")]
    public async Task<IActionResult> GetTransactionHistory(int userId)
    {
        try
        {
            var account = await _accountService.GetAccountByUser(userId);
            if (account == null)
            {
                return NotFound(new { 
                    Success = false, 
                    Message = "Kullanıcı hesabı bulunamadı" 
                });
            }

            var accountId = account.AccountId;

            var paymentHistory = new List<Dictionary<string, object>>();
            try
            {
                using var httpClient = new HttpClient();
                var paymentServiceUrl = Environment.GetEnvironmentVariable("PAYMENT_SERVICE_URL") ?? "http://localhost:5238";
                var paymentResponse = await httpClient.GetAsync($"{paymentServiceUrl}/api/Payment/history/{userId}");
                
                if (paymentResponse.IsSuccessStatusCode)
                {
                    var jsonString = await paymentResponse.Content.ReadAsStringAsync();
                    var paymentData = JsonSerializer.Deserialize<JsonElement>(jsonString);
                    
                    if (paymentData.TryGetProperty("data", out var dataArray) && dataArray.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var item in dataArray.EnumerateArray())
                        {
                            var paymentDict = new Dictionary<string, object>();
                            foreach (var prop in item.EnumerateObject())
                            {
                                paymentDict[prop.Name] = prop.Value.GetRawText();
                            }
                            paymentHistory.Add(paymentDict);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "PaymentService'den ödeme geçmişi alınamadı: UserId={UserId}", userId);
            }

            var tradeHistory = new List<Dictionary<string, object>>();
            try
            {
                using var httpClient = new HttpClient();
                var tradeServiceUrl = Environment.GetEnvironmentVariable("TRADE_SERVICE_URL") ?? "http://localhost:9084";
                var tradeResponse = await httpClient.GetAsync($"{tradeServiceUrl}/api/trade/history/{accountId}");
                
                if (tradeResponse.IsSuccessStatusCode)
                {
                    var jsonString = await tradeResponse.Content.ReadAsStringAsync();
                    var tradeData = JsonSerializer.Deserialize<JsonElement>(jsonString);
                    
                    if (tradeData.TryGetProperty("data", out var dataArray) && dataArray.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var item in dataArray.EnumerateArray())
                        {
                            var tradeDict = new Dictionary<string, object>();
                            foreach (var prop in item.EnumerateObject())
                            {
                                tradeDict[prop.Name] = prop.Value.GetRawText();
                            }
                            tradeHistory.Add(tradeDict);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "TradeService'den işlem geçmişi alınamadı: AccountId={AccountId}", accountId);
            }

            var allTransactions = new List<object>();
            
            foreach (var payment in paymentHistory)
            {
                allTransactions.Add(new
                {
                    type = "DEPOSIT",
                    source = "PAYMENT",
                    data = payment
                });
            }
            
            foreach (var trade in tradeHistory)
            {
                allTransactions.Add(new
                {
                    type = "TRADE",
                    source = "TRADE",
                    data = trade
                });
            }

            var sortedTransactions = allTransactions.OrderByDescending(t => 
            {
                try
                {
                    var dataDict = ((Dictionary<string, object>)((dynamic)t).data);
                    if (dataDict.ContainsKey("createdAt"))
                    {
                        var dateStr = dataDict["createdAt"].ToString().Trim('"');
                        if (DateTime.TryParse(dateStr, out var date))
                            return date;
                    }
                    if (dataDict.ContainsKey("executedAt") || dataDict.ContainsKey("executed_at"))
                    {
                        var key = dataDict.ContainsKey("executedAt") ? "executedAt" : "executed_at";
                        var dateStr = dataDict[key].ToString().Trim('"');
                        if (DateTime.TryParse(dateStr, out var date))
                            return date;
                    }
                }
                catch { }
                return DateTime.MinValue;
            }).ToList();

            return Ok(new
            {
                Success = true,
                Data = sortedTransactions,
                TotalCount = sortedTransactions.Count
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "İşlem geçmişi alınırken hata oluştu: UserId={UserId}", userId);
            return StatusCode(500, new
            {
                Success = false,
                Message = "İşlem geçmişi alınırken bir hata oluştu"
            });
        }
    }
}
