using System.ComponentModel.DataAnnotations;

namespace AccountService.Dtos.Request;

public class CreateAccountRequest
{
    [Required]
    public int UserId { get; set; }
    
    [Required]
    [StringLength(50)]
    public string FirstName { get; set; }
    
    [Required]
    [StringLength(50)]
    public string LastName { get; set; }
    
    [Required]
    [EmailAddress]
    public string Email { get; set; }
}
