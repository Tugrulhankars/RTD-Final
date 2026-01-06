using System.ComponentModel.DataAnnotations;

namespace AccountService.Dtos.Request;

public class UpdateBalanceRequest
{
    [Required]
    public int AccountId { get; set; }
    
    [Required]
    public int UserId { get; set; }
    
    [Required]
    public decimal Amount { get; set; }
}
