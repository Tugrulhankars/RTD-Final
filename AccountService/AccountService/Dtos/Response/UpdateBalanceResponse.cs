namespace AccountService.Dtos.Response;

public class UpdateBalanceResponse
{
    public bool IsSuccess { get; set; }
    public string Message { get; set; }
    public decimal NewBalance { get; set; }
}
