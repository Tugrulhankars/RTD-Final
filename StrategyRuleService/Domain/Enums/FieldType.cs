using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Enums;

public enum FieldType
{
    // MarketDataService'den gelen veriler
    CurrentPrice,       // Şimdiki fiyat (CurrentPrice)
    OpeningPrice,       // Açılış fiyatı (OpenPrice)
    LastClosingPrice,   // Son kapanış fiyatı (PreviousClosePrice)
    DailyHigh,          // Günlük yüksek fiyat (HighPrice)
    DailyLow,           // Günlük düşük fiyat (LowPrice)
    PriceChange,        // Fiyat değişimi (Change)
    PriceChangePercent, // Fiyat değişim yüzdesi (PercentChange)
    
    // Hesaplanan değerler (MarketDataService verilerinden türetilir)
    PriceChangeFromOpen, // Açılıştan fiyat değişimi (hesaplanır)
    
    // Strateji durumu (Strategy entity'sinden)
    BuyPrice,           // Alış fiyatı (strategy.BuyPrice)
    ProfitLossPercent,  // Alış fiyatından kar/zarar yüzdesi (hesaplanır)
    TotalLoss,          // Toplam zarar (strategy.TotalLoss)
    
    // Simüle edilmiş veriler (sadece fallback durumunda)
    CurrentVolume,      // Mevcut hacim (simüle edilmiş)
    MinuteVolume,       // 1 dakikalık hacim (simüle edilmiş)
    DailyVolume         // Günlük hacim (simüle edilmiş)
}
