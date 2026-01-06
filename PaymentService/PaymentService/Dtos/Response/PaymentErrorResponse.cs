namespace PaymentService.Dtos.Response;
public class PaymentErrorResponse
{
    public bool Success { get; set; } = false;
    public string ErrorCode { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? Details { get; set; }
    public Dictionary<string, object>? Metadata { get; set; }
}
