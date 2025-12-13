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
    
    // Strateji parametreleri
    public string StockSymbol { get; set; }        // THYAD, vb.
    public decimal TransactionAmount { get; set; }  // İşlem miktarı (TL)
    public decimal TransactionPercentage { get; set; } = 100m; // İşlem yüzdesi
    
    // Basit strateji parametreleri
    public decimal BuyThresholdPercent { get; set; } = -5.0m;  // Açılışın %5 altına düşerse al
    public decimal ProfitTargetPercent { get; set; } = 5.0m;   // %5 kar hedefi
    public decimal StopLossPercent { get; set; } = 2.0m;       // %2 zarar kesme
    
    // Zaman parametreleri
    public TimeSpan? StartTime { get; set; }       // 10:00
    public TimeSpan? EndTime { get; set; }         // 17:58
    
    // Strateji durumu
    public StrategyStatus Status { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? FinishTime { get; set; }
    
    // İşlem durumu
    public decimal? BuyPrice { get; set; }         // Alış fiyatı
    public decimal? SellPrice { get; set; }        // Satış fiyatı
    public decimal? ProfitLoss { get; set; }       // Kar/Zarar
    public bool IsPositionOpen { get; set; } = false; // Pozisyon açık mı?
    
    // İstatistikler
    public decimal TotalProfit { get; set; } = 0m;
    public decimal TotalLoss { get; set; } = 0m;
    public int TotalTransactions { get; set; } = 0;
    public int SuccessfulTransactions { get; set; } = 0;
    public decimal MaxTotalLoss { get; set; } = 5.0m; // Maksimum toplam zarar yüzdesi
    
    // Kurallar
    public int RuleCount { get; set; }
    
}
