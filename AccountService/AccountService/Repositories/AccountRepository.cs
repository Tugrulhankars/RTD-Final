using AccountService.Models;
using Microsoft.EntityFrameworkCore;

namespace AccountService.Repositories;

public class AccountRepository:EfRepositoryBase<Account>,IAccountRepository
{
    public AccountRepository(DatabaseContext context):base(context)
    {
        
    }

}
