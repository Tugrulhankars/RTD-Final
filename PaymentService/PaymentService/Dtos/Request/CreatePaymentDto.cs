namespace PaymentService.Dtos.Request;

public record CreatePaymentDto(decimal Amount, string Email, int UserId);
