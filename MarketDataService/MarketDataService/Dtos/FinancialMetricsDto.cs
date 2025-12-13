namespace MarketDataService.Dtos;

public class FinancialMetricsDto
{
    public decimal? Pe { get; set; }
    public decimal? Pb { get; set; }
    public decimal? Roe { get; set; }
    public decimal? NetMargin { get; set; }
    public decimal? DebtEquity { get; set; }
}
