using Application.Features.Strategies.Queries.GetStrategiesByUserId;

namespace Application.Features.Strategies.Queries.GetStrategyDetail;

public class GetStrategyDetailResponse
{
    public StrategyDetailDto Strategy { get; set; }
}

public class StrategyDetailDto : StrategyDto
{
    // Anlık piyasa bilgileri
    public decimal? CurrentPrice { get; set; }
    public decimal? OpeningPrice { get; set; }
    public decimal? PriceChangePercent { get; set; }
    public DateTime? LastPriceUpdate { get; set; }
    
    // Strateji durumu detayları
    public string CurrentStep { get; set; }
    public bool IsMarketOpen { get; set; }
}

