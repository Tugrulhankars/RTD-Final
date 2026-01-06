using Core.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
namespace Domain.Entities;
public class Transaction : BaseEntity<int>
{
    public int StrategyId { get; set; }
    public int StrategyExecutionId { get; set; }
    public string TransactionType { get; set; }
    public decimal Amount { get; set; }
    public decimal Price { get; set; }
    public decimal Quantity { get; set; }
    public DateTime TransactionTime { get; set; }
    public decimal? ProfitLoss { get; set; }
    public string Status { get; set; }
    public string OrderId { get; set; }
    public string Reason { get; set; }
    public string MarketData { get; set; }
    public Strategy Strategy { get; set; }
}
