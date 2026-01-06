using System.ComponentModel.DataAnnotations;
namespace PaymentService.Dtos.Request;
public record CreatePaymentDto(
    [Range(10, 100000, ErrorMessage = "Tutar 10 TL ile 100.000 TL arasında olmalıdır")]
    decimal Amount,
    [Required(ErrorMessage = "E-posta adresi gereklidir")]
    [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi giriniz")]
    string Email,
    [Range(1, int.MaxValue, ErrorMessage = "Geçerli bir kullanıcı ID'si giriniz")]
    int UserId
);
