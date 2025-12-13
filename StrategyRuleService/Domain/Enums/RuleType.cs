using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Enums;

public enum RuleType
{
    Price,              // Fiyat kuralları
    Volume,             // Hacim kuralları
    Time,               // Zaman kuralları
    StopLoss,           // Zarar durdurma
    TakeProfit,         // Kar alma
    TechnicalIndicator, // Teknik indikatörler (RSI, MACD vb.)
    Position,           // Pozisyon durumu
    TotalLoss,          // Toplam zarar kontrolü
    MarketCondition     // Piyasa koşulları
}
