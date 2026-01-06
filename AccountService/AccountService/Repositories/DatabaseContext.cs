using AccountService.Models;
using Microsoft.EntityFrameworkCore;

namespace AccountService.Repositories;

public class DatabaseContext:DbContext 
{
    public DatabaseContext(DbContextOptions<DatabaseContext> options): base(options)
    {
        
    }
    
    public DbSet<Account> Accounts { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Account>(entity =>
        {
            entity.Property(e => e.Balance)
                .HasColumnType("decimal(18,2)")
                .IsRequired();
        });
    }
}
