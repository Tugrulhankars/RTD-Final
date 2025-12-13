using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Persistance.DatabaseContext;

public class Context:DbContext
{
    public Context(DbContextOptions<Context> options):base(options)
    {
    }

    public DbSet<Strategy> Strategies { get; set; }
    public DbSet<StrategyEvent> StrategyEvents { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
    }
}
