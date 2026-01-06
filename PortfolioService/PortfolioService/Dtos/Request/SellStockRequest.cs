namespace PortfolioService.Dtos.Request;

public class SellStockRequest
{
    public int PortfolioId { get; set; }
    public string Symbol { get; set; }
    public double Lot { get; set; }
    public double PricePerShare { get; set; }
}
