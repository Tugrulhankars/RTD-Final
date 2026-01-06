using Microsoft.EntityFrameworkCore;
using PortfolioService.Models;

namespace PortfolioService.Repositories.Context;

public class DatabaseContext:DbContext
{
    public DatabaseContext(DbContextOptions options):base(options)
    {

    }

    public DbSet<Portfolio> Portfolios { get; set; }
    public DbSet<StockCertificate> StockCertificates { get; set; }

}
