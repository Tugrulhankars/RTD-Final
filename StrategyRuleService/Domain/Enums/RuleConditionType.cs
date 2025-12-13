using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Enums;

public enum RuleConditionType
{
    ALWAYS,         // Her zaman çalış
    ON_SUCCESS,     // Önceki kural başarılı olursa çalış
    ON_FAILURE,     // Önceki kural başarısız olursa çalış
    ON_POSITION_OPEN,    // Pozisyon açıkken çalış
    ON_POSITION_CLOSED,  // Pozisyon kapalıyken çalış
    ON_TIME,        // Belirli zaman aralığında çalış
    ON_MARKET_HOURS,     // Piyasa saatlerinde çalış
    ON_VOLUME_SPIKE,     // Hacim artışında çalış
    ON_PRICE_BREAKOUT    // Fiyat kırılımında çalış
}
