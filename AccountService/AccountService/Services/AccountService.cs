using AccountService.Configuration;
using AccountService.Dtos.Request;
using AccountService.Dtos.Response;
using AccountService.Events;
using AccountService.Models;
using AccountService.Protos;
using AccountService.Repositories;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Data.SqlClient;
using System.Linq;
using System.Transactions;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using static AccountService.Protos.AccountService;
using UpdateBalanceRequest = AccountService.Dtos.Request.UpdateBalanceRequest;
using UpdateBalanceResponse = AccountService.Dtos.Response.UpdateBalanceResponse;

namespace AccountService.Services;

public class AccountService : AccountServiceBase,IAccountService
{
    private readonly IAccountRepository _accountRepository;
    private readonly KafkaConsumerService<UserCreatedEvent> _kafkaConsumerService;
    private readonly IRabbitMQPublisher _rabbitMQPublisher;
    private readonly ILogger<AccountService>? _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    
    public AccountService(
        IAccountRepository accountRepository,
        KafkaConsumerService<UserCreatedEvent> kafkaConsumerService, 
        IRabbitMQPublisher rabbitMQPublisher, 
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<AccountService>? logger = null)
    {
        _accountRepository = accountRepository;
        _kafkaConsumerService = kafkaConsumerService;
        _rabbitMQPublisher = rabbitMQPublisher;
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _logger = logger;
    }
    public async Task CreateAccount()
    {
        UserCreatedEvent? userCreatedEvent = await _kafkaConsumerService.Consume("user-created");
        
        if (userCreatedEvent == null)
        {
            return;
        }
        
        Account account = new();
        account.Balance = 0;
        account.UserId = (int)userCreatedEvent.UserId;
        account.FirstName = userCreatedEvent.FirstName;
        account.LastName = userCreatedEvent.LastName;
        account.Email = userCreatedEvent.Email;
        account.AccountStatus = AccountStatus.ACTIVE;

        var createdAccount = await _accountRepository.AddAsync(account);
        
        _ = Task.Run(async () =>
        {
            try
            {
                await CreatePortfolioForAccount(createdAccount.Id, (int)userCreatedEvent.UserId);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Portfolio oluşturulurken hata oluştu: AccountId={AccountId}, UserId={UserId}", 
                    createdAccount.Id, userCreatedEvent.UserId);
            }
        });
    }

    public async Task<CreateAccountResponse> CreateAccount(CreateAccountRequest request)
    {
        try
        {
            var existingAccount = await _accountRepository.GetAsync(a => a.UserId == request.UserId);
            if (existingAccount != null)
            {
                return new CreateAccountResponse
                {
                    IsSuccess = false,
                    Message = "Bu kullanıcının zaten bir hesabı bulunmaktadır.",
                    AccountId = 0
                };
            }

            Account account = new()
            {
                UserId = request.UserId,
                FirstName = request.FirstName,
                LastName = request.LastName,
                Email = request.Email,
                Balance = 0,
                AccountStatus = AccountStatus.ACTIVE
            };

            var createdAccount = await _accountRepository.AddAsync(account);
            
            _ = Task.Run(async () =>
            {
                try
                {
                    await CreatePortfolioForAccount(createdAccount.Id, request.UserId);
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Portfolio oluşturulurken hata oluştu: AccountId={AccountId}, UserId={UserId}", 
                        createdAccount.Id, request.UserId);
                }
            });
            
            return new CreateAccountResponse
            {
                IsSuccess = true,
                Message = "Hesap başarıyla oluşturuldu.",
                AccountId = createdAccount.Id
            };
        }
        catch (Exception ex)
        {
            // Veritabanı bağlantı hatası durumunda uygun mesaj döndür
            if (ex is SqlException || 
                (ex.InnerException is SqlException) ||
                ex.Message.Contains("network-related", StringComparison.OrdinalIgnoreCase) ||
                ex.Message.Contains("instance-specific", StringComparison.OrdinalIgnoreCase) ||
                ex.Message.Contains("server was not found", StringComparison.OrdinalIgnoreCase) ||
                ex.Message.Contains("could not open a connection", StringComparison.OrdinalIgnoreCase))
            {
                _logger?.LogWarning("[AccountService] Veritabanı bağlantı hatası nedeniyle hesap oluşturulamadı. UserId={UserId}", request.UserId);
                return new CreateAccountResponse
                {
                    IsSuccess = false,
                    Message = "Veritabanı bağlantı hatası. Lütfen daha sonra tekrar deneyin.",
                    AccountId = 0
                };
            }
            return new CreateAccountResponse
            {
                IsSuccess = false,
                Message = $"Hesap oluşturulurken hata oluştu: {ex.Message}",
                AccountId = 0
            };
        }
    }

    public async Task<GetAccountByUserResponse> GetAccountByUser(int userId)
    {
        try
        {
            _logger?.LogInformation("[AccountService] GetAccountByUser called with UserId={UserId} (Type: {Type})", userId, userId.GetType().Name);
            
            Account account = await _accountRepository.GetAsync(u => u.UserId == userId);
            
            if (account == null)
            {
                _logger?.LogWarning("[AccountService] Account NOT FOUND for UserId={UserId}", userId);
                try
                {
                    var allAccounts = await _accountRepository.GetAllAsync();
                    _logger?.LogInformation("[AccountService] Total accounts in database: {Count}", allAccounts?.Count ?? 0);
                    if (allAccounts != null && allAccounts.Any())
                    {
                        var accountDetails = string.Join(", ", allAccounts.Select(a => $"Id={a.Id}, UserId={a.UserId}, Email={a.Email}"));
                        _logger?.LogInformation("[AccountService] All accounts UserIds: {AccountDetails}", accountDetails);
                    }
                }
                catch (Exception dbEx)
                {
                    // Veritabanı bağlantı hatası durumunda sessizce devam et
                    if (dbEx is SqlException || 
                        (dbEx.InnerException is SqlException) ||
                        dbEx.Message.Contains("network-related", StringComparison.OrdinalIgnoreCase) ||
                        dbEx.Message.Contains("instance-specific", StringComparison.OrdinalIgnoreCase) ||
                        dbEx.Message.Contains("server was not found", StringComparison.OrdinalIgnoreCase) ||
                        dbEx.Message.Contains("could not open a connection", StringComparison.OrdinalIgnoreCase))
                    {
                        _logger?.LogDebug("[AccountService] Veritabanı bağlantı hatası nedeniyle tüm hesaplar alınamadı. UserId={UserId}", userId);
                    }
                    else
                    {
                        _logger?.LogWarning(dbEx, "[AccountService] GetAllAsync hatası: UserId={UserId}", userId);
                    }
                }
                return null;
            }
            
            _logger?.LogInformation("[AccountService] Account FOUND for UserId={UserId}: AccountId={AccountId}, Email={Email}, FirstName={FirstName}", 
                userId, account.Id, account.Email, account.FirstName);
            
            GetAccountByUserResponse response = new();
            response.AccountId = account.Id;
            response.UserId = account.UserId;
            response.AccountStatus = account.AccountStatus;
            response.Balance = account.Balance;
            response.FirstName = account.FirstName ?? string.Empty;
            response.LastName = account.LastName ?? string.Empty;
            
            // Get email from account, if null/empty, try to get from AuthUserService
            string email = account.Email;
            if (string.IsNullOrWhiteSpace(email))
            {
                _logger?.LogWarning("[AccountService] Email is null/empty in database for UserId={UserId}, AccountId={AccountId}. Attempting to get from AuthUserService.", 
                    userId, account.Id);
                try
                {
                    email = await GetUserEmailFromAuthUserService(userId);
                    if (!string.IsNullOrWhiteSpace(email))
                    {
                        _logger?.LogInformation("[AccountService] Email retrieved from AuthUserService for UserId={UserId}, Email={Email}", userId, email);
                        // Optionally update account email in database
                        // account.Email = email;
                        // await _accountRepository.UpdateAsync(account);
                    }
                }
                catch (Exception ex)
                {
                    _logger?.LogWarning(ex, "[AccountService] Failed to get email from AuthUserService for UserId={UserId}", userId);
                }
            }
            
            response.Email = email ?? string.Empty;
            return response;
        }
        catch (Exception ex)
        {
            // Veritabanı bağlantı hatası durumunda sessizce null döndür
            if (ex is SqlException || 
                (ex.InnerException is SqlException) ||
                ex.Message.Contains("network-related", StringComparison.OrdinalIgnoreCase) ||
                ex.Message.Contains("instance-specific", StringComparison.OrdinalIgnoreCase) ||
                ex.Message.Contains("server was not found", StringComparison.OrdinalIgnoreCase) ||
                ex.Message.Contains("could not open a connection", StringComparison.OrdinalIgnoreCase))
            {
                _logger?.LogDebug("[AccountService] Veritabanı bağlantı hatası nedeniyle hesap alınamadı. UserId={UserId}", userId);
                return null;
            }
            _logger?.LogError(ex, "[AccountService] GetAccountByUser EXCEPTION for UserId={UserId}: {ErrorMessage}", userId, ex.Message);
            return null;
        }
    }

    public async Task<GetAccountByUserResponse> GetAccountByAccountId(int accountId)
    {
        try
        {
            _logger?.LogInformation("[AccountService] GetAccountByAccountId called with AccountId={AccountId}", accountId);
            
            Account account = await _accountRepository.GetAsync(a => a.Id == accountId);
            
            if (account == null)
            {
                _logger?.LogWarning("[AccountService] Account NOT FOUND for AccountId={AccountId}", accountId);
                return null;
            }
            
            _logger?.LogInformation("[AccountService] Account FOUND for AccountId={AccountId}: UserId={UserId}, Email={Email}, FirstName={FirstName}", 
                accountId, account.UserId, account.Email, account.FirstName);
            
            GetAccountByUserResponse response = new();
            response.AccountId = account.Id;
            response.UserId = account.UserId;
            response.AccountStatus = account.AccountStatus;
            response.Balance = account.Balance;
            response.FirstName = account.FirstName ?? string.Empty;
            response.LastName = account.LastName ?? string.Empty;
            response.Email = account.Email ?? string.Empty;
            return response;
        }
        catch (Exception ex)
        {
            if (ex is SqlException || 
                (ex.InnerException is SqlException) ||
                ex.Message.Contains("network-related", StringComparison.OrdinalIgnoreCase) ||
                ex.Message.Contains("instance-specific", StringComparison.OrdinalIgnoreCase) ||
                ex.Message.Contains("server was not found", StringComparison.OrdinalIgnoreCase) ||
                ex.Message.Contains("could not open a connection", StringComparison.OrdinalIgnoreCase))
            {
                _logger?.LogDebug("[AccountService] Veritabanı bağlantı hatası nedeniyle hesap alınamadı. AccountId={AccountId}", accountId);
                return null;
            }
            _logger?.LogError(ex, "[AccountService] GetAccountByAccountId EXCEPTION for AccountId={AccountId}: {ErrorMessage}", accountId, ex.Message);
            return null;
        }
    }

    public override async Task<GetAccountBalanceResponse> GetAccountBalance(GetAccountBalanceRequest request, ServerCallContext context)
    {
        try
        {
            _logger?.LogInformation("[AccountService] gRPC GetAccountBalance called with AccountId={AccountId}", request.AccountId);
            
            Account account = await _accountRepository.GetAsync(a => a.Id == request.AccountId);
            
            if (account == null)
            {
                _logger?.LogWarning("[AccountService] Account NOT FOUND for AccountId={AccountId}", request.AccountId);
                return new GetAccountBalanceResponse
                {
                    Success = false,
                    Message = $"Hesap bulunamadı: AccountId={request.AccountId}",
                    Balance = 0
                };
            }
            
            _logger?.LogInformation("[AccountService] Account FOUND for AccountId={AccountId}: Balance={Balance}", 
                request.AccountId, account.Balance);
            
            return new GetAccountBalanceResponse
            {
                Success = true,
                Message = "Bakiye başarıyla alındı.",
                Balance = (double)account.Balance
            };
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "[AccountService] GetAccountBalance EXCEPTION for AccountId={AccountId}: {ErrorMessage}", 
                request.AccountId, ex.Message);
            return new GetAccountBalanceResponse
            {
                Success = false,
                Message = $"Bakiye alınırken hata oluştu: {ex.Message}",
                Balance = 0
            };
        }
    }

    public async Task<UpdateBalanceResponse> UpdateBalance(Dtos.Request.UpdateBalanceRequest request)
    {
        try
        {
            Account account = await _accountRepository.GetAsync(a => a.Id == request.AccountId && a.UserId == request.UserId);
            if (account == null)
            {
                return new UpdateBalanceResponse
                {
                    IsSuccess = false,
                    Message = "Hesap bulunamadı.",
                    NewBalance = 0
                };
            }

            if (request.Amount < 0 && account.Balance < Math.Abs(request.Amount))
            {
                return new UpdateBalanceResponse
                {
                    IsSuccess = false,
                    Message = "Yetersiz bakiye.",
                    NewBalance = account.Balance
                };
            }

            account.Balance += request.Amount;
            await _accountRepository.UpdateAsync(account);

            AccountBalanceUpdatedEvent balanceUpdatedEvent = new()
            {
                Email = account.Email,
                Amount = request.Amount,
                Date = DateTime.Now
            };
            await _rabbitMQPublisher.Producer(balanceUpdatedEvent, "account-balance-updated");

            return new UpdateBalanceResponse
            {
                IsSuccess = true,
                Message = "Bakiye başarıyla güncellendi.",
                NewBalance = account.Balance
            };
        }
        catch (Exception ex)
        {
            // Veritabanı bağlantı hatası durumunda uygun mesaj döndür
            if (ex is SqlException || 
                (ex.InnerException is SqlException) ||
                ex.Message.Contains("network-related", StringComparison.OrdinalIgnoreCase) ||
                ex.Message.Contains("instance-specific", StringComparison.OrdinalIgnoreCase) ||
                ex.Message.Contains("server was not found", StringComparison.OrdinalIgnoreCase) ||
                ex.Message.Contains("could not open a connection", StringComparison.OrdinalIgnoreCase))
            {
                _logger?.LogWarning("[AccountService] Veritabanı bağlantı hatası nedeniyle bakiye güncellenemedi. AccountId={AccountId}, UserId={UserId}", 
                    request.AccountId, request.UserId);
                return new UpdateBalanceResponse
                {
                    IsSuccess = false,
                    Message = "Veritabanı bağlantı hatası. Lütfen daha sonra tekrar deneyin.",
                    NewBalance = 0
                };
            }
            return new UpdateBalanceResponse
            {
                IsSuccess = false,
                Message = $"Bakiye güncellenirken hata oluştu: {ex.Message}",
                NewBalance = 0
            };
        }
    }

    private async Task<string?> GetUserEmailFromAuthUserService(int userId)
    {
        try
        {
            var authUserServiceUrl = _configuration["AuthUserService:BaseUrl"] ?? "http://localhost:8080";
            var url = $"{authUserServiceUrl}/api/v1/users/{userId}";
            
            _logger?.LogInformation("[AccountService] Attempting to get email from AuthUserService: URL={Url}, UserId={UserId}", url, userId);
            
            using var httpClient = new HttpClient();
            httpClient.Timeout = TimeSpan.FromSeconds(10);
            
            var response = await httpClient.GetAsync(url);
            if (!response.IsSuccessStatusCode)
            {
                _logger?.LogWarning("[AccountService] AuthUserService returned non-success status: StatusCode={StatusCode}, UserId={UserId}", 
                    response.StatusCode, userId);
                return null;
            }
            
            var responseBody = await response.Content.ReadAsStringAsync();
            if (string.IsNullOrWhiteSpace(responseBody))
            {
                _logger?.LogWarning("[AccountService] AuthUserService returned empty response for UserId={UserId}", userId);
                return null;
            }
            
            using var jsonDoc = JsonDocument.Parse(responseBody);
            if (jsonDoc.RootElement.TryGetProperty("email", out var emailElement))
            {
                var email = emailElement.GetString();
                _logger?.LogInformation("[AccountService] Email retrieved from AuthUserService: UserId={UserId}, Email={Email}", userId, email);
                return email;
            }
            
            _logger?.LogWarning("[AccountService] Email property not found in AuthUserService response for UserId={UserId}, Response={Response}", 
                userId, responseBody);
            return null;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "[AccountService] Exception getting email from AuthUserService for UserId={UserId}", userId);
            return null;
        }
    }
    
    private async Task CreatePortfolioForAccount(int accountId, int userId)
    {
        try
        {
            _logger?.LogInformation("Portfolio oluşturuluyor: AccountId={AccountId}, UserId={UserId}", accountId, userId);
            
            var httpClient = _httpClientFactory.CreateClient("PortfolioService");
            
            var requestBody = new
            {
                userId = userId,
                accountId = accountId
            };
            
            var jsonContent = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
            
            var endpoint = "/api/portfolio/createPortfolio";
            _logger?.LogInformation("Portfolio-Service'e istek gönderiliyor: Endpoint={Endpoint}, Body={Body}", endpoint, jsonContent);
            
            var response = await httpClient.PostAsync(endpoint, content);
            var responseBody = await response.Content.ReadAsStringAsync();
            
            _logger?.LogInformation("Portfolio-Service yanıtı: StatusCode={StatusCode}, Body={Body}", 
                response.StatusCode, responseBody);
            
            if (response.IsSuccessStatusCode)
            {
                _logger?.LogInformation("Portfolio başarıyla oluşturuldu: AccountId={AccountId}, UserId={UserId}", accountId, userId);
            }
            else
            {
                _logger?.LogWarning("Portfolio oluşturulamadı: AccountId={AccountId}, UserId={UserId}, StatusCode={StatusCode}, Body={Body}", 
                    accountId, userId, response.StatusCode, responseBody);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Portfolio oluşturulurken exception: AccountId={AccountId}, UserId={UserId}, Error={Error}", 
                accountId, userId, ex.Message);
        }
    }
}
