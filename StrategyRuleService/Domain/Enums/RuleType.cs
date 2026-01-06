using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
namespace Domain.Enums;
public enum RuleType
{
    Price,
    Volume,
    Time,
    StopLoss,
    TakeProfit,
    TechnicalIndicator,
    Position,
    TotalLoss,
    MarketCondition
}
