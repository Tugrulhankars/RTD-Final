using Core.Entities;
using Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
namespace Domain.Entities;
public class Strategy : BaseEntity<int>
{
    public int UserId { get; set; }
    public string StrategyName { get; set; }
    public string Description { get; set; }
    public string StockSymbol { get; set; }
    public decimal TransactionAmount { get; set; }
    public decimal TransactionPercentage { get; set; } = 100m;
    public decimal BuyThresholdPercent { get; set; } = -5.0m;
    public decimal ProfitTargetPercent { get; set; } = 5.0m;
    public decimal StopLossPercent { get; set; } = 2.0m;
    public decimal StopLossPercentage { get; set; } = 5.0m;
    public decimal TakeProfitPercentage { get; set; } = 10.0m;
    public decimal EntryThresholdPercentage { get; set; } = -5.0m;
    public TimeSpan? StartTime { get; set; }
    public TimeSpan? EndTime { get; set; }
    public StrategyStatus Status { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? FinishTime { get; set; }
    public int? DurationHours { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public bool IsActive { get; set; } = true;
    public decimal? BuyPrice { get; set; }
    public decimal? SellPrice { get; set; }
    public decimal? ProfitLoss { get; set; }
    public bool IsPositionOpen { get; set; } = false;
    public decimal TotalProfit { get; set; } = 0m;
    public decimal TotalLoss { get; set; } = 0m;
    public int TotalTransactions { get; set; } = 0;
    public int SuccessfulTransactions { get; set; } = 0;
    public decimal MaxTotalLoss { get; set; } = 5.0m;
    public int RuleCount { get; set; }
    public int? CurrentStep { get; set; }
    public int? AccountId { get; set; }
    public int? PortfolioId { get; set; }
}
