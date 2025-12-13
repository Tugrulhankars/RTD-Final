namespace PaymentService.Dtos.Response;

public class PaymentResponse
{
    public string Description { get; set; }

    public PaymentResponse(string description)
    {
        Description = description;
    }

    public PaymentResponse()
    {
        
    }
}
