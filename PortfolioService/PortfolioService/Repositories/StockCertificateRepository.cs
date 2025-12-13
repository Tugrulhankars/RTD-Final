using PortfolioService.Models;
using PortfolioService.Repositories.Abstracts;
using PortfolioService.Repositories.Context;

namespace PortfolioService.Repositories;

public class StockCertificateRepository : EfRepositoryBase<StockCertificate>, IStockCertificateRepository
{
    public StockCertificateRepository(DatabaseContext context) : base(context)
    {
    }
}
