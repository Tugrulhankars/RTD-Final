using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities;
//finhub'dan gelcek veriler

public class StockMarketData
{
    // MarketDataService'den gelen temel veriler
    public string StockSymbol { get; set; }
    public decimal NowPrice { get; set; }                    // CurrentPrice
    public decimal DayOpenPrice { get; set; }                // OpeningPrice
    public decimal LastClosePrice { get; set; }              // LastClosingPrice
    public decimal DailyHighPrice { get; set; }              // DailyHigh
    public decimal DailyLowPrice { get; set; }               // DailyLow
    public decimal PriceChange { get; set; }                 // Change
    public decimal PriceChangePercent { get; set; }          // PercentChange
    public DateTime Timestamp { get; set; } = DateTime.Now;
    
    // Simüle edilmiş veriler (sadece fallback durumunda)
    public decimal? NowDailyVolume { get; set; }             // CurrentVolume
    public decimal? OneMinuteVolume { get; set; }            // MinuteVolume
    public decimal? DailyVolume { get; set; }                // DailyVolume
    
    // Hesaplanan değerler
    public decimal PriceChangeFromOpen => DayOpenPrice > 0 ? ((NowPrice - DayOpenPrice) / DayOpenPrice) * 100 : 0;
}
