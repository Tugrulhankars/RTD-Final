using Core.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
namespace Domain.Entities;
public class StockCertificate:BaseEntity<int>
{
    public string StockSymbol { get; set; }
    public decimal NowPrice { get; set; }
    public decimal DayOpenPrice { get; set; }
    public decimal LastClosePrice { get; set; }
    public decimal? NowDailyVolume { get; set; }
    public decimal DailyLowPrice { get; set; }
    public decimal? OneMinuteVolume { get; set; }
    public decimal? StopLoss { get; set; }
    public decimal? TakeProfitLevel { get; set; }
    public decimal? PurchasePrice { get; set; }
    public decimal? PercentLoss { get; set; }
    public int? TimeTracking { get; set; }
    public double? TimeTrackingPrice { get; set; }
}
