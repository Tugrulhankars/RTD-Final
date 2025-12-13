using AccountService.Models;
using Microsoft.EntityFrameworkCore;

namespace AccountService.Repositories;

public class DatabaseContext:DbContext 
{
    public DatabaseContext(DbContextOptions options): base(options)
    {
        
    }
    
    public DbSet<Account> Accounts { get; set; }
}
