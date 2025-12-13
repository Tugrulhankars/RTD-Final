namespace AccountService.Events;

public class UserCreatedEvent
{
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string Email { get; set; }
    public int UserId { get; set; }
   
}
