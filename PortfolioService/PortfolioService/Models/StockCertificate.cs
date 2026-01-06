namespace PortfolioService.Models;

public class StockCertificate
{
    public int Id { get; set; }
    public string Symbol { get; set; }
    public int Lot { get; set; }
    public bool IsSell { get; set; }
    public double PricePerShare { get; set; }
    public DateTime? BuyDate { get; set; }
    public DateTime? SellDate { get; set; }

    public int PortfolioId { get; set; }
    public Portfolio Portfolio { get; set; }
}
