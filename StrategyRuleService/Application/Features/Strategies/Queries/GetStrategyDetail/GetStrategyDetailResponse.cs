using Application.Features.Strategies.Queries.GetStrategiesByUserId;
namespace Application.Features.Strategies.Queries.GetStrategyDetail;
public class GetStrategyDetailResponse
{
    public StrategyDetailDto Strategy { get; set; }
    public List<StrategyEventDto> Events { get; set; } = new List<StrategyEventDto>();
}
public class StrategyDetailDto : StrategyDto
{
    public decimal? CurrentPrice { get; set; }
    public decimal? OpeningPrice { get; set; }
    public decimal? HighPrice { get; set; }
    public decimal? LowPrice { get; set; }
    public decimal? PreviousClosePrice { get; set; }
    public decimal? Change { get; set; }
    public decimal? PercentChange { get; set; }
    public decimal? PriceChangePercent { get; set; }
    public DateTime? LastPriceUpdate { get; set; }
    public string CompanyName { get; set; }
    public string Exchange { get; set; }
    public string Industry { get; set; }
    public string Currency { get; set; }
    public string Ipo { get; set; }
    public decimal? Pe { get; set; }
    public decimal? Pb { get; set; }
    public decimal? Roe { get; set; }
    public decimal? NetMargin { get; set; }
    public decimal? DebtEquity { get; set; }
    public string CurrentStep { get; set; }
    public bool IsMarketOpen { get; set; }
}
