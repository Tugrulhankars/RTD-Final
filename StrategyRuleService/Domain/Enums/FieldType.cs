using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
namespace Domain.Enums;
public enum FieldType
{
    CurrentPrice,
    OpeningPrice,
    LastClosingPrice,
    DailyHigh,
    DailyLow,
    PriceChange,
    PriceChangePercent,
    PriceChangeFromOpen,
    BuyPrice,
    ProfitLossPercent,
    TotalLoss,
    CurrentVolume,
    MinuteVolume,
    DailyVolume
}
