namespace PortfolioService.Dtos.Response;

public class GetAllPortfolioResponse
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int AccountId { get; set; }
    public string Symbol { get; set; }
    public int Lot { get; set; }
    public double AveragePrice { get; set; }
}
