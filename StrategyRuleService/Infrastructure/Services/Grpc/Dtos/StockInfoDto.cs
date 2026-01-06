namespace Infrastructure.Services.Grpc.Dtos;
public class StockInfoDto
{
    public StockQuoteDto Quote { get; set; }
    public CompanyProfileDto Profile { get; set; }
    public FinancialMetricsDto Metrics { get; set; }
}
public class StockQuoteDto
{
    public string Ticker { get; set; }
    public decimal CurrentPrice { get; set; }
    public decimal OpenPrice { get; set; }
    public decimal HighPrice { get; set; }
    public decimal LowPrice { get; set; }
    public decimal PreviousClosePrice { get; set; }
    public decimal Change { get; set; }
    public decimal PercentChange { get; set; }
    public long Timestamp { get; set; }
}
public class CompanyProfileDto
{
    public string Ticker { get; set; }
    public string Name { get; set; }
    public string Exchange { get; set; }
    public string Industry { get; set; }
    public string Ipo { get; set; }
    public string Currency { get; set; }
}
public class FinancialMetricsDto
{
    public decimal? Pe { get; set; }
    public decimal? Pb { get; set; }
    public decimal? Roe { get; set; }
    public decimal? NetMargin { get; set; }
    public decimal? DebtEquity { get; set; }
}
