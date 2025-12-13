namespace AccountService.Events;

public class AccountBalanceUpdatedEvent
{
    public string Email { get; set; }
    public double Amount { get; set; }
    public DateTime Date { get; set; }
}
