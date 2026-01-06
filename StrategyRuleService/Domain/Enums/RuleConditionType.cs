using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
namespace Domain.Enums;
public enum RuleConditionType
{
    ALWAYS,
    ON_SUCCESS,
    ON_FAILURE,
    ON_POSITION_OPEN,
    ON_POSITION_CLOSED,
    ON_TIME,
    ON_MARKET_HOURS,
    ON_VOLUME_SPIKE,
    ON_PRICE_BREAKOUT
}
