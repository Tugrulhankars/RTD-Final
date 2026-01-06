using AccountService.Configuration;
using AccountService.Dtos.Request;
using AccountService.Dtos.Response;
using AccountService.Events;
using AccountService.Models;
using AccountService.Protos;
using AccountService.Repositories;
using Grpc.Core;
using Microsoft.Extensions.Logging;
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
                var allAccounts = await _accountRepository.GetAllAsync();
                _logger?.LogInformation("[AccountService] Total accounts in database: {Count}", allAccounts?.Count ?? 0);
                if (allAccounts != null && allAccounts.Any())
                {
                    var accountDetails = string.Join(", ", allAccounts.Select(a => $"Id={a.Id}, UserId={a.UserId}, Email={a.Email}"));
                    _logger?.LogInformation("[AccountService] All accounts UserIds: {AccountDetails}", accountDetails);
                }
                return null;
            }
            
            _logger?.LogInformation("[AccountService] Account FOUND for UserId={UserId}: AccountId={AccountId}, Email={Email}, FirstName={FirstName}", 
                userId, account.Id, account.Email, account.FirstName);
            
            GetAccountByUserResponse response = new();
            response.AccountId = account.Id;
            response.AccountStatus = account.AccountStatus;
            response.Balance = account.Balance;
            response.FirstName = account.FirstName ?? string.Empty;
            response.LastName = account.LastName ?? string.Empty;
            return response;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "[AccountService] GetAccountByUser EXCEPTION for UserId={UserId}: {ErrorMessage}", userId, ex.Message);
            return null;
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
            return new UpdateBalanceResponse
            {
                IsSuccess = false,
                Message = $"Bakiye güncellenirken hata oluştu: {ex.Message}",
                NewBalance = 0
            };
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
