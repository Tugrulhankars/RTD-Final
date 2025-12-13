using PortfolioService.Models;
using PortfolioService.Repositories.Abstracts;
using PortfolioService.Repositories.Context;

namespace PortfolioService.Repositories;

public class PortfolioRepository : EfRepositoryBase<Portfolio>, IPortfolioRepository
{
    public PortfolioRepository(DatabaseContext context) : base(context)
    {
    }
}
