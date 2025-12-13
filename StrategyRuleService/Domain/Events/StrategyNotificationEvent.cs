namespace Domain.Events;

public class StrategyNotificationEvent
{
    public int StrategyId { get; set; }
    public int UserId { get; set; }
    public string? UserEmail { get; set; } // Email adresi - NotificationService için
    public string StrategyName { get; set; }
    public string StockSymbol { get; set; }
    public string Status { get; set; } // "Active", "Completed", "Inactive"
    public string Action { get; set; } // "BUY", "SELL"
    public decimal? BuyPrice { get; set; }
    public decimal? SellPrice { get; set; }
    public decimal? ProfitLoss { get; set; }
    public decimal CurrentPrice { get; set; }
    public DateTime Timestamp { get; set; }
    public List<RuleExecutionInfo> ExecutedRules { get; set; } = new List<RuleExecutionInfo>();
    public string? Reason { get; set; }
}

public class RuleExecutionInfo
{
    public string RuleName { get; set; }
    public int Step { get; set; }
    public string Action { get; set; }
    public string Reason { get; set; }
    public decimal Price { get; set; }
    public DateTime Timestamp { get; set; }
}

