namespace PaymentService.Dtos.Request;
public class PaymentRequest
{
    public int UserId { get; set; }
    public int AccountId { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public string PaymentMethod { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}
