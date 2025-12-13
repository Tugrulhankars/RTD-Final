using Iyzipay;
using Iyzipay.Model;
using Iyzipay.Request;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using PaymentService.Dtos.Request;
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
    private readonly IAccountService _accountService;
    private readonly ILogger<PaymentController> _logger;

    public PaymentController(
        IIyzicoPaymentService iyzico, 
        IRabbitMQPublisher rabbitMQPublisher,
        IPaymentRepository paymentRepository,
        IAccountService accountService,
        ILogger<PaymentController> logger)
    {
        _iyzico = iyzico;
        _rabbitMQPublisher = rabbitMQPublisher;
        _paymentRepository = paymentRepository;
        _accountService = accountService;
        _logger = logger;
    }

    [HttpPost("create")]
    public async Task<IActionResult> Create([FromBody] CreatePaymentDto dto)
    {
        var checkout = await _iyzico.CreatePaymentAsync(dto.Amount, dto.Email, dto.UserId);
        return Ok(new { checkoutFormContent = checkout.CheckoutFormContent, token = checkout.Token, conversationId = checkout.ConversationId });
    }


    [HttpPost("iyzico/callback")]
    public async Task<IActionResult> Callback([FromForm] string token)
    {
        try
        {
            var checkout = await _iyzico.RetrievePaymentAsync(token);
            
            // ConversationId format: user-{userId}-email-{emailBase64}-{guid}
            var parts = checkout.ConversationId?.Split('-') ?? Array.Empty<string>();
            int userId = 0;
            string email = "";
            
            if (parts.Length >= 2 && int.TryParse(parts[1], out var id))
            {
                userId = id;
            }
            
            // Email'i conversationId'den parse et
            if (parts.Length >= 4 && parts[2] == "email")
            {
                try
                {
                    var emailBase64 = parts[3];
                    var emailBytes = Convert.FromBase64String(emailBase64);
                    email = System.Text.Encoding.UTF8.GetString(emailBytes);
                }
                catch
                {
                    // Email parse edilemezse boş string kalır
                    email = "";
                }
            }
            
            decimal amount = decimal.Parse(checkout.PaidPrice ?? "0", CultureInfo.InvariantCulture);
            string paymentTransactionId = checkout.PaymentId ?? "";
            string paymentMethod = checkout.PaymentItems?.FirstOrDefault()?.PaymentTransactionId ?? "CreditCard";

            if (userId == 0)
            {
                // Başarısız ödeme event'i gönder
                await SendPaymentFailedEventAsync(userId, amount, email, paymentTransactionId, paymentMethod, "Geçersiz kullanıcı ID'si");
                return BadRequest("Geçersiz kullanıcı ID'si");
            }

            if (checkout.PaymentStatus == "SUCCESS")
            {
                // AccountService'den kullanıcının hesabını al (HTTP - sadece hesap bilgisi için)
                var httpClientFactory = HttpContext.RequestServices.GetRequiredService<IHttpClientFactory>();
                var accountClient = httpClientFactory.CreateClient("AccountService");

                try
                {
                    // Önce kullanıcının hesabını bul (HTTP ile)
                    var accountResponse = await accountClient.GetAsync($"/api/account/getAccountByUser/{userId}");
                    
                    if (!accountResponse.IsSuccessStatusCode)
                    {
                        await SendPaymentFailedEventAsync(userId, amount, email, paymentTransactionId, paymentMethod, "Kullanıcı hesabı bulunamadı");
                        return StatusCode(500, "Kullanıcı hesabı bulunamadı");
                    }

                    var accountJson = await accountResponse.Content.ReadAsStringAsync();
                    var account = System.Text.Json.JsonSerializer.Deserialize<AccountResponse>(accountJson, new System.Text.Json.JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    if (account == null)
                    {
                        await SendPaymentFailedEventAsync(userId, amount, email, paymentTransactionId, paymentMethod, "Hesap bilgisi alınamadı");
                        return StatusCode(500, "Hesap bilgisi alınamadı");
                    }

                    // gRPC ile bakiye güncelle (pozitif amount = bakiye artır)
                    try
                    {
                        var balanceUpdated = await _accountService.UpdateAccountBalanceAsync(
                            accountId: account.AccountId,
                            userId: userId,
                            firstName: account.FirstName ?? "",
                            lastName: account.LastName ?? "",
                            amount: (double)amount // Pozitif değer = bakiye artır
                        );

                        if (balanceUpdated)
                        {
                            // Ödeme kaydı oluştur
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

                            await _paymentRepository.CreatePayment(payment);

                            // Başarılı ödeme event'i gönder
                            await SendPaymentSuccessEventAsync(userId, account.AccountId, amount, email, paymentTransactionId, paymentMethod, "Ödeme başarılı ve bakiye gRPC ile güncellendi");

                            _logger.LogInformation("Ödeme başarılı ve bakiye gRPC ile güncellendi: UserId={UserId}, AccountId={AccountId}, Amount={Amount}, TransactionId={TransactionId}", 
                                userId, account.AccountId, amount, paymentTransactionId);

                            return Ok(new { message = "Ödeme başarılı ve bakiye güncellendi", userId, accountId = account.AccountId, amount });
                        }
                        else
                        {
                            await SendPaymentFailedEventAsync(userId, amount, email, paymentTransactionId, paymentMethod, "gRPC ile bakiye güncellenemedi");
                            return StatusCode(500, "Bakiye güncellenemedi");
                        }
                    }
                    catch (Exception grpcEx)
                    {
                        _logger.LogError(grpcEx, "gRPC AccountService bakiye güncelleme hatası: UserId={UserId}, AccountId={AccountId}", 
                            userId, account.AccountId);
                        await SendPaymentFailedEventAsync(userId, amount, email, paymentTransactionId, paymentMethod, $"gRPC bakiye güncelleme hatası: {grpcEx.Message}");
                        return StatusCode(500, $"Bakiye güncellenemedi: {grpcEx.Message}");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "AccountService ile iletişim hatası: UserId={UserId}", userId);
                    await SendPaymentFailedEventAsync(userId, amount, email, paymentTransactionId, paymentMethod, $"AccountService ile iletişim hatası: {ex.Message}");
                    return StatusCode(500, $"AccountService ile iletişim hatası: {ex.Message}");
                }
            }
            else
            {
                // Ödeme başarısız - event gönder
                string failureReason = checkout.ErrorMessage ?? checkout.Status ?? "Ödeme başarısız";
                await SendPaymentFailedEventAsync(userId, amount, email, paymentTransactionId, paymentMethod, failureReason, checkout.ErrorCode);

                _logger.LogWarning("Ödeme başarısız: UserId={UserId}, Amount={Amount}, Reason={Reason}", 
                    userId, amount, failureReason);

                return BadRequest(new { message = "Ödeme başarısız", reason = failureReason });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Callback işlenirken hata oluştu");
            return StatusCode(500, $"Callback işlenirken hata oluştu: {ex.Message}");
        }
    }

    private async Task SendPaymentSuccessEventAsync(int userId, int accountId, decimal amount, string email, string paymentTransactionId, string paymentMethod, string message)
    {
        try
        {
            var successEvent = new PaymentSuccessEvent
            {
                UserId = userId,
                AccountId = accountId,
                Amount = amount,
                Currency = "TRY",
                PaymentTransactionId = paymentTransactionId,
                PaymentMethod = paymentMethod,
                PaymentDate = DateTime.UtcNow,
                Email = email,
                Status = "SUCCESS",
                Message = message
            };

            await _rabbitMQPublisher.PublishAsync(successEvent, "payment-notifications");
            _logger.LogInformation("PaymentSuccessEvent gönderildi: UserId={UserId}, Amount={Amount}", userId, amount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "PaymentSuccessEvent gönderilirken hata oluştu: UserId={UserId}", userId);
            // Event gönderilemese bile ödeme işlemi başarılı olduğu için exception fırlatmıyoruz
        }
    }

    private async Task SendPaymentFailedEventAsync(int userId, decimal amount, string email, string paymentTransactionId, string paymentMethod, string failureReason, string? errorCode = null)
    {
        try
        {
            var failedEvent = new PaymentFailedEvent
            {
                UserId = userId,
                Amount = amount,
                Currency = "TRY",
                PaymentTransactionId = paymentTransactionId,
                PaymentMethod = paymentMethod,
                PaymentDate = DateTime.UtcNow,
                Email = email,
                Status = "FAILED",
                FailureReason = failureReason,
                ErrorCode = errorCode,
                ErrorMessage = failureReason
            };

            await _rabbitMQPublisher.PublishAsync(failedEvent, "payment-notifications");
            _logger.LogInformation("PaymentFailedEvent gönderildi: UserId={UserId}, Amount={Amount}, Reason={Reason}", 
                userId, amount, failureReason);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "PaymentFailedEvent gönderilirken hata oluştu: UserId={UserId}", userId);
            // Event gönderilemese bile log kaydı yapıldığı için exception fırlatmıyoruz
        }
    }

    private class AccountResponse
    {
        public int AccountId { get; set; }
        public double Balance { get; set; }
        public string AccountStatus { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
    }

    //[HttpGet]
    //public async Task<IActionResult> Pay()
    //{
    //    Options options = new()
    //    {
    //        ApiKey = "sandbox-jJ9iwVPKmLVPhHy9quhLMsdqvDLQY0J9",
    //        SecretKey = "sandbox-q4dk0SrgBiNf9mr2zCCU5PuHQwMYGxKv",
    //        BaseUrl = "https://sandbox-api.iyzipay.com"
    //    };

    //    CreatePaymentRequest request = new CreatePaymentRequest();
    //    request.Locale = Locale.TR.ToString();
    //    request.ConversationId = Guid.NewGuid().ToString();
    //    request.Price = "1";
    //    request.PaidPrice = "1.2";
    //    request.Currency = Currency.TRY.ToString();
    //    request.Installment = 1;
    //    request.BasketId = "B67832";
    //    request.PaymentChannel = PaymentChannel.WEB.ToString();
    //    request.PaymentGroup = PaymentGroup.PRODUCT.ToString();
    //    request.CallbackUrl = "https://localhost:7224/api/Payments/PayCallBack";

    //    PaymentCard paymentCard = new PaymentCard();
    //    paymentCard.CardHolderName = "John Doe";
    //    paymentCard.CardNumber = "5528790000000008";
    //    paymentCard.ExpireMonth = "12";
    //    paymentCard.ExpireYear = "2030";
    //    paymentCard.Cvc = "123";
    //    paymentCard.RegisterCard = 0;
    //    request.PaymentCard = paymentCard;

    //    Buyer buyer = new Buyer();
    //    buyer.Id = "BY789";
    //    buyer.Name = "John";
    //    buyer.Surname = "Doe";
    //    buyer.GsmNumber = "+905350000000";
    //    buyer.Email = "email@email.com";
    //    buyer.IdentityNumber = "74300864791";
    //    buyer.LastLoginDate = "2015-10-05 12:43:35";
    //    buyer.RegistrationDate = "2013-04-21 15:12:09";
    //    buyer.RegistrationAddress = "Nidakule Göztepe, Merdivenköy Mah. Bora Sok. No:1";
    //    buyer.Ip = "85.34.78.112";
    //    buyer.City = "Istanbul";
    //    buyer.Country = "Turkey";
    //    buyer.ZipCode = "34732";
    //    request.Buyer = buyer;

    //    Address shippingAddress = new Address();
    //    shippingAddress.ContactName = "Jane Doe";
    //    shippingAddress.City = "Istanbul";
    //    shippingAddress.Country = "Turkey";
    //    shippingAddress.Description = "Nidakule Göztepe, Merdivenköy Mah. Bora Sok. No:1";
    //    shippingAddress.ZipCode = "34742";
    //    request.ShippingAddress = shippingAddress;

    //    Address billingAddress = new Address();
    //    billingAddress.ContactName = "Jane Doe";
    //    billingAddress.City = "Istanbul";
    //    billingAddress.Country = "Turkey";
    //    billingAddress.Description = "Nidakule Göztepe, Merdivenköy Mah. Bora Sok. No:1";
    //    billingAddress.ZipCode = "34742";
    //    request.BillingAddress = billingAddress;

    //    List<BasketItem> basketItems = new List<BasketItem>();
    //    BasketItem firstBasketItem = new BasketItem();
    //    firstBasketItem.Id = "BI101";
    //    firstBasketItem.Name = "Binocular";
    //    firstBasketItem.Category1 = "Collectibles";
    //    firstBasketItem.Category2 = "Accessories";
    //    firstBasketItem.ItemType = BasketItemType.PHYSICAL.ToString();
    //    firstBasketItem.Price = "0.3";
    //    basketItems.Add(firstBasketItem);

    //    BasketItem secondBasketItem = new BasketItem();
    //    secondBasketItem.Id = "BI102";
    //    secondBasketItem.Name = "Game code";
    //    secondBasketItem.Category1 = "Game";
    //    secondBasketItem.Category2 = "Online Game Items";
    //    secondBasketItem.ItemType = BasketItemType.VIRTUAL.ToString();
    //    secondBasketItem.Price = "0.5";
    //    basketItems.Add(secondBasketItem);

    //    BasketItem thirdBasketItem = new BasketItem();
    //    thirdBasketItem.Id = "BI103";
    //    thirdBasketItem.Name = "Usb";
    //    thirdBasketItem.Category1 = "Electronics";
    //    thirdBasketItem.Category2 = "Usb / Cable";
    //    thirdBasketItem.ItemType = BasketItemType.PHYSICAL.ToString();
    //    thirdBasketItem.Price = "0.2";
    //    basketItems.Add(thirdBasketItem);
    //    request.BasketItems = basketItems;

    //    //ThreedsInitialize threedsInitialize = ThreedsInitialize.Create(request, options);

    //    return Ok(new { Content = threedsInitialize.HtmlContent, ConversationId = request.ConversationId });
    //}

    [HttpPost]
    public async Task<IActionResult> PayCallBack([FromForm] IFormCollection collections)
    {
        CallbackData data = new(
            Status: collections["status"],
            PaymentId: collections["paymentId"],
            ConversationData: collections["conversationData"],
            ConversationId: collections["conversationId"],
            MDStatus: collections["mdStatus"]);

        if (data.Status != "success")
        {
            return BadRequest("Ödeme başarısız oldu!");
        }

        //await _hubContext.Clients.Client(PayHub.TransactionConnections[data.ConversationId]).SendAsync("Receive", data);

        return Ok();
    }
}


public sealed record CallbackData(
    string Status,
    string PaymentId,
    string ConversationData,
    string ConversationId,
    string MDStatus);