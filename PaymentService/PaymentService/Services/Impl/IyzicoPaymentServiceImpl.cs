using Iyzipay;
using Iyzipay.Model;
using Iyzipay.Request;
using Microsoft.Extensions.Configuration;
using PaymentService.Dtos.Request;
using PaymentService.Dtos.Response;
using PaymentService.Repositories;
using System.Globalization;

namespace PaymentService.Services.Impl;

public class IyzicoPaymentServiceImpl : IIyzicoPaymentService
{
    private readonly IPaymentRepository _paymentRepository;
    private readonly Options _options;
    private readonly IConfiguration _configuration;

    public IyzicoPaymentServiceImpl(IPaymentRepository paymentRepository, Options opts, IConfiguration configuration)
    {
        _paymentRepository = paymentRepository;
        _configuration = configuration;
        _options = new Options
        {
            ApiKey = opts.ApiKey,
            SecretKey = opts.SecretKey,
            BaseUrl = opts.BaseUrl
        };
    }

    public PaymentResponse Pay(PaymentRequest paymentRequest)
    {
        Options options = new()
        {
            ApiKey = "sandbox-jJ9iwVPKmLVPhHy9quhLMsdqvDLQY0J9",
            SecretKey = "sandbox-q4dk0SrgBiNf9mr2zCCU5PuHQwMYGxKv",
            BaseUrl = "https://sandbox-api.iyzipay.com"
        };

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

        //ThreedsInitialize threedsInitialize = ThreedsInitialize.Create(request, options);
        //ThreedsInitialize threedsInitialize = ThreedsInitialize.Create(request,options);

        return new PaymentResponse("");
        
    }

    // async versiyon — derleyici artık Task döndüğünü bilir
    public async Task<CheckoutFormInitialize> CreatePaymentAsync(decimal amount, string email, int userId)
    {
        // Email'i conversationId'ye ekle (callback'te parse edilebilmesi için)
        // Format: user-{userId}-email-{emailBase64}-{guid}
        var emailBase64 = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(email ?? ""));
        var conversationId = $"user-{userId}-email-{emailBase64}-{Guid.NewGuid()}";
        
        var request = new CreateCheckoutFormInitializeRequest
        {
            Locale = Locale.TR.ToString(),
            ConversationId = conversationId,
            Price = amount.ToString("F2", CultureInfo.InvariantCulture),
            PaidPrice = amount.ToString("F2", CultureInfo.InvariantCulture),
            Currency = Currency.TRY.ToString(),
            BasketId = $"BASKET_{userId}",
            CallbackUrl = _configuration["Iyzico:CallbackUrl"] ?? "https://localhost:7009/api/payment/iyzico/callback",
            PaymentGroup = PaymentGroup.PRODUCT.ToString()
        };

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

        // **ÖNEMLİ**: Create() metodu SDK'da Task döndürüyorsa await et.
        // Bazı SDK sürümlerinde metot adı CreateAsync olabilir; IntelliSense'e bak.
        var checkout = await CheckoutFormInitialize.Create(request, _options);
        return checkout;
    }

    public async Task<CheckoutForm> RetrievePaymentAsync(string token)
    {
        var req = new RetrieveCheckoutFormRequest { Token = token };
        var checkout = await CheckoutForm.Retrieve(req, _options); // await ile Task çözülür
        return checkout;
    }


}
