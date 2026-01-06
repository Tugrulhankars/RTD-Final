namespace PaymentService.Services;
public interface IAccountService
{
    Task<bool> UpdateAccountBalanceAsync(int accountId, int userId, string firstName, string lastName, double amount);
}
