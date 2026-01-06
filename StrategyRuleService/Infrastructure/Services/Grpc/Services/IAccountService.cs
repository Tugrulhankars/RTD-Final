using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
namespace Infrastructure.Services.Grpc.Services;
public interface IAccountService
{
    Task<double> GetAccountBalanceAsync(int accountId);
    Task<bool> UpdateAccountBalanceAsync(int accountId, double newBalance);
}
