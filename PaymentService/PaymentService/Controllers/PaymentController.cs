using Iyzipay;
using Iyzipay.Model;
using Iyzipay.Request;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PaymentService.Dtos.Request;
using PaymentService.Dtos.Response;
using PaymentService.Exceptions;
using PaymentService.Events;
using PaymentService.Models;
using PaymentService.Repositories;
using PaymentService.Services;
using System.Globalization;
using System.Net.Http.Json;
using Payment = PaymentService.Models.Payment;
namespace PaymentService.Controllers;
[Route("api/[controller]")]
[ApiController]
public class PaymentController : ControllerBase
{
    private readonly IIyzicoPaymentService _iyzico;
    private readonly IRabbitMQPublisher _rabbitMQPublisher;
    private readonly IPaymentRepository _paymentRepository;
    private readonly IKafkaProducerService _kafkaProducer;
    private readonly ILogger<PaymentController> _logger;
    public PaymentController(
        IIyzicoPaymentService iyzico, 
        IRabbitMQPublisher rabbitMQPublisher,
        IPaymentRepository paymentRepository,
        IKafkaProducerService kafkaProducer,
        ILogger<PaymentController> logger)
    {
        _iyzico = iyzico;
        _rabbitMQPublisher = rabbitMQPublisher;
        _paymentRepository = paymentRepository;
        _kafkaProducer = kafkaProducer;
        _logger = logger;
    }
    [HttpPost("create")]
    public async Task<IActionResult> Create([FromBody] CreatePaymentDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }
        try
        {
            _logger.LogInformation("Ödeme oluşturma isteği alındı: UserId={UserId}, Amount={Amount}, Email={Email}", 
                dto.UserId, dto.Amount, dto.Email);
            var checkout = await _iyzico.CreatePaymentAsync(dto.Amount, dto.Email, dto.UserId);
            if (checkout == null)
            {
                _logger.LogError("Iyzico'dan null yanıt döndü: UserId={UserId}, Amount={Amount}", dto.UserId, dto.Amount);
                return StatusCode(500, new { 
                    success = false, 
                    message = "Ödeme formu oluşturulamadı. Lütfen tekrar deneyin.",
                    checkoutFormContent = (string?)null,
                    token = (string?)null,
                    conversationId = (string?)null
                });
            }
            if (string.IsNullOrEmpty(checkout.CheckoutFormContent) || string.IsNullOrEmpty(checkout.Token))
            {
                _logger.LogError("Iyzico yanıtı eksik: UserId={UserId}, Status={Status}, ErrorMessage={ErrorMessage}, ErrorCode={ErrorCode}", 
                    dto.UserId, checkout.Status, checkout.ErrorMessage, checkout.ErrorCode);
                return StatusCode(500, new { 
                    success = false, 
                    message = $"Ödeme formu oluşturulamadı. Hata: {checkout.ErrorMessage ?? checkout.ErrorCode ?? "Bilinmeyen hata"}",
                    checkoutFormContent = (string?)null,
                    token = (string?)null,
                    conversationId = checkout.ConversationId
                });
            }
            _logger.LogInformation("Ödeme formu başarıyla oluşturuldu: UserId={UserId}, Token={Token}", 
                dto.UserId, checkout.Token);
            return Ok(new { 
                checkoutFormContent = checkout.CheckoutFormContent, 
                token = checkout.Token, 
                conversationId = checkout.ConversationId 
            });
        }
        catch (PaymentProviderUnreachableException ex)
        {
            _logger.LogError(ex, "Payment Provider (Iyzico) erişilemez: UserId={UserId}, Amount={Amount}, Email={Email}, Hostname={Hostname}, ErrorCode={ErrorCode}", 
                dto.UserId, dto.Amount, dto.Email, ex.Hostname, ex.ErrorCode);
            var errorResponse = new PaymentErrorResponse
            {
                Success = false,
                ErrorCode = "PAYMENT_PROVIDER_UNREACHABLE",
                Message = "Payment Provider is temporarily unreachable due to local network/DNS issues",
                Details = ex.Message,
                Metadata = new Dictionary<string, object>
                {
                    { "hostname", ex.Hostname ?? "unknown" },
                    { "errorCode", ex.ErrorCode ?? 0 },
                    { "dnsDiagnostic", ex.DnsDiagnosticMessage ?? "N/A" }
                }
            };
            return StatusCode(503, errorResponse);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("bağlanılamıyor") || ex.Message.Contains("internet"))
        {
            _logger.LogError(ex, "Iyzico API bağlantı hatası: UserId={UserId}, Amount={Amount}, Email={Email}", 
                dto.UserId, dto.Amount, dto.Email);
            return StatusCode(503, new { 
                success = false, 
                message = $"Ödeme servisine bağlanılamıyor. Lütfen internet bağlantınızı kontrol edin ve tekrar deneyin. " +
                         $"Detay: {ex.Message}",
                checkoutFormContent = (string?)null,
                token = (string?)null,
                conversationId = (string?)null
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ödeme oluşturma hatası: UserId={UserId}, Amount={Amount}, Email={Email}, ExceptionType={ExceptionType}", 
                dto.UserId, dto.Amount, dto.Email, ex.GetType().Name);
            return StatusCode(500, new { 
                success = false, 
                message = $"Ödeme formu oluşturulamadı: {ex.Message}",
                checkoutFormContent = (string?)null,
                token = (string?)null,
                conversationId = (string?)null
            });
        }
    }
    [HttpPost("iyzico/callback")]
    [AllowAnonymous]
    public async Task<IActionResult> Callback([FromForm] string token, [FromQuery] int userId)
    {
        var conversationIdFromForm = Request.Form["conversationId"].ToString();
        var status = Request.Form["status"].ToString();
        _logger.LogInformation("Iyzico callback alındı: UserId={UserId} (query param), Token={Token}, ConversationId={ConversationId}, Status={Status}", 
            userId, token ?? "NULL", conversationIdFromForm ?? "NULL", status ?? "NULL");
        if (string.IsNullOrWhiteSpace(token))
        {
            _logger.LogWarning("Callback çağrıldı ancak token parametresi boş. UserId={UserId}, Form içeriği: {FormContent}", 
                userId, string.Join(", ", Request.Form.Select(kv => $"{kv.Key}={kv.Value}")));
            return BadRequest(new { success = false, message = "Token parametresi gerekli" });
        }
        try
        {
            var checkout = await _iyzico.RetrievePaymentAsync(token);
            if (checkout == null)
            {
                _logger.LogError("Iyzico callback: checkout null döndü: Token={Token}, UserId={UserId}", token, userId);
                return BadRequest(new { success = false, message = "Ödeme bilgisi alınamadı" });
            }
            decimal amount = decimal.Parse(checkout.PaidPrice ?? "0", CultureInfo.InvariantCulture);
            string paymentTransactionId = checkout.PaymentId ?? "";
            string paymentMethod = checkout.PaymentItems?.FirstOrDefault()?.PaymentTransactionId ?? "CreditCard";
            var conversationId = checkout.ConversationId ?? conversationIdFromForm ?? "";
            string email = "";
            if (userId <= 0)
            {
                _logger.LogWarning("UserId query parametresinden geçersiz değer geldi: UserId={UserId}, ConversationId'den parse edilecek", userId);
                if (!string.IsNullOrWhiteSpace(conversationId) && conversationId.StartsWith("user-", StringComparison.OrdinalIgnoreCase))
                {
                    var afterUser = conversationId.Substring(5);
                    var firstDashIndex = afterUser.IndexOf('-');
                    if (firstDashIndex > 0 && int.TryParse(afterUser.Substring(0, firstDashIndex), out var parsedUserId))
                    {
                        userId = parsedUserId;
                        _logger.LogInformation("UserId ConversationId'den başarıyla parse edildi (fallback): UserId={UserId}", userId);
                    }
                }
            }
            if (!string.IsNullOrWhiteSpace(conversationId) && conversationId.StartsWith("user-", StringComparison.OrdinalIgnoreCase))
            {
                var emailPrefix = "-email-";
                var emailPrefixIndex = conversationId.IndexOf(emailPrefix, StringComparison.OrdinalIgnoreCase);
                if (emailPrefixIndex > 0)
                {
                    var afterEmailPrefix = conversationId.Substring(emailPrefixIndex + emailPrefix.Length);
                    var lastDashIndex = afterEmailPrefix.LastIndexOf('-');
                    if (lastDashIndex > 0)
                    {
                        var emailBase64 = afterEmailPrefix.Substring(0, lastDashIndex);
                        try
                        {
                            var emailBytes = Convert.FromBase64String(emailBase64);
                            email = System.Text.Encoding.UTF8.GetString(emailBytes);
                            _logger.LogInformation("Email başarıyla parse edildi: Email={Email}", email);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Email Base64 decode edilemedi: EmailBase64={EmailBase64}", emailBase64);
                            email = "";
                        }
                    }
                }
            }
            if (userId <= 0)
            {
                _logger.LogError("Geçersiz kullanıcı ID'si: UserId={UserId}, ConversationId={ConversationId}, Token={Token}, CheckoutConversationId={CheckoutConversationId}", 
                    userId, conversationId, token, checkout.ConversationId);
                await SendPaymentFailedEventAsync(userId, amount, email, paymentTransactionId, paymentMethod, "Geçersiz kullanıcı ID'si - query parametresi ve ConversationId'den parse edilemedi");
                return BadRequest(new { 
                    success = false, 
                    message = "Geçersiz kullanıcı ID'si",
                    conversationId = conversationId,
                    checkoutConversationId = checkout.ConversationId,
                    details = "UserId query parametresi ve ConversationId'den parse edilemedi"
                });
            }
            _logger.LogInformation("Callback işleniyor: UserId={UserId}, Email={Email}, Token={Token}, ConversationId={ConversationId}, Amount={Amount}", 
                userId, email, token, conversationId, amount);
            if (string.IsNullOrWhiteSpace(checkout.PaymentStatus))
            {
                _logger.LogWarning("Iyzico callback: PaymentStatus boş: Token={Token}, ConversationId={ConversationId}", 
                    token, checkout.ConversationId);
                await SendPaymentFailedEventAsync(userId, amount, email, paymentTransactionId, paymentMethod, 
                    "Ödeme durumu belirlenemedi");
                var statusUnknownRedirectUrl = "http://localhost:3000/dashboard/payment-failed";
                var statusUnknownHtml = $@"
<!DOCTYPE html>
<html lang=""tr"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>Ödeme Durumu Belirlenemedi</title>
</head>
<body>
    <div style=""text-align: center; margin-top: 50px; font-family: Arial, sans-serif;"">
        <h2>Ödeme Durumu Belirlenemedi</h2>
        <p>Yönlendiriliyorsunuz...</p>
        <p>Eğer yönlendirme otomatik olmazsa, <a href=""{statusUnknownRedirectUrl}"">buraya tıklayın</a>.</p>
    </div>
    <script>
        window.location.href = '{statusUnknownRedirectUrl}';
    </script>
</body>
</html>";
                return Content(statusUnknownHtml, "text/html; charset=utf-8");
            }
            if (checkout.PaymentStatus.Equals("SUCCESS", StringComparison.OrdinalIgnoreCase))
            {
                var httpClientFactory = HttpContext.RequestServices.GetRequiredService<IHttpClientFactory>();
                var accountClient = httpClientFactory.CreateClient("AccountService");
                try
                {
                    _logger.LogInformation("AccountService'den hesap bilgisi alınıyor: UserId={UserId}", userId);
                    var accountResponse = await accountClient.GetAsync($"/api/account/getAccountByUser/{userId}");
                    if (!accountResponse.IsSuccessStatusCode)
                    {
                        var errorContent = await accountResponse.Content.ReadAsStringAsync();
                        _logger.LogError("AccountService yanıt hatası: UserId={UserId}, StatusCode={StatusCode}, Response={Response}", 
                            userId, accountResponse.StatusCode, errorContent);
                        await SendPaymentFailedEventAsync(userId, amount, email, paymentTransactionId, paymentMethod, 
                            $"Kullanıcı hesabı bulunamadı (HTTP {(int)accountResponse.StatusCode})");
                        return StatusCode(500, $"Kullanıcı hesabı bulunamadı. HTTP Status: {accountResponse.StatusCode}");
                    }
                    var accountJson = await accountResponse.Content.ReadAsStringAsync();
                    _logger.LogInformation("AccountService yanıtı alındı: UserId={UserId}, Response={Response}", userId, accountJson);
                    AccountResponse account;
                    try
                    {
                        using var jsonDoc = System.Text.Json.JsonDocument.Parse(accountJson);
                        var root = jsonDoc.RootElement;
                        account = new AccountResponse
                        {
                            AccountId = root.GetProperty("accountId").GetInt32(),
                            Balance = root.GetProperty("balance").GetDecimal(),
                            FirstName = root.GetProperty("firstName").GetString() ?? string.Empty,
                            LastName = root.GetProperty("lastName").GetString() ?? string.Empty
                        };
                        if (root.TryGetProperty("accountStatus", out var accountStatusElement))
                        {
                            if (accountStatusElement.ValueKind == System.Text.Json.JsonValueKind.Number)
                            {
                                account.AccountStatus = accountStatusElement.GetInt32();
                            }
                            else if (accountStatusElement.ValueKind == System.Text.Json.JsonValueKind.String)
                            {
                                var statusString = accountStatusElement.GetString();
                                account.AccountStatus = statusString switch
                                {
                                    "ACTIVE" => 0,
                                    "INACTIVE" => 1,
                                    "SUSPENDED" => 2,
                                    "CLOSED" => 3,
                                    _ => 0
                                };
                            }
                            else
                            {
                                account.AccountStatus = 0;
                            }
                        }
                        else
                        {
                            account.AccountStatus = 0;
                        }
                        _logger.LogInformation("Hesap bilgisi başarıyla alındı: UserId={UserId}, AccountId={AccountId}, Balance={Balance}, AccountStatus={AccountStatus}", 
                            userId, account.AccountId, account.Balance, account.AccountStatus);
                    }
                    catch (Exception parseEx)
                    {
                        _logger.LogError(parseEx, "AccountService yanıtı parse edilemedi: UserId={UserId}, JSON={Json}", userId, accountJson);
                        await SendPaymentFailedEventAsync(userId, amount, email, paymentTransactionId, paymentMethod, "Hesap bilgisi alınamadı (parse hatası)");
                        return StatusCode(500, "Hesap bilgisi alınamadı");
                    }
                    if (account == null)
                    {
                        _logger.LogError("AccountService yanıtı deserialize edilemedi: UserId={UserId}, JSON={Json}", userId, accountJson);
                        await SendPaymentFailedEventAsync(userId, amount, email, paymentTransactionId, paymentMethod, "Hesap bilgisi alınamadı (deserialize hatası)");
                        return StatusCode(500, "Hesap bilgisi alınamadı");
                    }
                    _logger.LogInformation("Hesap bilgisi başarıyla alındı: UserId={UserId}, AccountId={AccountId}, Balance={Balance}", 
                        userId, account.AccountId, account.Balance);
                    var payment = new Payment
                    {
                        Id = Guid.NewGuid(),
                        UserId = userId,
                        AccountId = account.AccountId,
                        Amount = amount,
                        Currency = checkout.Currency ?? "TRY",
                        PaymentMethod = paymentMethod,
                        PaymentTransactionId = paymentTransactionId,
                        Status = "Success",
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };
                    try
                    {
                        await _paymentRepository.CreatePayment(payment);
                    }
                    catch (InvalidOperationException invOpEx) when (invOpEx.Message.Contains("Veritabanına bağlanılamadı") || invOpEx.Message.Contains("Veritabanı"))
                    {
                        _logger.LogError(invOpEx, 
                            "Payment kaydı oluşturulamadı - Veritabanı erişim hatası: UserId={UserId}, AccountId={AccountId}, Amount={Amount}, InnerException={InnerException}", 
                            userId, account.AccountId, amount, invOpEx.InnerException?.Message);
                        string technicalDetails = "Database connection error";
                        if (invOpEx.InnerException is Microsoft.Data.SqlClient.SqlException sqlEx)
                        {
                            _logger.LogError("SQL Exception Details: ErrorCode={ErrorCode}, Server={Server}, Number={Number}, State={State}, Class={Class}, Message={Message}", 
                                sqlEx.ErrorCode, sqlEx.Server, sqlEx.Number, sqlEx.State, sqlEx.Class, sqlEx.Message);
                            technicalDetails = $"SQL Error: {sqlEx.Message} (Error Code: {sqlEx.Number}, Server: {sqlEx.Server ?? "Unknown"}, State: {sqlEx.State}, Class: {sqlEx.Class})";
                        }
                        else if (invOpEx.InnerException != null)
                        {
                            technicalDetails = $"Inner Exception: {invOpEx.InnerException.GetType().Name} - {invOpEx.InnerException.Message}";
                        }
                        await SendPaymentFailedEventAsync(userId, amount, email, paymentTransactionId, paymentMethod, 
                            "Veritabanı bağlantı hatası - Ödeme kaydı oluşturulamadı");
                        var dbErrorRedirectUrl = "http://localhost:3000/dashboard/payment-failed";
                        var dbErrorHtml = $@"
<!DOCTYPE html>
<html lang=""tr"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>Ödeme Hatası</title>
</head>
<body>
    <div style=""text-align: center; margin-top: 50px; font-family: Arial, sans-serif;"">
        <h2>Ödeme İşlemi Tamamlandı Ancak Kayıt Oluşturulamadı</h2>
        <p>Lütfen sistem yöneticisiyle iletişime geçin.</p>
        <p>Yönlendiriliyorsunuz...</p>
        <p>Eğer yönlendirme otomatik olmazsa, <a href=""{dbErrorRedirectUrl}"">buraya tıklayın</a>.</p>
    </div>
    <script>
        window.location.href = '{dbErrorRedirectUrl}';
    </script>
</body>
</html>";
                        return Content(dbErrorHtml, "text/html; charset=utf-8");
                    }
                    catch (DbUpdateException dbUpdateEx)
                    {
                        _logger.LogError(dbUpdateEx, 
                            "Payment kaydı güncellenemedi - Database update hatası: UserId={UserId}, AccountId={AccountId}, Amount={Amount}, InnerException={InnerException}", 
                            userId, account.AccountId, amount, dbUpdateEx.InnerException?.Message);
                        string technicalDetails = "Database update failed";
                        if (dbUpdateEx.InnerException is Microsoft.Data.SqlClient.SqlException sqlEx)
                        {
                            technicalDetails = $"SQL Error: {sqlEx.Message} (Error Code: {sqlEx.Number}, Server: {sqlEx.Server ?? "Unknown"}, State: {sqlEx.State}, Class: {sqlEx.Class})";
                        }
                        else if (dbUpdateEx.InnerException != null)
                        {
                            technicalDetails = $"Inner Exception: {dbUpdateEx.InnerException.GetType().Name} - {dbUpdateEx.InnerException.Message}";
                        }
                        await SendPaymentFailedEventAsync(userId, amount, email, paymentTransactionId, paymentMethod, 
                            "Veritabanı güncelleme hatası - Ödeme kaydı oluşturulamadı");
                        var dbUpdateErrorRedirectUrl = "http://localhost:3000/dashboard/payment-failed";
                        var dbUpdateErrorHtml = $@"
<!DOCTYPE html>
<html lang=""tr"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>Ödeme Hatası</title>
</head>
<body>
    <div style=""text-align: center; margin-top: 50px; font-family: Arial, sans-serif;"">
        <h2>Ödeme İşlemi Tamamlandı Ancak Kayıt Oluşturulamadı</h2>
        <p>Lütfen daha sonra tekrar deneyin.</p>
        <p>Yönlendiriliyorsunuz...</p>
        <p>Eğer yönlendirme otomatik olmazsa, <a href=""{dbUpdateErrorRedirectUrl}"">buraya tıklayın</a>.</p>
    </div>
    <script>
        window.location.href = '{dbUpdateErrorRedirectUrl}';
    </script>
</body>
</html>";
                        return Content(dbUpdateErrorHtml, "text/html; charset=utf-8");
                    }
                    catch (Exception paymentDbEx)
                    {
                        _logger.LogError(paymentDbEx, 
                            "Payment kaydı oluşturulurken beklenmeyen hata: UserId={UserId}, AccountId={AccountId}, Amount={Amount}, ExceptionType={ExceptionType}", 
                            userId, account.AccountId, amount, paymentDbEx.GetType().Name);
                        string technicalDetails = $"Unexpected error: {paymentDbEx.GetType().Name} - {paymentDbEx.Message}";
                        if (paymentDbEx.InnerException is Microsoft.Data.SqlClient.SqlException sqlEx)
                        {
                            technicalDetails = $"SQL Error: {sqlEx.Message} (Error Code: {sqlEx.Number}, Server: {sqlEx.Server ?? "Unknown"}, State: {sqlEx.State}, Class: {sqlEx.Class})";
                        }
                        await SendPaymentFailedEventAsync(userId, amount, email, paymentTransactionId, paymentMethod, 
                            $"Beklenmeyen veritabanı hatası: {paymentDbEx.Message}");
                        var unexpectedErrorRedirectUrl = "http://localhost:3000/dashboard/payment-failed";
                        var unexpectedErrorHtml = $@"
<!DOCTYPE html>
<html lang=""tr"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>Ödeme Hatası</title>
</head>
<body>
    <div style=""text-align: center; margin-top: 50px; font-family: Arial, sans-serif;"">
        <h2>Ödeme İşlemi Sırasında Bir Hata Oluştu</h2>
        <p>Lütfen daha sonra tekrar deneyin.</p>
        <p>Yönlendiriliyorsunuz...</p>
        <p>Eğer yönlendirme otomatik olmazsa, <a href=""{unexpectedErrorRedirectUrl}"">buraya tıklayın</a>.</p>
    </div>
    <script>
        window.location.href = '{unexpectedErrorRedirectUrl}';
    </script>
</body>
</html>";
                        return Content(unexpectedErrorHtml, "text/html; charset=utf-8");
                    }
                    try
                    {
                        var paymentSuccessEvent = new PaymentSuccessEvent
                        {
                            UserId = userId,
                            AccountId = account.AccountId,
                            Amount = amount,
                            Currency = checkout.Currency ?? "TRY",
                            PaymentTransactionId = paymentTransactionId,
                            PaymentMethod = paymentMethod,
                            PaymentDate = DateTime.UtcNow,
                            Email = email,
                            Status = "SUCCESS",
                            Message = "Ödeme başarılı, bakiye güncellemesi için Kafka event gönderildi"
                        };
                        await _kafkaProducer.PublishAsync("payment-success", paymentSuccessEvent);
                        _logger.LogInformation("PaymentSuccessEvent Kafka'ya gönderildi: UserId={UserId}, AccountId={AccountId}, Amount={Amount}", 
                            userId, account.AccountId, amount);
                    }
                    catch (Exception kafkaEx)
                    {
                        _logger.LogError(kafkaEx, "Kafka'ya PaymentSuccessEvent gönderilirken hata: UserId={UserId}, AccountId={AccountId}", 
                            userId, account.AccountId);
                    }
                    await SendPaymentSuccessEventAsync(userId, account.AccountId, amount, email, paymentTransactionId, paymentMethod, "Ödeme başarılı");
                    _logger.LogInformation("Ödeme başarıyla tamamlandı: UserId={UserId}, AccountId={AccountId}, Amount={Amount}, TransactionId={TransactionId}", 
                        userId, account.AccountId, amount, paymentTransactionId);
                    var successRedirectUrl = "http://localhost:5173/strategy/dashboard";
                    var successHtml = $@"
<!DOCTYPE html>
<html lang=""tr"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>Ödeme Başarılı</title>
</head>
<body>
    <div style=""text-align: center; margin-top: 50px; font-family: Arial, sans-serif;"">
        <h2>Ödeme Başarılı!</h2>
        <p>Yönlendiriliyorsunuz...</p>
        <p>Eğer yönlendirme otomatik olmazsa, <a href=""{successRedirectUrl}"">buraya tıklayın</a>.</p>
    </div>
    <script>
        window.location.href = '{successRedirectUrl}';
    </script>
</body>
</html>";
                    return Content(successHtml, "text/html; charset=utf-8");
                }
                catch (System.Net.Http.HttpRequestException httpEx)
                {
                    _logger.LogError(httpEx, "AccountService'e HTTP isteği başarısız: UserId={UserId}, Message={Message}", userId, httpEx.Message);
                    await SendPaymentFailedEventAsync(userId, amount, email, paymentTransactionId, paymentMethod, 
                        $"AccountService'e bağlanılamadı: {httpEx.Message}");
                    var accountServiceErrorRedirectUrl = "http://localhost:3000/dashboard/payment-failed";
                    var accountServiceErrorHtml = $@"
<!DOCTYPE html>
<html lang=""tr"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>Ödeme Hatası</title>
</head>
<body>
    <div style=""text-align: center; margin-top: 50px; font-family: Arial, sans-serif;"">
        <h2>AccountService'e Ulaşılamıyor</h2>
        <p>Lütfen servisin çalıştığından emin olun.</p>
        <p>Yönlendiriliyorsunuz...</p>
        <p>Eğer yönlendirme otomatik olmazsa, <a href=""{accountServiceErrorRedirectUrl}"">buraya tıklayın</a>.</p>
    </div>
    <script>
        window.location.href = '{accountServiceErrorRedirectUrl}';
    </script>
</body>
</html>";
                    return Content(accountServiceErrorHtml, "text/html; charset=utf-8");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "AccountService'den hesap bilgisi alınırken beklenmeyen hata: UserId={UserId}, Exception={Exception}", 
                        userId, ex);
                    await SendPaymentFailedEventAsync(userId, amount, email, paymentTransactionId, paymentMethod, 
                        $"AccountService hatası: {ex.Message}");
                    var accountServiceExceptionRedirectUrl = "http://localhost:3000/dashboard/payment-failed";
                    var accountServiceExceptionHtml = $@"
<!DOCTYPE html>
<html lang=""tr"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>Ödeme Hatası</title>
</head>
<body>
    <div style=""text-align: center; margin-top: 50px; font-family: Arial, sans-serif;"">
        <h2>AccountService Hatası</h2>
        <p>{ex.Message}</p>
        <p>Yönlendiriliyorsunuz...</p>
        <p>Eğer yönlendirme otomatik olmazsa, <a href=""{accountServiceExceptionRedirectUrl}"">buraya tıklayın</a>.</p>
    </div>
    <script>
        window.location.href = '{accountServiceExceptionRedirectUrl}';
    </script>
</body>
</html>";
                    return Content(accountServiceExceptionHtml, "text/html; charset=utf-8");
                }
            }
            else
            {
                string failureReason = checkout.ErrorMessage ?? checkout.Status ?? checkout.PaymentStatus ?? "Ödeme başarısız";
                _logger.LogWarning("Ödeme başarısız: UserId={UserId}, Amount={Amount}, PaymentStatus={PaymentStatus}, Reason={Reason}", 
                    userId, amount, checkout.PaymentStatus, failureReason);
                await SendPaymentFailedEventAsync(userId, amount, email, paymentTransactionId, paymentMethod, failureReason, checkout.ErrorCode);
                var paymentFailedRedirectUrl = "http://localhost:3000/dashboard/payment-failed";
                var paymentFailedHtml = $@"
<!DOCTYPE html>
<html lang=""tr"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>Ödeme Başarısız</title>
</head>
<body>
    <div style=""text-align: center; margin-top: 50px; font-family: Arial, sans-serif;"">
        <h2>Ödeme Başarısız</h2>
        <p>{failureReason}</p>
        <p>Yönlendiriliyorsunuz...</p>
        <p>Eğer yönlendirme otomatik olmazsa, <a href=""{paymentFailedRedirectUrl}"">buraya tıklayın</a>.</p>
    </div>
    <script>
        window.location.href = '{paymentFailedRedirectUrl}';
    </script>
</body>
</html>";
                return Content(paymentFailedHtml, "text/html; charset=utf-8");
            }
        }
        catch (ArgumentException argEx)
        {
            _logger.LogWarning(argEx, "Callback: Geçersiz parametre: {Message}", argEx.Message);
            return BadRequest(new { success = false, message = argEx.Message });
        }
        catch (InvalidOperationException invEx)
        {
            _logger.LogError(invEx, "Callback: İşlem hatası: {Message}", invEx.Message);
            return StatusCode(500, new { success = false, message = invEx.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Callback işlenirken beklenmeyen hata: Token={Token}, ExceptionType={ExceptionType}", 
                token, ex.GetType().Name);
            return StatusCode(500, new { 
                success = false,
                message = "Ödeme sonucu işlenirken bir hata oluştu", 
                error = ex.Message 
            });
        }
    }
    private async Task SendPaymentSuccessEventAsync(int userId, int accountId, decimal amount, string email, string paymentTransactionId, string paymentMethod, string message)
    {
        try
        {
            // Email boşsa AccountService'den al
            if (string.IsNullOrWhiteSpace(email))
            {
                _logger.LogWarning("Email boş, AccountService'den alınmaya çalışılıyor: UserId={UserId}, AccountId={AccountId}", 
                    userId, accountId);
                try
                {
                    var httpClientFactory = HttpContext.RequestServices.GetRequiredService<IHttpClientFactory>();
                    var accountClient = httpClientFactory.CreateClient("AccountService");
                    var accountResponse = await accountClient.GetAsync($"/api/account/getAccountByUser/{userId}");
                    if (accountResponse.IsSuccessStatusCode)
                    {
                        var accountJson = await accountResponse.Content.ReadAsStringAsync();
                        using var jsonDoc = System.Text.Json.JsonDocument.Parse(accountJson);
                        var root = jsonDoc.RootElement;
                        if (root.TryGetProperty("email", out var emailElement))
                        {
                            email = emailElement.GetString() ?? string.Empty;
                            _logger.LogInformation("Email AccountService'den alındı: UserId={UserId}, Email={Email}", userId, email);
                        }
                    }
                }
                catch (Exception emailEx)
                {
                    _logger.LogWarning(emailEx, "AccountService'den email alınamadı: UserId={UserId}, AccountId={AccountId}", 
                        userId, accountId);
                }
            }
            
            // Hala email boşsa event gönderilemez
            if (string.IsNullOrWhiteSpace(email))
            {
                _logger.LogWarning("PaymentSuccessEvent gönderilemedi: Email boş veya null. UserId={UserId}, AccountId={AccountId}, Amount={Amount}", 
                    userId, accountId, amount);
                return;
            }
            
            var successEvent = new PaymentSuccessEvent
            {
                UserId = userId,
                AccountId = accountId,
                Amount = amount,
                Currency = "TRY",
                PaymentTransactionId = paymentTransactionId ?? string.Empty,
                PaymentMethod = paymentMethod ?? string.Empty,
                PaymentDate = DateTime.UtcNow,
                Email = email.Trim(),
                Status = "SUCCESS",
                Message = message
            };
            await _rabbitMQPublisher.PublishAsync(successEvent, "notification.payment.success.queue");
            _logger.LogInformation("PaymentSuccessEvent gönderildi: UserId={UserId}, AccountId={AccountId}, Amount={Amount}, Email={Email}, Status={Status}", 
                userId, accountId, amount, email, successEvent.Status);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "PaymentSuccessEvent gönderilirken hata oluştu: UserId={UserId}, Email={Email}", userId, email);
        }
    }
    private async Task SendPaymentFailedEventAsync(int userId, decimal amount, string email, string paymentTransactionId, string paymentMethod, string failureReason, string? errorCode = null)
    {
        try
        {
            // Email boşsa AccountService'den al
            if (string.IsNullOrWhiteSpace(email))
            {
                _logger.LogWarning("Email boş, AccountService'den alınmaya çalışılıyor: UserId={UserId}", userId);
                try
                {
                    var httpClientFactory = HttpContext.RequestServices.GetRequiredService<IHttpClientFactory>();
                    var accountClient = httpClientFactory.CreateClient("AccountService");
                    var accountResponse = await accountClient.GetAsync($"/api/account/getAccountByUser/{userId}");
                    if (accountResponse.IsSuccessStatusCode)
                    {
                        var accountJson = await accountResponse.Content.ReadAsStringAsync();
                        using var jsonDoc = System.Text.Json.JsonDocument.Parse(accountJson);
                        var root = jsonDoc.RootElement;
                        if (root.TryGetProperty("email", out var emailElement))
                        {
                            email = emailElement.GetString() ?? string.Empty;
                            _logger.LogInformation("Email AccountService'den alındı: UserId={UserId}, Email={Email}", userId, email);
                        }
                    }
                }
                catch (Exception emailEx)
                {
                    _logger.LogWarning(emailEx, "AccountService'den email alınamadı: UserId={UserId}", userId);
                }
            }
            
            // Hala email boşsa event gönderilemez
            if (string.IsNullOrWhiteSpace(email))
            {
                _logger.LogWarning("PaymentFailedEvent gönderilemedi: Email boş veya null. UserId={UserId}, Amount={Amount}, Reason={Reason}", 
                    userId, amount, failureReason);
                return;
            }
            
            var failedEvent = new PaymentFailedEvent
            {
                UserId = userId,
                Amount = amount,
                Currency = "TRY",
                PaymentTransactionId = paymentTransactionId ?? string.Empty,
                PaymentMethod = paymentMethod ?? string.Empty,
                PaymentDate = DateTime.UtcNow,
                Email = email.Trim(),
                Status = "FAILED",
                FailureReason = failureReason ?? string.Empty,
                ErrorCode = errorCode,
                ErrorMessage = failureReason ?? string.Empty
            };
            await _rabbitMQPublisher.PublishAsync(failedEvent, "notification.payment.failed.queue");
            _logger.LogInformation("PaymentFailedEvent gönderildi: UserId={UserId}, Amount={Amount}, Email={Email}, Status={Status}, Reason={Reason}", 
                userId, amount, email, failedEvent.Status, failureReason);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "PaymentFailedEvent gönderilirken hata oluştu: UserId={UserId}, Email={Email}", userId, email);
        }
    }
    private class AccountResponse
    {
        public int AccountId { get; set; }
        public decimal Balance { get; set; }
        public int AccountStatus { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
    }
    
    
    [HttpGet("health/dns")]
    [AllowAnonymous]
    public async Task<IActionResult> HealthCheckDns()
    {
        var results = new Dictionary<string, object>();
        try
        {
            var googleAddresses = await System.Net.Dns.GetHostAddressesAsync("google.com");
            results["google.com"] = new
            {
                success = true,
                ipAddresses = googleAddresses.Select(a => a.ToString()).ToArray(),
                message = "DNS resolution successful"
            };
        }
        catch (Exception ex)
        {
            results["google.com"] = new
            {
                success = false,
                ipAddresses = Array.Empty<string>(),
                message = ex.Message,
                errorType = ex.GetType().Name
            };
        }
        try
        {
            var iyzicoAddresses = await System.Net.Dns.GetHostAddressesAsync("sandbox-api.iyzipay.com");
            results["sandbox-api.iyzipay.com"] = new
            {
                success = true,
                ipAddresses = iyzicoAddresses.Select(a => a.ToString()).ToArray(),
                message = "DNS resolution successful"
            };
        }
        catch (System.Net.Sockets.SocketException socketEx)
        {
            results["sandbox-api.iyzipay.com"] = new
            {
                success = false,
                ipAddresses = Array.Empty<string>(),
                message = socketEx.Message,
                errorType = socketEx.GetType().Name,
                socketErrorCode = socketEx.SocketErrorCode.ToString(),
                errorCode = socketEx.ErrorCode,
                nativeErrorCode = socketEx.NativeErrorCode
            };
        }
        catch (Exception ex)
        {
            results["sandbox-api.iyzipay.com"] = new
            {
                success = false,
                ipAddresses = Array.Empty<string>(),
                message = ex.Message,
                errorType = ex.GetType().Name
            };
        }
        var googleSuccess = ((dynamic)results["google.com"]).success;
        var iyzicoSuccess = ((dynamic)results["sandbox-api.iyzipay.com"]).success;
        var overallHealthy = googleSuccess && iyzicoSuccess;
        return overallHealthy 
            ? Ok(new { status = "healthy", dnsTests = results })
            : StatusCode(503, new { status = "unhealthy", dnsTests = results });
    }
    [HttpPost]
    public IActionResult PayCallBack([FromForm] IFormCollection collections)
    {
        CallbackData data = new(
            Status: collections["status"].ToString() ?? string.Empty,
            PaymentId: collections["paymentId"].ToString() ?? string.Empty,
            ConversationData: collections["conversationData"].ToString() ?? string.Empty,
            ConversationId: collections["conversationId"].ToString() ?? string.Empty,
            MDStatus: collections["mdStatus"].ToString() ?? string.Empty);
        if (data.Status != "success")
        {
            return BadRequest("Ödeme başarısız oldu!");
        }
        return Ok();
    }
    [HttpGet("history/{userId}")]
    public async Task<IActionResult> GetPaymentHistory(int userId)
    {
        try
        {
            var payments = await _paymentRepository.GetAllPaymentByUser(userId);
            var history = payments.Select(p => new
            {
                id = p.Id,
                type = "DEPOSIT",
                amount = p.Amount,
                currency = p.Currency,
                paymentMethod = p.PaymentMethod,
                status = p.Status,
                transactionId = p.PaymentTransactionId,
                createdAt = p.CreatedAt,
                updatedAt = p.UpdatedAt
            }).OrderByDescending(p => p.createdAt).ToList();
            return Ok(new
            {
                success = true,
                data = history
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ödeme geçmişi alınırken hata oluştu: UserId={UserId}", userId);
            return StatusCode(500, new
            {
                success = false,
                message = "Ödeme geçmişi alınırken bir hata oluştu"
            });
        }
    }
    private sealed record CallbackData(
        string Status,
        string PaymentId,
        string ConversationData,
        string ConversationId,
        string MDStatus);
}