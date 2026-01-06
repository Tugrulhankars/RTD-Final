using System.Text.Json.Serialization;
namespace PaymentService.Events;
public class PaymentSuccessEvent
{
    [JsonPropertyName("userId")]
    public int UserId { get; set; }
    [JsonPropertyName("accountId")]
    public int AccountId { get; set; }
    [JsonPropertyName("amount")]
    public decimal Amount { get; set; }
    [JsonPropertyName("currency")]
    public string Currency { get; set; } = "TRY";
    [JsonPropertyName("paymentTransactionId")]
    public string PaymentTransactionId { get; set; } = string.Empty;
    [JsonPropertyName("paymentMethod")]
    public string PaymentMethod { get; set; } = string.Empty;
    [JsonPropertyName("paymentDate")]
    public DateTime PaymentDate { get; set; }
    [JsonPropertyName("email")]
    public string Email { get; set; } = string.Empty;
    [JsonPropertyName("status")]
    public string Status { get; set; } = "SUCCESS";
    [JsonPropertyName("message")]
    public string? Message { get; set; }
}
