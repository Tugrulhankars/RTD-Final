namespace AccountService.Events;

public class AccountBalanceUpdatedEvent
{
    public string Email { get; set; }
    public decimal Amount { get; set; }
    public DateTime Date { get; set; }
}
