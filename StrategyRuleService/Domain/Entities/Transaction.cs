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
    public string TransactionType { get; set; } // BUY, SELL
    public decimal Amount { get; set; }         // İşlem miktarı (TL)
    public decimal Price { get; set; }          // İşlem fiyatı
    public decimal Quantity { get; set; }       // İşlem adedi (lot)
    public DateTime TransactionTime { get; set; }
    
    // İşlem sonucu
    public decimal? ProfitLoss { get; set; }    // Kar/Zarar
    public string Status { get; set; }          // PENDING, COMPLETED, CANCELLED, FAILED
    public string OrderId { get; set; }         // Broker sipariş ID'si
    
    // İşlem detayları
    public string Reason { get; set; }          // İşlem sebebi (hangi kural)
    public string MarketData { get; set; }      // JSON formatında piyasa verisi
    
    // Navigation properties
    public Strategy Strategy { get; set; }
}
