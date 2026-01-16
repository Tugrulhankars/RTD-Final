using Domain.Entities;
using Infrastructure.Services.Grpc.Services;
using Infrastructure.Services.Grpc.Dtos;
using StrategyRuleService.Protos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
namespace Application.Features.Strategies.Dtos;
public class StockWorkflow
{
    public string Symbol { get; set; }
    public bool Cancelled { get; set; } = false;
    public int Step { get; set; } = 0;
    public decimal CurrentPrice { get; set; }
    public decimal OpeningPrice { get; set; }
    public decimal HighPrice { get; set; }
    public decimal LowPrice { get; set; }
    public decimal PreviousClosePrice { get; set; }
    public decimal Change { get; set; }
    public decimal PercentChange { get; set; }
    public string CompanyName { get; set; }
    public string Exchange { get; set; }
    public string Industry { get; set; }
    public string Currency { get; set; }
    public decimal? Pe { get; set; }
    public decimal? Pb { get; set; }
    public decimal? Roe { get; set; }
    public decimal? NetMargin { get; set; }
    public decimal? DebtEquity { get; set; }
    public bool InPortfolio { get; set; }
    public decimal TotalLossPercent { get; set; }
    public DateTime Now { get; set; }
    public List<StrategyEvent> StrategyEvents { get; set; } = new List<StrategyEvent>();
    public int StrategyId { get; set; }
    public int UserId { get; set; }
    public int AccountId { get; set; }
    public decimal TransactionAmount { get; set; }
    public int PortfolioId { get; set; }
    public decimal StopLossPercent { get; set; } = 2.0m;
    public decimal ProfitTargetPercent { get; set; } = 5.0m;
    public decimal MaxTotalLoss { get; set; } = 5.0m;
    public decimal? BuyPrice { get; set; }
    public Func<CreateTradeRequest, Task<CreateTradeResponse>> TradeService { get; set; }
    public Func<int, string, Task<bool>> PortfolioService { get; set; }
    public Func<int, Task<decimal>> AccountService { get; set; }
    public Func<string, Task<StockInfoDto>> MarketDataService { get; set; }
    public Func<Task> OnStrategyCompleted { get; set; } // Strateji tamamlandığında çağrılacak callback
    public bool MarketOpen =>
        !Cancelled && 
        Now.TimeOfDay >= new TimeSpan(10, 0, 0) &&
        Now.TimeOfDay <= new TimeSpan(17, 59, 0);
    public decimal PriceChangeFromOpen => OpeningPrice > 0 
        ? ((CurrentPrice - OpeningPrice) / OpeningPrice) * 100 
        : 0;
    public decimal? ProfitLossPercentFromBuy => BuyPrice.HasValue && BuyPrice.Value > 0
        ? ((CurrentPrice - BuyPrice.Value) / BuyPrice.Value) * 100
        : null;
    public decimal? StopLossPrice => BuyPrice.HasValue && StopLossPercent > 0
        ? BuyPrice.Value * (1 - StopLossPercent / 100)
        : null;
    public decimal? TakeProfitPrice => BuyPrice.HasValue && ProfitTargetPercent > 0
        ? BuyPrice.Value * (1 + ProfitTargetPercent / 100)
        : null;
    public decimal EntryThresholdPercent { get; set; } = -5.0m;
    public UserPreference UserPreference { get; set; }
}
public class UserPreference
{
    public int StrategyId { get; set; }
    public int UserId { get; set; }
    public string Ticker { get; set; }
    public decimal StopLossPercentage { get; set; } = 2.0m;
    public decimal TakeProfitPercentage { get; set; } = 5.0m;
    public decimal EntryThresholdPercentage { get; set; } = -5.0m;
    public decimal MaxLossLimitPercentage { get; set; } = 5.0m;
}
