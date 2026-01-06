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
        modelBuilder.Entity<Strategy>(entity =>
        {
            entity.Property(e => e.TransactionAmount)
                .HasPrecision(18, 2);
            entity.Property(e => e.TransactionPercentage)
                .HasPrecision(18, 2);
            entity.Property(e => e.BuyThresholdPercent)
                .HasPrecision(18, 2);
            entity.Property(e => e.ProfitTargetPercent)
                .HasPrecision(18, 2);
            entity.Property(e => e.StopLossPercent)
                .HasPrecision(18, 2);
            entity.Property(e => e.EntryThresholdPercentage)
                .HasPrecision(18, 4)
                .IsRequired();
            entity.Property(e => e.StopLossPercentage)
                .HasPrecision(18, 4)
                .IsRequired();
            entity.Property(e => e.TakeProfitPercentage)
                .HasPrecision(18, 4)
                .IsRequired();
            entity.Property(e => e.IsActive)
                .IsRequired();
            entity.Property(e => e.BuyPrice)
                .HasPrecision(18, 2);
            entity.Property(e => e.SellPrice)
                .HasPrecision(18, 2);
            entity.Property(e => e.ProfitLoss)
                .HasPrecision(18, 2);
            entity.Property(e => e.TotalProfit)
                .HasPrecision(18, 2);
            entity.Property(e => e.TotalLoss)
                .HasPrecision(18, 2);
            entity.Property(e => e.MaxTotalLoss)
                .HasPrecision(18, 2);
        });
        modelBuilder.Entity<StrategyEvent>(entity =>
        {
            entity.Property(e => e.Price)
                .HasPrecision(18, 2);
        });
    }
}
