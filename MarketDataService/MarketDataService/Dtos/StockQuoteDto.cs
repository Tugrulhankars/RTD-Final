namespace MarketDataService.Dtos;

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
