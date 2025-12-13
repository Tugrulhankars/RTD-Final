namespace AccountService.Dtos.Response;

public class CreateAccountResponse
{
    public bool IsSuccess { get; set; }
    public string Message { get; set; }
    public int AccountId { get; set; }
}
