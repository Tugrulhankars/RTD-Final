using Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Features.Strategies.Queries.GetStrategiesByUserId;

public class GetStrategiesByUserIdResponse
{
    public List<StrategyDto> Strategies { get; set; } = new List<StrategyDto>();
}

public class StrategyDto
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string StrategyName { get; set; }
    public string Description { get; set; }
    public string StockSymbol { get; set; }
    public string Status { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? FinishTime { get; set; }
    public decimal? BuyPrice { get; set; }
    public decimal? SellPrice { get; set; }
    public decimal? ProfitLoss { get; set; }
    public bool IsPositionOpen { get; set; }
    public decimal TotalProfit { get; set; }
    public decimal TotalLoss { get; set; }
    public int TotalTransactions { get; set; }
    public int SuccessfulTransactions { get; set; }
    public List<StrategyEventDto> Events { get; set; } = new List<StrategyEventDto>();
}

public class StrategyEventDto
{
    public int Id { get; set; }
    public int StrategyId { get; set; }
    public int Step { get; set; }
    public string RuleName { get; set; }
    public string Action { get; set; }
    public string Reason { get; set; }
    public decimal Price { get; set; }
    public DateTime Timestamp { get; set; }
}

