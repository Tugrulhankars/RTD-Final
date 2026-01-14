using AccountService.Models;

namespace AccountService.Dtos.Response;

public class GetAccountByUserResponse
{
    public int AccountId { get; set; }
    public int UserId { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string Email { get; set; }
    public decimal Balance { get; set; }
    public AccountStatus AccountStatus { get; set; }
}
