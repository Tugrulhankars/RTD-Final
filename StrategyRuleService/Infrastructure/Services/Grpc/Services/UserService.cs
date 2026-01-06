using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
namespace Infrastructure.Services.Grpc.Services;
public class UserService : IUserService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<UserService> _logger;
    private readonly string _baseUrl;
    public UserService(HttpClient httpClient, IConfiguration configuration, ILogger<UserService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _baseUrl = configuration["AuthUserService:BaseUrl"] ?? "http://localhost:8080";
        _httpClient.BaseAddress = new Uri(_baseUrl);
        _httpClient.Timeout = TimeSpan.FromSeconds(30);
    }
    public async Task<string?> GetUserEmailByIdAsync(int userId)
    {
        try
        {
            var requestUrl = $"/api/v1/users/{userId}";
            var fullUrl = $"{_baseUrl}{requestUrl}";
            _logger.LogInformation("🔍 Requesting user email from AuthUserService: Url={Url}, UserId={UserId}", fullUrl, userId);
            var response = await _httpClient.GetAsync(requestUrl);
            _logger.LogInformation("📡 AuthUserService response: StatusCode={StatusCode}, UserId={UserId}", response.StatusCode, userId);
            if (response.IsSuccessStatusCode)
            {
                var jsonString = await response.Content.ReadAsStringAsync();
                _logger.LogDebug("📄 AuthUserService response body: {ResponseBody}", jsonString);
                var user = JsonSerializer.Deserialize<UserResponse>(jsonString, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
                if (user != null)
                {
                    var email = user.Email;
                    if (string.IsNullOrEmpty(email))
                    {
                        var userDict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(jsonString, new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });
                        if (userDict != null && userDict.TryGetValue("email", out var emailElement))
                        {
                            email = emailElement.GetString();
                        }
                    }
                    if (!string.IsNullOrEmpty(email))
                    {
                        _logger.LogInformation("✅ User email retrieved from AuthUserService: UserId={UserId}, Email={Email}", userId, email);
                        return email;
                    }
                    else
                    {
                        _logger.LogWarning("⚠️ User email is null or empty: UserId={UserId}, User={User}", 
                            userId, $"Id={user.Id}");
                    }
                }
                else
                {
                    _logger.LogWarning("⚠️ User response is null: UserId={UserId}", userId);
                }
            }
            else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                _logger.LogWarning("⚠️ User not found in AuthUserService: UserId={UserId}, StatusCode=404", userId);
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("⚠️ Failed to get user email from AuthUserService: UserId={UserId}, StatusCode={StatusCode}, Error={Error}", 
                    userId, response.StatusCode, errorContent);
            }
        }
        catch (HttpRequestException httpEx)
        {
            _logger.LogError(httpEx, "❌ HTTP error getting user email from AuthUserService: UserId={UserId}, BaseUrl={BaseUrl}", 
                userId, _baseUrl);
        }
        catch (TaskCanceledException timeoutEx)
        {
            _logger.LogError(timeoutEx, "❌ Timeout getting user email from AuthUserService: UserId={UserId}, BaseUrl={BaseUrl}", 
                userId, _baseUrl);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error getting user email from AuthUserService: UserId={UserId}, BaseUrl={BaseUrl}", 
                userId, _baseUrl);
        }
        return null;
    }
}
public class UserResponse
{
    public long? Id { get; set; }
    public string? Email { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? PhoneNumber { get; set; }
}
