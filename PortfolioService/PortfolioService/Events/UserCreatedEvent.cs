using System.Text.Json.Serialization;

namespace PortfolioService.Events;

public class UserCreatedEvent
{
    [JsonPropertyName("userId")]
    public long UserId { get; set; }
    
    [JsonPropertyName("email")]
    public string Email { get; set; }
    
    [JsonPropertyName("firstName")]
    public string FirstName { get; set; }
    
    [JsonPropertyName("lastName")]
    public string LastName { get; set; }
}
