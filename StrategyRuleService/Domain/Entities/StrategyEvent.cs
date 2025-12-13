using Core.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities;

public class StrategyEvent : BaseEntity<int>
{
    public int StrategyId { get; set; }       // Hangi stratejiye ait
    public int Step { get; set; }             // Workflow adımı
    public string RuleName { get; set; }      // Çalışan kural
    public string Action { get; set; }        // Buy / Sell / Stop / Continue
    public string Reason { get; set; }        // Neden çalıştı/durduruldu
    public decimal Price { get; set; }        // O anki fiyat
    public DateTime Timestamp { get; set; }   // Olay zamanı
}
