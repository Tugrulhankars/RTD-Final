using Iyzipay;
using Iyzipay.Model;
using Iyzipay.Request;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PaymentService.Dtos.Request;
using PaymentService.Dtos.Response;
using PaymentService.Exceptions;
using PaymentService.Repositories;
using PaymentService.Services;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
namespace PaymentService.Services.Impl;
public class IyzicoPaymentServiceImpl : IIyzicoPaymentService
{
    private readonly IPaymentRepository _paymentRepository;
    private readonly Options _options;
    private readonly IConfiguration _configuration;
    private readonly ILogger<IyzicoPaymentServiceImpl> _logger;
    public IyzicoPaymentServiceImpl(
        IPaymentRepository paymentRepository, 
        Microsoft.Extensions.Options.IOptions<Options> opts, 
        IConfiguration configuration,
        ILogger<IyzicoPaymentServiceImpl> logger)
    {
        _paymentRepository = paymentRepository;
        _configuration = configuration;
        _logger = logger;
        var enableMockMode = _configuration.GetValue<bool>("Iyzico:EnableMockMode", false);
        if (enableMockMode)
        {
            _logger.LogWarning("Iyzico Mock Mode ENABLED - Gerçek Iyzico API çağrıları yapılmayacak!");
        }
        var baseUrl = opts.Value.BaseUrl?.Trim() ?? throw new InvalidOperationException("Iyzico BaseUrl yapılandırılmamış");
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            throw new InvalidOperationException("Iyzico BaseUrl boş veya geçersiz");
        }
        baseUrl = SanitizeBaseUrl(baseUrl);
        var hostname = ExtractHostnameFromUrl(baseUrl);
        if (!string.IsNullOrWhiteSpace(hostname) && System.Net.IPAddress.TryParse(hostname, out _))
        {
            throw new InvalidOperationException(
                $"Iyzico BaseUrl IP adresi içermemelidir. Domain adı kullanılmalı: '{baseUrl}'. " +
                $"SSL sertifika doğrulaması için domain adı gereklidir. " +
                $"Eğer DNS çözümlemesi sorunluysa, Windows Hosts dosyasına (C:\\Windows\\System32\\drivers\\etc\\hosts) " +
                $"şu satırı ekleyin: 213.226.118.91 sandbox-api.iyzipay.com");
        }
        try
        {
            var servicePointUri = new Uri(baseUrl);
            var servicePoint = ServicePointManager.FindServicePoint(servicePointUri);
            servicePoint.ConnectionLeaseTimeout = 0;
            _logger.LogInformation("ServicePointManager ayarlandı: BaseUrl={BaseUrl}, ConnectionLeaseTimeout=0 (DNS refresh forced)", baseUrl);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "ServicePointManager ayarlanırken hata oluştu (non-critical): {Error}", ex.Message);
        }
        _logger.LogDebug("Iyzico BaseUrl yüklendi: '{BaseUrl}' (Uzunluk: {Length}, İlk karakter: '{FirstChar}', Son karakter: '{LastChar}')", 
            baseUrl, baseUrl.Length, baseUrl.FirstOrDefault(), baseUrl.LastOrDefault());
        _options = new Options
        {
            ApiKey = opts.Value.ApiKey?.Trim(),
            SecretKey = opts.Value.SecretKey?.Trim(),
            BaseUrl = baseUrl
        };
        try
        {
            _logger.LogInformation("HttpClient yapılandırması: ServicePointManager.ConnectionLeaseTimeout=0 (DNS cache disabled)");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "HttpClient yapılandırması sırasında hata oluştu (non-critical): {Error}", ex.Message);
        }
        if (string.IsNullOrWhiteSpace(_options.BaseUrl))
        {
            throw new InvalidOperationException("Iyzico BaseUrl Options'ta geçersiz");
        }
    }
    public PaymentResponse Pay(PaymentRequest paymentRequest)
    {
        Options options = _options;
        CreatePaymentRequest request = new CreatePaymentRequest();
        request.Locale = Locale.TR.ToString();
        request.ConversationId = Guid.NewGuid().ToString();
        request.Price = "1";
        request.PaidPrice = "1.2";
        request.Currency = Currency.TRY.ToString();
        request.Installment = 1;
        request.BasketId = "B67832";
        request.PaymentChannel = PaymentChannel.WEB.ToString();
        request.PaymentGroup = PaymentGroup.PRODUCT.ToString();
        request.CallbackUrl = "https://localhost:7224/api/Payments/PayCallBack";
        PaymentCard paymentCard = new PaymentCard();
        paymentCard.CardHolderName = "John Doe";
        paymentCard.CardNumber = "5528790000000008";
        paymentCard.ExpireMonth = "12";
        paymentCard.ExpireYear = "2030";
        paymentCard.Cvc = "123";
        paymentCard.RegisterCard = 0;
        request.PaymentCard = paymentCard;
        Buyer buyer = new Buyer();
        buyer.Id = "BY789";
        buyer.Name = "John";
        buyer.Surname = "Doe";
        buyer.GsmNumber = "+905350000000";
        buyer.Email = "email@email.com";
        buyer.IdentityNumber = "74300864791";
        buyer.LastLoginDate = "2015-10-05 12:43:35";
        buyer.RegistrationDate = "2013-04-21 15:12:09";
        buyer.RegistrationAddress = "Nidakule Göztepe, Merdivenköy Mah. Bora Sok. No:1";
        buyer.Ip = "85.34.78.112";
        buyer.City = "Istanbul";
        buyer.Country = "Turkey";
        buyer.ZipCode = "34732";
        request.Buyer = buyer;
        Address shippingAddress = new Address();
        shippingAddress.ContactName = "Jane Doe";
        shippingAddress.City = "Istanbul";
        shippingAddress.Country = "Turkey";
        shippingAddress.Description = "Nidakule Göztepe, Merdivenköy Mah. Bora Sok. No:1";
        shippingAddress.ZipCode = "34742";
        request.ShippingAddress = shippingAddress;
        Address billingAddress = new Address();
        billingAddress.ContactName = "Jane Doe";
        billingAddress.City = "Istanbul";
        billingAddress.Country = "Turkey";
        billingAddress.Description = "Nidakule Göztepe, Merdivenköy Mah. Bora Sok. No:1";
        billingAddress.ZipCode = "34742";
        request.BillingAddress = billingAddress;
        List<BasketItem> basketItems = new List<BasketItem>();
        BasketItem firstBasketItem = new BasketItem();
        firstBasketItem.Id = "BI101";
        firstBasketItem.Name = "Binocular";
        firstBasketItem.Category1 = "Collectibles";
        firstBasketItem.Category2 = "Accessories";
        firstBasketItem.ItemType = BasketItemType.PHYSICAL.ToString();
        firstBasketItem.Price = "0.3";
        basketItems.Add(firstBasketItem);
        BasketItem secondBasketItem = new BasketItem();
        secondBasketItem.Id = "BI102";
        secondBasketItem.Name = "Game code";
        secondBasketItem.Category1 = "Game";
        secondBasketItem.Category2 = "Online Game Items";
        secondBasketItem.ItemType = BasketItemType.VIRTUAL.ToString();
        secondBasketItem.Price = "0.5";
        basketItems.Add(secondBasketItem);
        BasketItem thirdBasketItem = new BasketItem();
        thirdBasketItem.Id = "BI103";
        thirdBasketItem.Name = "Usb";
        thirdBasketItem.Category1 = "Electronics";
        thirdBasketItem.Category2 = "Usb / Cable";
        thirdBasketItem.ItemType = BasketItemType.PHYSICAL.ToString();
        thirdBasketItem.Price = "0.2";
        basketItems.Add(thirdBasketItem);
        request.BasketItems = basketItems;
        return new PaymentResponse("");
    }
    public async Task<CheckoutFormInitialize> CreatePaymentAsync(decimal amount, string email, int userId)
    {
        var enableMockMode = _configuration.GetValue<bool>("Iyzico:EnableMockMode", false);
        if (enableMockMode)
        {
            _logger.LogInformation("Mock Mode: Gerçek Iyzico API çağrısı atlanıyor. Mock response döndürülüyor: UserId={UserId}, Amount={Amount}", userId, amount);
            var mockCheckout = new CheckoutFormInitialize
            {
                Status = "success",
                Token = $"MOCK_TOKEN_{Guid.NewGuid()}",
                CheckoutFormContent = "<div>Mock Payment Form - Iyzico Mock Mode Enabled</div>",
                ConversationId = $"user-{userId}-email-{Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(email ?? ""))}-{Guid.NewGuid()}",
                ErrorMessage = null,
                ErrorCode = null
            };
            return mockCheckout;
        }
        var emailBase64 = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(email ?? ""));
        var conversationId = $"user-{userId}-email-{emailBase64}-{Guid.NewGuid()}";
        _logger.LogInformation("ConversationId oluşturuldu: ConversationId={ConversationId}, UserId={UserId}, Email={Email}", 
            conversationId, userId, email);
        var baseCallbackUrl = _configuration["Iyzico:CallbackUrl"] ?? "https://localhost:7009/api/payment/iyzico/callback";
        var callbackUrlWithUserId = $"{baseCallbackUrl}?userId={userId}";
        var request = new CreateCheckoutFormInitializeRequest
        {
            Locale = Locale.TR.ToString(),
            ConversationId = conversationId,
            Price = amount.ToString("F2", CultureInfo.InvariantCulture),
            PaidPrice = amount.ToString("F2", CultureInfo.InvariantCulture),
            Currency = Currency.TRY.ToString(),
            BasketId = $"BASKET_{userId}",
            CallbackUrl = callbackUrlWithUserId,
            PaymentGroup = PaymentGroup.PRODUCT.ToString()
        };
        _logger.LogInformation("CallbackUrl oluşturuldu: CallbackUrl={CallbackUrl}, UserId={UserId}", callbackUrlWithUserId, userId);
        request.BasketItems = new List<BasketItem>
        {
            new BasketItem
            {
                Id = "1",
                Name = "Bakiye Yükleme",
                Category1 = "Wallet",
                ItemType = BasketItemType.VIRTUAL.ToString(),
                Price = amount.ToString("F2", CultureInfo.InvariantCulture)
            }
        };
        var buyer = new Buyer
        {
            Id = userId.ToString(),
            Name = "Kullanıcı",
            Surname = "Kullanıcı",
            Email = email ?? "",
            GsmNumber = "+905550000000",
            IdentityNumber = "74455555555",
            City = "Istanbul",
            Country = "Turkey",
            ZipCode = "34732",
            RegistrationAddress = "Nidakule Göztepe, Merdivenköy Mah. Bora Sok. No:1",
            RegistrationDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
            LastLoginDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture)
        };
        if (string.IsNullOrWhiteSpace(buyer.RegistrationAddress))
        {
            _logger.LogError("Buyer RegistrationAddress boş - ErrorCode=5026 hatası oluşacak");
            buyer.RegistrationAddress = "Nidakule Göztepe, Merdivenköy Mah. Bora Sok. No:1";
        }
        if (string.IsNullOrWhiteSpace(buyer.City))
            buyer.City = "Istanbul";
        if (string.IsNullOrWhiteSpace(buyer.Country))
            buyer.Country = "Turkey";
        if (string.IsNullOrWhiteSpace(buyer.ZipCode))
            buyer.ZipCode = "34732";
        if (string.IsNullOrWhiteSpace(buyer.IdentityNumber))
            buyer.IdentityNumber = "74455555555";
        request.Buyer = buyer;
        _logger.LogDebug("Buyer bilgileri hazırlandı: Id={Id}, Email={Email}, RegistrationAddress={RegistrationAddress}, City={City}, Country={Country}, ZipCode={ZipCode}", 
            buyer.Id, buyer.Email, buyer.RegistrationAddress, buyer.City, buyer.Country, buyer.ZipCode);
        var billingAddress = new Address
        {
            ContactName = "Kullanıcı Kullanıcı",
            City = "Istanbul",
            Country = "Turkey",
            Description = "Nidakule Göztepe, Merdivenköy Mah. Bora Sok. No:1",
            ZipCode = "34732"
        };
        request.BillingAddress = billingAddress;
        var shippingAddress = new Address
        {
            ContactName = "Kullanıcı Kullanıcı",
            City = "Istanbul",
            Country = "Turkey",
            Description = "Nidakule Göztepe, Merdivenköy Mah. Bora Sok. No:1",
            ZipCode = "34732"
        };
        request.ShippingAddress = shippingAddress;
        var baseUrlForRequest = _options.BaseUrl?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(baseUrlForRequest))
        {
            _logger.LogError("Iyzico BaseUrl boş veya null: BaseUrl='{BaseUrl}'", baseUrlForRequest);
            throw new InvalidOperationException("Iyzico BaseUrl yapılandırması geçersiz veya boş");
        }
        baseUrlForRequest = baseUrlForRequest.Trim();
        ValidateBaseUrl(baseUrlForRequest);
        var expectedUrl = "https://sandbox-api.iyzipay.com";
        if (!baseUrlForRequest.Equals(expectedUrl, StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning("Iyzico BaseUrl beklenen değerden farklı: Beklenen='{Expected}', Mevcut='{Current}'", 
                expectedUrl, baseUrlForRequest);
        }
        var baseUrlBytes = System.Text.Encoding.UTF8.GetBytes(baseUrlForRequest);
        _logger.LogInformation("Iyzico ödeme formu oluşturuluyor: UserId={UserId}, Amount={Amount}, Email={Email}, ConversationId={ConversationId}", 
            userId, amount, email, conversationId);
        _logger.LogInformation("Iyzico BaseUrl detayları: BaseUrl='{BaseUrl}', Length={Length}, BytesLength={BytesLength}, " +
            "StartsWithHttps={StartsWithHttps}, EndsWithCom={EndsWithCom}", 
            baseUrlForRequest, baseUrlForRequest.Length, baseUrlBytes.Length,
            baseUrlForRequest.StartsWith("https://", StringComparison.OrdinalIgnoreCase),
            baseUrlForRequest.EndsWith(".com", StringComparison.OrdinalIgnoreCase));
        var finalUrl = $"{baseUrlForRequest.TrimEnd('/')}/payment/iyzipos/checkoutform/initialize/auth";
        _logger.LogInformation("Iyzico SDK çağrısı öncesi - Final URL: {FinalUrl}, BaseUrl: {BaseUrl}, RequestUri: /payment/iyzipos/checkoutform/initialize/auth", 
            finalUrl, baseUrlForRequest);
        try
        {
            var hostnameForDns = ExtractHostnameFromUrl(baseUrlForRequest);
            _logger.LogInformation("DNS çözümleme testi (Dns.GetHostAddresses) başlatılıyor: Hostname={Hostname}", hostnameForDns);
            var ipAddresses = Dns.GetHostAddresses(hostnameForDns);
            if (ipAddresses != null && ipAddresses.Length > 0)
            {
                var ipList = string.Join(", ", ipAddresses.Select(ip => ip.ToString()));
                _logger.LogInformation("DNS çözümleme başarılı (Dns.GetHostAddresses): Hostname={Hostname} -> IP Addresses: {IpAddresses} (Count: {Count})", 
                    hostnameForDns, ipList, ipAddresses.Length);
                foreach (var ip in ipAddresses)
                {
                    _logger.LogDebug("DNS çözümleme detayı: Hostname={Hostname} -> IP={Ip}, AddressFamily={AddressFamily}", 
                        hostnameForDns, ip.ToString(), ip.AddressFamily);
                }
            }
            else
            {
                _logger.LogWarning("DNS çözümleme başarısız (Dns.GetHostAddresses): Hostname={Hostname} -> IP adresi bulunamadı (null veya boş array)", 
                    hostnameForDns);
            }
        }
        catch (SocketException dnsSocketEx)
        {
            var hostnameForDns = ExtractHostnameFromUrl(baseUrlForRequest);
            _logger.LogError(dnsSocketEx, 
                "DNS çözümleme hatası (Dns.GetHostAddresses - SocketException): Hostname={Hostname}, SocketErrorCode={SocketErrorCode}, ErrorCode={ErrorCode}, Message={Message}", 
                hostnameForDns, dnsSocketEx.SocketErrorCode, dnsSocketEx.ErrorCode, dnsSocketEx.Message);
        }
        catch (Exception dnsEx)
        {
            var hostnameForDns = ExtractHostnameFromUrl(baseUrlForRequest);
            _logger.LogError(dnsEx, 
                "DNS çözümleme hatası (Dns.GetHostAddresses - Genel Exception): Hostname={Hostname}, ExceptionType={ExceptionType}, Message={Message}", 
                hostnameForDns, dnsEx.GetType().Name, dnsEx.Message);
        }
        CheckoutFormInitialize checkout;
        try
        {
            _logger.LogDebug("Iyzico CheckoutFormInitialize.Create çağrılıyor: BaseUrl='{BaseUrl}', FinalUrl='{FinalUrl}'", baseUrlForRequest, finalUrl);
            checkout = await CheckoutFormInitialize.Create(request, _options).ConfigureAwait(false);
            _logger.LogDebug("Iyzico CheckoutFormInitialize.Create başarıyla tamamlandı");
        }
        catch (System.Net.Sockets.SocketException socketEx)
        {
            var baseUrlForLog = baseUrlForRequest;
            var hostname = ExtractHostnameFromUrl(baseUrlForLog);
            _logger.LogError(socketEx,
                "SocketException (DNS/Ağ Bağlantı Hatası): SocketErrorCode={SocketErrorCode}, ErrorCode={ErrorCode}, " +
                "Message={Message}, NativeErrorCode={NativeErrorCode}, TargetHost={TargetHost}",
                socketEx.SocketErrorCode, socketEx.ErrorCode, socketEx.Message, socketEx.NativeErrorCode, baseUrlForLog);
                if (socketEx.SocketErrorCode == System.Net.Sockets.SocketError.HostNotFound || socketEx.ErrorCode == 11001)
                {
                    var errorMessage = $"Payment Provider is temporarily unreachable due to local network/DNS issues. " +
                        $"Hostname '{hostname}' cannot be resolved. " +
                        $"If DNS resolution is failing, add this line to Windows Hosts file (C:\\Windows\\System32\\drivers\\etc\\hosts): " +
                        $"213.226.118.91 sandbox-api.iyzipay.com";
                    _logger.LogError("Iyzico API bağlantı hatası: {ErrorMessage}", errorMessage);
                    throw new PaymentProviderUnreachableException(
                        errorMessage,
                        socketEx,
                        hostname,
                        socketEx.ErrorCode,
                        "DNS resolution failed");
                }
                var friendlyMessage = $"Iyzico API'sine bağlanılamıyor (Ağ hatası: {socketEx.SocketErrorCode}).";
                var errorMessageGeneral = $"{friendlyMessage} " +
                    $"(Hedef URL: {baseUrlForLog}, SocketErrorCode: {socketEx.SocketErrorCode}, ErrorCode: {socketEx.ErrorCode}).";
                _logger.LogError("Iyzico API bağlantı hatası: {ErrorMessage}", errorMessageGeneral);
                throw new PaymentProviderUnreachableException(
                    errorMessageGeneral,
                    socketEx,
                    hostname,
                    socketEx.ErrorCode,
                    "Network error");
        }
        catch (System.Net.Http.HttpRequestException httpEx)
        {
            var baseUrlForLog = baseUrlForRequest;
            _logger.LogError(httpEx, 
                "Iyzico API'sine HTTP isteği başarısız: UserId={UserId}, BaseUrl='{BaseUrl}', " +
                "HttpRequestException.Message={HttpExceptionMessage}, InnerExceptionType={InnerExceptionType}, InnerExceptionMessage={InnerExceptionMessage}, " +
                "TargetSite={TargetSite}", 
                userId, baseUrlForLog, httpEx.Message, 
                httpEx.InnerException?.GetType().Name ?? "NULL", 
                httpEx.InnerException?.Message ?? "NULL",
                httpEx.TargetSite?.ToString() ?? "NULL");
            if (httpEx.InnerException is System.Net.Sockets.SocketException socketEx)
            {
                var hostname = ExtractHostnameFromUrl(baseUrlForLog);
                _logger.LogError(
                    "SocketException (HttpRequestException içinde - DNS/Ağ Bağlantı Hatası): SocketErrorCode={SocketErrorCode}, ErrorCode={ErrorCode}, " +
                    "Message={Message}, NativeErrorCode={NativeErrorCode}, TargetHost={TargetHost}", 
                    socketEx.SocketErrorCode, socketEx.ErrorCode, socketEx.Message, socketEx.NativeErrorCode, baseUrlForLog);
                if (socketEx.SocketErrorCode == System.Net.Sockets.SocketError.HostNotFound || socketEx.ErrorCode == 11001)
                {
                    var errorMessage = $"Payment Provider is temporarily unreachable due to local network/DNS issues. " +
                        $"Hostname '{hostname}' cannot be resolved. " +
                        $"If DNS resolution is failing, add this line to Windows Hosts file (C:\\Windows\\System32\\drivers\\etc\\hosts): " +
                        $"213.226.118.91 sandbox-api.iyzipay.com";
                    _logger.LogError("Iyzico API bağlantı hatası (nested - HostNotFound): {ErrorMessage}", errorMessage);
                    throw new PaymentProviderUnreachableException(
                        errorMessage,
                        httpEx,
                        hostname,
                        socketEx.ErrorCode,
                        "DNS resolution failed");
                }
                var friendlyMessage = $"Iyzico API'sine bağlanılamıyor (Ağ hatası: {socketEx.SocketErrorCode}).";
                var errorMessageGeneral = $"{friendlyMessage} " +
                    $"(Hedef URL: {baseUrlForLog}, SocketErrorCode: {socketEx.SocketErrorCode}, ErrorCode: {socketEx.ErrorCode}).";
                _logger.LogError("Iyzico API bağlantı hatası (nested): {ErrorMessage}", errorMessageGeneral);
                throw new PaymentProviderUnreachableException(
                    errorMessageGeneral,
                    httpEx,
                    hostname,
                    socketEx.ErrorCode,
                    "Network error");
            }
            var userFriendlyMessage = httpEx.InnerException != null 
                ? $"Iyzico API'sine bağlanılamıyor. Ağ hatası: {httpEx.InnerException.GetType().Name} - {httpEx.InnerException.Message}"
                : $"Iyzico API'sine bağlanılamıyor. HTTP hatası: {httpEx.Message}";
            _logger.LogError("Iyzico API HTTP hatası: {Message}, URL: {BaseUrl}", userFriendlyMessage, baseUrlForLog);
            throw new InvalidOperationException(
                $"{userFriendlyMessage} (Hedef URL: {baseUrlForLog}). " +
                $"Lütfen internet bağlantınızı, DNS ayarlarınızı ve firewall/proxy yapılandırmanızı kontrol edin.", httpEx);
        }
        catch (System.AggregateException aggEx)
        {
            var baseUrlForLog = baseUrlForRequest;
            System.Net.Http.HttpRequestException? httpEx = null;
            System.Net.Sockets.SocketException? socketEx = null;
            if (aggEx.InnerException is System.Net.Http.HttpRequestException innerHttpEx)
            {
                httpEx = innerHttpEx;
                socketEx = innerHttpEx.InnerException as System.Net.Sockets.SocketException;
            }
            else if (aggEx.InnerExceptions != null)
            {
                httpEx = aggEx.InnerExceptions.OfType<System.Net.Http.HttpRequestException>().FirstOrDefault();
                socketEx = httpEx?.InnerException as System.Net.Sockets.SocketException;
            }
            _logger.LogError(aggEx, 
                "Iyzico API'sine bağlantı hatası (AggregateException): UserId={UserId}, BaseUrl='{BaseUrl}', " +
                "InnerExceptionCount={InnerExceptionCount}, InnerExceptionType={InnerExceptionType}", 
                userId, baseUrlForLog, aggEx.InnerExceptions?.Count ?? (aggEx.InnerException != null ? 1 : 0), 
                aggEx.InnerException?.GetType().Name ?? "NULL");
            if (aggEx.InnerExceptions != null && aggEx.InnerExceptions.Count > 0)
            {
                var exceptionIndex = 0;
                foreach (var innerEx in aggEx.InnerExceptions)
                {
                    _logger.LogError("AggregateException InnerException[{Index}]: Type={Type}, Message={Message}, " +
                        "InnerExceptionType={InnerInnerExceptionType}, StackTrace={StackTrace}", 
                        exceptionIndex++, innerEx.GetType().Name, innerEx.Message, 
                        innerEx.InnerException?.GetType().Name ?? "NULL",
                        innerEx.StackTrace ?? "NULL");
                }
            }
            else if (aggEx.InnerException != null)
            {
                _logger.LogError("AggregateException InnerException: Type={Type}, Message={Message}, " +
                    "InnerExceptionType={InnerInnerExceptionType}", 
                    aggEx.InnerException.GetType().Name, aggEx.InnerException.Message,
                    aggEx.InnerException.InnerException?.GetType().Name ?? "NULL");
            }
            if (socketEx != null)
            {
                var hostname = ExtractHostnameFromUrl(baseUrlForLog);
                _logger.LogError(
                    "SocketException detayları (AggregateException unwrapped): SocketErrorCode={SocketErrorCode}, " +
                    "ErrorCode={ErrorCode}, NativeErrorCode={NativeErrorCode}, Message={Message}", 
                    socketEx.SocketErrorCode, socketEx.ErrorCode, socketEx.NativeErrorCode, socketEx.Message);
                if (socketEx.SocketErrorCode == System.Net.Sockets.SocketError.HostNotFound || socketEx.ErrorCode == 11001)
                {
                    var errorMessage = $"Payment Provider is temporarily unreachable due to local network/DNS issues. " +
                        $"Hostname '{hostname}' cannot be resolved. " +
                        $"If DNS resolution is failing, add this line to Windows Hosts file (C:\\Windows\\System32\\drivers\\etc\\hosts): " +
                        $"213.226.118.91 sandbox-api.iyzipay.com";
                    _logger.LogError("Iyzico API bağlantı hatası (AggregateException - HostNotFound): {ErrorMessage}", errorMessage);
                    throw new PaymentProviderUnreachableException(
                        errorMessage,
                        aggEx,
                        hostname,
                        socketEx.ErrorCode,
                        "DNS resolution failed");
                }
                var friendlyMessage = $"Iyzico API'sine bağlanılamıyor (Ağ hatası: {socketEx.SocketErrorCode}).";
                var errorMessageGeneral = $"{friendlyMessage} (Hedef URL: {baseUrlForLog}, SocketErrorCode: {socketEx.SocketErrorCode}, ErrorCode: {socketEx.ErrorCode}).";
                _logger.LogError("Iyzico API bağlantı hatası (AggregateException unwrapped): {ErrorMessage}", errorMessageGeneral);
                throw new PaymentProviderUnreachableException(
                    errorMessageGeneral,
                    aggEx,
                    hostname,
                    socketEx.ErrorCode,
                    "Network error");
            }
            if (httpEx != null)
            {
                var userFriendlyMessage = httpEx.InnerException != null 
                    ? $"Iyzico API'sine bağlanılamıyor. Ağ hatası: {httpEx.InnerException.GetType().Name} - {httpEx.InnerException.Message}"
                    : $"Iyzico API'sine bağlanılamıyor. HTTP hatası: {httpEx.Message}";
                _logger.LogError("Iyzico API bağlantı hatası (AggregateException - HttpRequestException): {Message}, URL: {BaseUrl}", 
                    userFriendlyMessage, baseUrlForLog);
                throw new InvalidOperationException(
                    $"{userFriendlyMessage} (Hedef URL: {baseUrlForLog}). " +
                    $"Lütfen internet bağlantınızı, DNS ayarlarınızı ve firewall/proxy yapılandırmanızı kontrol edin.", httpEx);
            }
            var generalMessage = $"Iyzico API'sine bağlanılamıyor. AggregateException: {aggEx.Message}";
            _logger.LogError("Iyzico API bağlantı hatası (AggregateException - genel): {Message}, URL: {BaseUrl}", 
                generalMessage, baseUrlForLog);
            throw new InvalidOperationException(
                $"{generalMessage} (Hedef URL: {baseUrlForLog}). " +
                $"Lütfen internet bağlantınızı, DNS ayarlarınızı ve firewall/proxy yapılandırmanızı kontrol edin.", aggEx);
        }
        catch (System.Exception ex)
        {
            var baseUrlForLog = _options.BaseUrl?.Trim() ?? "NULL";
            _logger.LogError(ex, 
                "Iyzico API çağrısında beklenmeyen hata: UserId={UserId}, BaseUrl='{BaseUrl}', ExceptionType={ExceptionType}", 
                userId, baseUrlForLog, ex.GetType().Name);
            throw;
        }
        if (checkout == null)
        {
            _logger.LogError("Iyzico'dan null yanıt döndü: UserId={UserId}, Amount={Amount}", userId, amount);
            throw new InvalidOperationException("Iyzico'dan yanıt alınamadı. CheckoutFormInitialize null döndü.");
        }
        _logger.LogInformation("Iyzico yanıtı alındı: Status={Status}, ErrorMessage={ErrorMessage}, ErrorCode={ErrorCode}, Token={Token}", 
            checkout.Status, checkout.ErrorMessage, checkout.ErrorCode, checkout.Token);
        if (!string.IsNullOrEmpty(checkout.Status) && checkout.Status.ToLower() != "success")
        {
            var errorMessage = checkout.ErrorMessage ?? checkout.ErrorCode ?? "Bilinmeyen hata";
            _logger.LogError("Iyzico ödeme başlatma hatası: UserId={UserId}, Status={Status}, ErrorMessage={ErrorMessage}, ErrorCode={ErrorCode}", 
                userId, checkout.Status, checkout.ErrorMessage, checkout.ErrorCode);
            throw new InvalidOperationException($"Iyzico ödeme başlatma hatası: {errorMessage} (Status: {checkout.Status})");
        }
        if (string.IsNullOrEmpty(checkout.CheckoutFormContent) || string.IsNullOrEmpty(checkout.Token))
        {
            var errorInfo = $"CheckoutFormContent: {(string.IsNullOrEmpty(checkout.CheckoutFormContent) ? "null/empty" : "OK")}, " +
                           $"Token: {(string.IsNullOrEmpty(checkout.Token) ? "null/empty" : "OK")}, " +
                           $"Status: {checkout.Status}, " +
                           $"ErrorMessage: {checkout.ErrorMessage}, " +
                           $"ErrorCode: {checkout.ErrorCode}";
            _logger.LogError("Iyzico ödeme formu eksik veri ile döndü: UserId={UserId}, {ErrorInfo}", userId, errorInfo);
            throw new InvalidOperationException($"Iyzico ödeme formu oluşturulamadı. {errorInfo}");
        }
        _logger.LogInformation("Iyzico ödeme formu başarıyla oluşturuldu: UserId={UserId}, Token={Token}", userId, checkout.Token);
        return checkout;
    }
    public async Task<CheckoutForm> RetrievePaymentAsync(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            _logger.LogError("RetrievePaymentAsync: Token boş veya null");
            throw new ArgumentException("Token parametresi boş olamaz", nameof(token));
        }
        var baseUrlForLog = _options.BaseUrl?.Trim() ?? "NULL";
        _logger.LogInformation("Iyzico ödeme sonucu sorgulanıyor: Token={Token}, BaseUrl='{BaseUrl}'", token, baseUrlForLog);
        try
        {
            var req = new RetrieveCheckoutFormRequest { Token = token };
            var checkout = await CheckoutForm.Retrieve(req, _options).ConfigureAwait(false);
            if (checkout == null)
            {
                _logger.LogError("Iyzico'dan null yanıt döndü: Token={Token}, BaseUrl='{BaseUrl}'", token, baseUrlForLog);
                throw new InvalidOperationException("Iyzico'dan ödeme sonucu alınamadı");
            }
            _logger.LogInformation(
                "Iyzico ödeme sonucu alındı: Token={Token}, PaymentStatus={PaymentStatus}, PaymentId={PaymentId}, ConversationId={ConversationId}", 
                token, checkout.PaymentStatus, checkout.PaymentId, checkout.ConversationId);
            return checkout;
        }
        catch (System.Net.Http.HttpRequestException httpEx)
        {
            _logger.LogError(httpEx, 
                "Iyzico RetrievePaymentAsync HTTP isteği başarısız: Token={Token}, BaseUrl='{BaseUrl}', " +
                "HttpRequestException.Message={HttpExceptionMessage}, InnerExceptionType={InnerExceptionType}, InnerExceptionMessage={InnerExceptionMessage}", 
                token, baseUrlForLog, httpEx.Message, 
                httpEx.InnerException?.GetType().Name ?? "NULL", 
                httpEx.InnerException?.Message ?? "NULL");
            if (httpEx.InnerException is System.Net.Sockets.SocketException socketEx)
            {
                _logger.LogError(
                    "SocketException detayları (RetrievePaymentAsync): SocketErrorCode={SocketErrorCode}, ErrorCode={ErrorCode}, " +
                    "NativeErrorCode={NativeErrorCode}, Message={Message}", 
                    socketEx.SocketErrorCode, socketEx.ErrorCode, socketEx.NativeErrorCode, socketEx.Message);
                throw new InvalidOperationException(
                    $"Iyzico API'sine bağlanılamıyor (RetrievePaymentAsync). DNS çözümleme veya ağ bağlantısı sorunu. " +
                    $"Hedef URL: '{baseUrlForLog}'. " +
                    $"SocketErrorCode: {socketEx.SocketErrorCode}, ErrorCode: {socketEx.ErrorCode}. " +
                    $"Hata: {socketEx.Message}.", httpEx);
            }
            throw new InvalidOperationException(
                $"Iyzico API'sine bağlanılamıyor (RetrievePaymentAsync). HTTP İstek Hatası: {httpEx.Message}. " +
                $"Hedef URL: '{baseUrlForLog}'.", httpEx);
        }
        catch (System.AggregateException aggEx)
        {
            System.Net.Http.HttpRequestException? httpEx = null;
            System.Net.Sockets.SocketException? socketEx = null;
            if (aggEx.InnerException is System.Net.Http.HttpRequestException innerHttpEx)
            {
                httpEx = innerHttpEx;
                socketEx = innerHttpEx.InnerException as System.Net.Sockets.SocketException;
            }
            else if (aggEx.InnerExceptions != null)
            {
                httpEx = aggEx.InnerExceptions.OfType<System.Net.Http.HttpRequestException>().FirstOrDefault();
                socketEx = httpEx?.InnerException as System.Net.Sockets.SocketException;
            }
            _logger.LogError(aggEx, 
                "Iyzico RetrievePaymentAsync AggregateException: Token={Token}, BaseUrl='{BaseUrl}', InnerExceptionCount={InnerExceptionCount}", 
                token, baseUrlForLog, aggEx.InnerExceptions?.Count ?? (aggEx.InnerException != null ? 1 : 0));
            if (aggEx.InnerExceptions != null && aggEx.InnerExceptions.Count > 0)
            {
                var exceptionIndex = 0;
                foreach (var innerEx in aggEx.InnerExceptions)
                {
                    _logger.LogError("RetrievePaymentAsync AggregateException InnerException[{Index}]: Type={Type}, Message={Message}", 
                        exceptionIndex++, innerEx.GetType().Name, innerEx.Message);
                }
            }
            else if (aggEx.InnerException != null)
            {
                _logger.LogError("RetrievePaymentAsync AggregateException InnerException: Type={Type}, Message={Message}", 
                    aggEx.InnerException.GetType().Name, aggEx.InnerException.Message);
            }
            if (socketEx != null)
            {
                _logger.LogError(
                    "SocketException detayları (RetrievePaymentAsync - AggregateException unwrapped): SocketErrorCode={SocketErrorCode}, ErrorCode={ErrorCode}, Message={Message}", 
                    socketEx.SocketErrorCode, socketEx.ErrorCode, socketEx.Message);
                var friendlyMessage = socketEx.SocketErrorCode == System.Net.Sockets.SocketError.HostNotFound 
                    ? "Iyzico API sunucusu bulunamadı (DNS hatası). Lütfen internet bağlantınızı kontrol edin ve DNS ayarlarınızı doğrulayın."
                    : $"Iyzico API'sine bağlanılamıyor (Ağ hatası: {socketEx.SocketErrorCode}). Lütfen internet bağlantınızı ve firewall ayarlarınızı kontrol edin.";
                throw new InvalidOperationException(
                    $"{friendlyMessage} (RetrievePaymentAsync, Hedef URL: {baseUrlForLog}, SocketErrorCode: {socketEx.SocketErrorCode}, ErrorCode: {socketEx.ErrorCode}). " +
                    $"DNS testi için: nslookup sandbox-api.iyzipay.com komutunu çalıştırın.", socketEx);
            }
            if (httpEx != null)
            {
                var userFriendlyMessage = httpEx.InnerException != null 
                    ? $"Iyzico API'sine bağlanılamıyor (RetrievePaymentAsync). Ağ hatası: {httpEx.InnerException.GetType().Name} - {httpEx.InnerException.Message}"
                    : $"Iyzico API'sine bağlanılamıyor (RetrievePaymentAsync). HTTP hatası: {httpEx.Message}";
                throw new InvalidOperationException(
                    $"{userFriendlyMessage} (Hedef URL: {baseUrlForLog}). " +
                    $"Lütfen internet bağlantınızı, DNS ayarlarınızı ve firewall/proxy yapılandırmanızı kontrol edin.", httpEx);
            }
            throw new InvalidOperationException(
                $"Iyzico API'sine bağlanılamıyor (RetrievePaymentAsync). AggregateException: {aggEx.Message}. " +
                $"Hedef URL: '{baseUrlForLog}'.", aggEx);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, 
                "Iyzico ödeme sonucu sorgulanırken beklenmeyen hata: Token={Token}, BaseUrl='{BaseUrl}', ExceptionType={ExceptionType}", 
                token, baseUrlForLog, ex.GetType().Name);
            throw;
        }
    }
    private static string SanitizeBaseUrl(string baseUrl)
    {
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            return baseUrl;
        }
        baseUrl = baseUrl.Trim();
        baseUrl = baseUrl.Replace("\u200B", "");
        baseUrl = baseUrl.Replace("\uFEFF", "");
        baseUrl = baseUrl.Replace("\n", "");
        baseUrl = baseUrl.Replace("\r", "");
        baseUrl = baseUrl.Replace("\t", "");
        if (!baseUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
            !baseUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            baseUrl = "https://" + baseUrl;
        }
        baseUrl = baseUrl.TrimEnd('/');
        return baseUrl;
    }
    private static void ValidateBaseUrl(string baseUrl)
    {
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            return;
        }
        var invalidChars = new[] { '\n', '\r', '\t', ' ', '\u200B', '\uFEFF' };
        foreach (var invalidChar in invalidChars)
        {
            if (baseUrl.Contains(invalidChar))
            {
                throw new InvalidOperationException(
                    $"Iyzico BaseUrl geçersiz karakter içeriyor: '{invalidChar}' (Unicode: {(int)invalidChar:X4}). " +
                    $"DNS resolution'ı bozabilecek karakterler URL'de olmamalı. " +
                    $"BaseUrl: '{baseUrl}'");
            }
        }
        try
        {
            var uri = new Uri(baseUrl);
            var hostname = uri.Host;
            if (!System.Text.RegularExpressions.Regex.IsMatch(hostname, @"^[a-zA-Z0-9.-]+$"))
            {
                throw new InvalidOperationException(
                    $"Iyzico BaseUrl hostname'i geçersiz karakterler içeriyor: '{hostname}'. " +
                    $"Hostname sadece alphanumeric karakterler, nokta ve tire içermelidir. " +
                    $"BaseUrl: '{baseUrl}'");
            }
        }
        catch (UriFormatException)
        {
        }
    }
    
    private static string ExtractHostnameFromUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return string.Empty;
        }
        try
        {
            var uri = new Uri(url);
            return uri.Host;
        }
        catch
        {
            var cleaned = url.Trim().TrimEnd('/');
            if (cleaned.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
            {
                cleaned = cleaned.Substring(7);
            }
            else if (cleaned.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                cleaned = cleaned.Substring(8);
            }
            var slashIndex = cleaned.IndexOf('/');
            if (slashIndex > 0)
            {
                cleaned = cleaned.Substring(0, slashIndex);
            }
            return cleaned;
        }
    }
}
