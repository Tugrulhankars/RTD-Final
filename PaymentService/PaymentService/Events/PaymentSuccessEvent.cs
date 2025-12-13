namespace PaymentService.Events;

public class PaymentSuccessEvent
{
    public int UserId { get; set; }
    public int AccountId { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "TRY";
    public string PaymentTransactionId { get; set; }
    public string PaymentMethod { get; set; }
    public DateTime PaymentDate { get; set; }
    public string Email { get; set; }
    public string Status { get; set; } = "SUCCESS";
    public string? Message { get; set; }
}

