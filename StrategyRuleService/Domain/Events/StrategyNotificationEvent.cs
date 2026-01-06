using Newtonsoft.Json;
using System.Text.Json.Serialization;
namespace Domain.Events;
public class StrategyNotificationEvent
{
    [JsonProperty("strategyId")]
    [JsonPropertyName("strategyId")]
    public int StrategyId { get; set; }
    [JsonProperty("userId")]
    [JsonPropertyName("userId")]
    public int UserId { get; set; }
    [JsonProperty("userEmail")]
    [JsonPropertyName("userEmail")]
    public string? UserEmail { get; set; }
    [JsonProperty("strategyName")]
    [JsonPropertyName("strategyName")]
    public string StrategyName { get; set; } = string.Empty;
    [JsonProperty("stockSymbol")]
    [JsonPropertyName("stockSymbol")]
    public string StockSymbol { get; set; } = string.Empty;
    [JsonProperty("status")]
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;
    [JsonProperty("action")]
    [JsonPropertyName("action")]
    public string Action { get; set; } = string.Empty;
    [JsonProperty("buyPrice")]
    [JsonPropertyName("buyPrice")]
    public decimal? BuyPrice { get; set; }
    [JsonProperty("sellPrice")]
    [JsonPropertyName("sellPrice")]
    public decimal? SellPrice { get; set; }
    [JsonProperty("profitLoss")]
    [JsonPropertyName("profitLoss")]
    public decimal? ProfitLoss { get; set; }
    [JsonProperty("currentPrice")]
    [JsonPropertyName("currentPrice")]
    public decimal CurrentPrice { get; set; }
    [JsonProperty("timestamp")]
    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; }
    [JsonProperty("executedRules")]
    [JsonPropertyName("executedRules")]
    public List<RuleExecutionInfo> ExecutedRules { get; set; } = new List<RuleExecutionInfo>();
    [JsonProperty("reason")]
    [JsonPropertyName("reason")]
    public string? Reason { get; set; }
}
public class RuleExecutionInfo
{
    [JsonProperty("ruleName")]
    [JsonPropertyName("ruleName")]
    public string RuleName { get; set; } = string.Empty;
    [JsonProperty("step")]
    [JsonPropertyName("step")]
    public int Step { get; set; }
    [JsonProperty("action")]
    [JsonPropertyName("action")]
    public string Action { get; set; } = string.Empty;
    [JsonProperty("reason")]
    [JsonPropertyName("reason")]
    public string Reason { get; set; } = string.Empty;
    [JsonProperty("price")]
    [JsonPropertyName("price")]
    public decimal Price { get; set; }
    [JsonProperty("timestamp")]
    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; }
}
