using AccountService.Dtos.Request;
using AccountService.Dtos.Response;
using AccountService.Events;

namespace AccountService.Services;

public interface IAccountService
{
    public Task CreateAccount();
    public Task<CreateAccountResponse> CreateAccount(CreateAccountRequest request);
    public Task<GetAccountByUserResponse> GetAccountByUser(int userId);
    public Task<GetAccountByUserResponse> GetAccountByAccountId(int accountId);
    public Task<UpdateBalanceResponse> UpdateBalance(UpdateBalanceRequest request);
}
