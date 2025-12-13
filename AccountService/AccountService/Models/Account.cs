namespace AccountService.Models;

public class Account
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string Email { get; set; }
    public double Balance { get; set; }
    public AccountStatus AccountStatus { get; set; }


}
