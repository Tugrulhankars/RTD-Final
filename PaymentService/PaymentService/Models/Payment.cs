namespace PaymentService.Models;

public class Payment
{
    public Guid Id { get; set; }                     // Payment Service iç ID
    public int UserId { get; set; }               // Hangi kullanıcıya ait
    public int AccountId { get; set; }
    public decimal Amount { get; set; }              // Ödeme miktarı
    public string Currency { get; set; }             // TRY, USD vb.
    public string PaymentMethod { get; set; }        // CreditCard, EFT, vs.
    public string PaymentTransactionId { get; set; } // Iyzipay veya gateway transaction ID
    public string Status { get; set; }               // "Pending", "Success", "Failed"
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

