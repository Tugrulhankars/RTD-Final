using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
namespace Domain.Entities;
public class StockMarketData
{
    public string StockSymbol { get; set; }
    public decimal NowPrice { get; set; }
    public decimal DayOpenPrice { get; set; }
    public decimal LastClosePrice { get; set; }
    public decimal DailyHighPrice { get; set; }
    public decimal DailyLowPrice { get; set; }
    public decimal PriceChange { get; set; }
    public decimal PriceChangePercent { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.Now;
    public decimal? NowDailyVolume { get; set; }
    public decimal? OneMinuteVolume { get; set; }
    public decimal? DailyVolume { get; set; }
    public decimal PriceChangeFromOpen => DayOpenPrice > 0 ? ((NowPrice - DayOpenPrice) / DayOpenPrice) * 100 : 0;
}
