using Iyzipay.Model;
using PaymentService.Dtos.Request;
using PaymentService.Dtos.Response;

namespace PaymentService.Services;

public interface IIyzicoPaymentService
{
    PaymentResponse Pay(PaymentRequest  request);
    Task<CheckoutForm> RetrievePaymentAsync(string token);
    Task<CheckoutFormInitialize> CreatePaymentAsync(decimal amount, string email, int userId);
}
