using AccountService.Models;

namespace AccountService.Dtos.Response;

public class GetAccountByUserResponse
{
    public int AccountId { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public double Balance { get; set; }
    public AccountStatus AccountStatus { get; set; }
}
