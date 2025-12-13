using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities;

//finhub'dan gelcek veriler
public class StockTradeState
{
    public string StockSymbol { get; set; }
    public decimal? PurchasePrice { get; set; }
    public decimal? StopLoss { get; set; }
    public List<decimal>? TakeProfitLevels { get; set; }
    public decimal? PercentLoss { get; set; }
    public decimal? LotSize { get; set; }
    public bool HasOpenPosition { get; set; }
    public TimeSpan? TradingStartTime { get; set; }
    public TimeSpan? TradingEndTime { get; set; }
}

