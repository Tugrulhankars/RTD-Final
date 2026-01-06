# DNS Test ve Proxy Yapılandırması Örnekleri

## 1. DNS Test Kodu (C#)

DNS çözümlemesini test etmek için aşağıdaki kodu kullanabilirsiniz:

### Basit DNS Test

```csharp
using System.Net;

// Program.cs veya bir controller'da test için
try
{
    var hostname = "sandbox-api.iyzipay.com";
    var addresses = await Dns.GetHostAddressesAsync(hostname);
    
    if (addresses.Length > 0)
    {
        Console.WriteLine($"DNS çözümlemesi başarılı: {hostname}");
        foreach (var address in addresses)
        {
            Console.WriteLine($"  IP Adresi: {address}");
        }
    }
    else
    {
        Console.WriteLine($"DNS çözümlemesi başarısız: {hostname} için IP adresi bulunamadı");
    }
}
catch (System.Net.Sockets.SocketException ex)
{
    Console.WriteLine($"DNS çözümleme hatası: {ex.SocketErrorCode} - {ex.Message}");
    // SocketErrorCode.HostNotFound = 11001 (DNS hatası)
}
catch (Exception ex)
{
    Console.WriteLine($"Genel hata: {ex.Message}");
}
```

### Test Endpoint (PaymentController'a Eklenebilir)

```csharp
[HttpGet("test-dns")]
public async Task<IActionResult> TestDns()
{
    try
    {
        var hostname = "sandbox-api.iyzipay.com";
        var addresses = await System.Net.Dns.GetHostAddressesAsync(hostname);
        
        return Ok(new
        {
            success = true,
            hostname = hostname,
            ipAddresses = addresses.Select(a => a.ToString()).ToArray(),
            message = $"DNS çözümlemesi başarılı. {addresses.Length} IP adresi bulundu."
        });
    }
    catch (System.Net.Sockets.SocketException ex)
    {
        return StatusCode(500, new
        {
            success = false,
            error = "DNS çözümleme hatası",
            socketErrorCode = ex.SocketErrorCode.ToString(),
            errorCode = ex.ErrorCode,
            message = ex.Message
        });
    }
    catch (Exception ex)
    {
        return StatusCode(500, new
        {
            success = false,
            error = "Genel hata",
            message = ex.Message
        });
    }
}
```

## 2. HttpClient Proxy Yapılandırması

Iyzico SDK kendi HttpClient'ını kullandığı için, proxy yapılandırması doğrudan SDK üzerinden yapılamaz. Ancak sistem genelinde proxy kullanılabilir.

### Sistem Proxy Kullanımı (Program.cs)

Eğer Iyzico SDK dışında kendi HttpClient'ınız varsa (örneğin AccountService için), şu şekilde yapılandırabilirsiniz:

```csharp
// Program.cs içinde
builder.Services.AddHttpClient("AccountService", client =>
{
    var accountServiceUrl = builder.Configuration["AccountService:BaseUrl"] ?? "http://localhost:5239";
    client.BaseAddress = new Uri(accountServiceUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
})
.ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler()
{
    // Sistem proxy ayarlarını kullan (varsayılan davranış)
    UseProxy = true,
    Proxy = System.Net.WebRequest.GetSystemWebProxy(),
    // Veya manuel proxy ayarı:
    // Proxy = new System.Net.WebProxy("http://proxy.example.com:8080")
});
```

### Manuel Proxy Yapılandırması

```csharp
builder.Services.AddHttpClient("AccountService", client =>
{
    var accountServiceUrl = builder.Configuration["AccountService:BaseUrl"] ?? "http://localhost:5239";
    client.BaseAddress = new Uri(accountServiceUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
})
.ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler()
{
    UseProxy = true,
    // Proxy adresi ve portu appsettings.json'dan al
    Proxy = new System.Net.WebProxy(
        builder.Configuration["Proxy:Address"] ?? "http://proxy.example.com:8080"
    )
    {
        // Proxy kimlik doğrulama (gerekirse)
        Credentials = new System.Net.NetworkCredential(
            builder.Configuration["Proxy:Username"] ?? "",
            builder.Configuration["Proxy:Password"] ?? ""
        )
    }
});
```

### appsettings.json Proxy Yapılandırması

```json
{
  "Proxy": {
    "Address": "http://proxy.example.com:8080",
    "Username": "proxy_user",
    "Password": "proxy_password"
  }
}
```

## 3. Iyzico SDK için Proxy Çözümü

Iyzico SDK kendi HttpClient'ını kullandığı için, proxy yapılandırması için şu seçenekler var:

### Seçenek 1: Sistem Genelinde Proxy (Önerilen)

Windows'ta sistem proxy ayarlarını yapılandırın:
- Settings > Network & Internet > Proxy
- "Use a proxy server" seçeneğini etkinleştirin
- Proxy adresini ve portunu girin

Iyzico SDK otomatik olarak sistem proxy'sini kullanacaktır.

### Seçenek 2: Ortam Değişkenleri

Windows'ta ortam değişkenleri ile proxy ayarlayın:

```cmd
# HTTP Proxy
set HTTP_PROXY=http://proxy.example.com:8080
set HTTPS_PROXY=http://proxy.example.com:8080

# Proxy kimlik doğrulama (gerekirse)
set HTTP_PROXY_USER=username
set HTTP_PROXY_PASS=password
```

### Seçenek 3: Iyzico SDK Source Code Değişikliği (Gelişmiş)

Iyzico SDK'nın kaynak kodunu değiştirmek gerekiyorsa, SDK içindeki HttpClient yapılandırmasını değiştirmeniz gerekir. Bu önerilmez çünkü SDK güncellemelerinde kaybolur.

## 4. DNS Test Helper Kullanımı

`DnsTestHelper` sınıfını kullanarak DNS testi yapabilirsiniz:

```csharp
using PaymentService.Helpers;

// Basit test
var isDnsWorking = await DnsTestHelper.TestDnsResolutionAsync("sandbox-api.iyzipay.com", _logger);

if (!isDnsWorking)
{
    _logger.LogError("DNS çözümlemesi başarısız - Iyzico API'sine bağlanılamayabilir");
}

// Detaylı test
var (success, ipAddresses, errorMessage) = await DnsTestHelper.TestDnsResolutionDetailedAsync(
    "sandbox-api.iyzipay.com", _logger);

if (success)
{
    _logger.LogInformation("DNS çözümlemesi başarılı. IP adresleri: {IpAddresses}", 
        string.Join(", ", ipAddresses));
}
else
{
    _logger.LogError("DNS çözümlemesi başarısız: {ErrorMessage}", errorMessage);
}
```

## 5. Geliştirme Ortamında Test

PaymentController'a geçici bir test endpoint'i ekleyebilirsiniz:

```csharp
[HttpGet("test/dns")]
public async Task<IActionResult> TestDnsResolution()
{
    var hostname = "sandbox-api.iyzipay.com";
    var (success, ipAddresses, errorMessage) = await DnsTestHelper.TestDnsResolutionDetailedAsync(
        hostname, _logger);
    
    if (success)
    {
        return Ok(new
        {
            success = true,
            hostname = hostname,
            ipAddresses = ipAddresses,
            message = "DNS çözümlemesi başarılı"
        });
    }
    else
    {
        return StatusCode(500, new
        {
            success = false,
            hostname = hostname,
            error = errorMessage,
            message = "DNS çözümlemesi başarısız"
        });
    }
}
```

Test etmek için: `GET https://localhost:7009/api/payment/test/dns`

