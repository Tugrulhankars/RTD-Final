using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Services.Grpc.Services;

public interface IPortfolioService
{
    public Task<bool> IsInPortfolio(int portfolioId,string symbol);
}
