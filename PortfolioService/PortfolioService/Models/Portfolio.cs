namespace PortfolioService.Models;

public class Portfolio
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int AccountId { get; set; }

    public List<StockCertificate> StockCertificates { get; set; }
}
