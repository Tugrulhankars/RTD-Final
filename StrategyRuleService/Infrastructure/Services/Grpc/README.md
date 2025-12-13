# GRPC Client Servisleri

Bu klasör, GRPC client servislerini içerir. Bu servisler, diğer mikroservislere GRPC protokolü üzerinden bağlanmak için kullanılır.

## Servisler

### AccountService
- **Amaç**: Hesap bakiyesi işlemleri
- **Metodlar**:
  - `GetAccountBalanceAsync(int accountId)`: Hesap bakiyesini getirir
  - `UpdateAccountBalanceAsync(int accountId, double newBalance)`: Hesap bakiyesini günceller

### PortfolioService
- **Amaç**: Portföy işlemleri
- **Metodlar**:
  - `IsInPortfolio(int portfolioId, string symbol)`: Portföyde belirli bir hisse senedi olup olmadığını kontrol eder

## Kullanım

### Dependency Injection ile Kayıt
```csharp
// Program.cs veya Startup.cs'de
builder.Services.AddInfrastructureServices(builder.Configuration);
```

### Servis Kullanımı
```csharp
public class ExampleController : ControllerBase
{
    private readonly IAccountService _accountService;
    private readonly IPortfolioService _portfolioService;

    public ExampleController(IAccountService accountService, IPortfolioService portfolioService)
    {
        _accountService = accountService;
        _portfolioService = portfolioService;
    }

    [HttpGet("balance/{accountId}")]
    public async Task<IActionResult> GetBalance(int accountId)
    {
        try
        {
            var balance = await _accountService.GetAccountBalanceAsync(accountId);
            return Ok(new { Balance = balance });
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpGet("portfolio/{portfolioId}/has-stock/{symbol}")]
    public async Task<IActionResult> HasStock(int portfolioId, string symbol)
    {
        try
        {
            var hasStock = await _portfolioService.IsInPortfolio(portfolioId, symbol);
            return Ok(new { HasStock = hasStock });
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }
}
```

## Konfigürasyon

`appsettings.json` dosyasında GRPC servis adreslerini belirtin:

```json
{
  "GrpcServices": {
    "AccountServiceAddress": "https://localhost:5001",
    "PortfolioServiceAddress": "https://localhost:5002"
  }
}
```

## Notlar

- Şu anda servisler mock response döndürüyor
- Gerçek GRPC server'lara bağlanmak için client implementasyonlarını güncelleyin
- Proto dosyaları `Protos` klasöründe bulunmaktadır
- Code generation sorunları nedeniyle client class'ları manuel olarak oluşturulmuştur
