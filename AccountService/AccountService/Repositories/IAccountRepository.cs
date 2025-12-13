using AccountService.Models;

namespace AccountService.Repositories;

public interface IAccountRepository:IAsyncRepository<Account>
{
}
