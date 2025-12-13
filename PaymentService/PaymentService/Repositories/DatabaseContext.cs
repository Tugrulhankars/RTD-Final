using Microsoft.EntityFrameworkCore;
using PaymentService.Models;

namespace PaymentService.Repositories;

public class DatabaseContext:DbContext
{
    public DatabaseContext(DbContextOptions dbContextOptions): base(dbContextOptions)
    {
        
    }

    public DbSet<Payment> Payments { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlServer("");
        
    }
}
