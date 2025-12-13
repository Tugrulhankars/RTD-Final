namespace MarketDataService.Dtos;

public class StockInfoDto
{
    public StockQuoteDto Quote { get; set; }
    public CompanyProfileDto Profile { get; set; }
    public FinancialMetricsDto Metrics { get; set; }
}
