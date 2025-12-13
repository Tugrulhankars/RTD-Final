using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Enums;

public enum StrategyStatus
{
    Active,         // Aktif - çalışıyor
    Inactive,       // Pasif - durdurulmuş
    Completed,      // Tamamlanmış
    Paused,         // Duraklatılmış
    Error,          // Hata durumu
    Waiting         // Beklemede
}
