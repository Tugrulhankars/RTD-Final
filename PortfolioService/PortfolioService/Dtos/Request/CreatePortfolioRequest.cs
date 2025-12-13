namespace PortfolioService.Dtos.Request;

public class CreatePortfolioRequest
{
    public int UserId { get; set; }
    public int AccountId { get; set; }
    public string Symbol { get; set; }
    public int Lot { get; set; }
}
