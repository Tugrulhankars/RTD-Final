namespace PaymentService.Dtos.Request;

public class PaymentRequest
{
    public int UserId { get; set; }               // Hangi kullanıcıya ait
    public int AccountId { get; set; }
    public decimal Amount { get; set; }              // Ödeme miktarı
    public string Currency { get; set; }             // TRY, USD vb.
    public string PaymentMethod { get; set; }        // CreditCard, EFT, vs.
    public string Status { get; set; }               // "Pending", "Success", "Failed"
   
}
