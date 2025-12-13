namespace PaymentService.Events;

public class PaymentFailedEvent
{
    public int UserId { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "TRY";
    public string PaymentTransactionId { get; set; }
    public string PaymentMethod { get; set; }
    public DateTime PaymentDate { get; set; }
    public string Email { get; set; }
    public string Status { get; set; } = "FAILED";
    public string FailureReason { get; set; }
    public string? ErrorCode { get; set; }
    public string? ErrorMessage { get; set; }
}

