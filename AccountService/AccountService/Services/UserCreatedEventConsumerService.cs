using AccountService.Configuration;
using AccountService.Events;
using AccountService.Repositories;
using AccountService.Models;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;

namespace AccountService.Services;

public class UserCreatedEventConsumerService : BackgroundService
{
    private readonly ILogger<UserCreatedEventConsumerService> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;

    public UserCreatedEventConsumerService(
        ILogger<UserCreatedEventConsumerService> logger,
        IServiceProvider serviceProvider,
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("UserCreatedEventConsumerService başlatıldı");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var kafkaConsumer = scope.ServiceProvider.GetRequiredService<KafkaConsumerService<UserCreatedEvent>>();
                var accountRepository = scope.ServiceProvider.GetRequiredService<IAccountRepository>();

                _logger.LogInformation("Kafka'dan UserCreatedEvent bekleniyor...");

                var userCreatedEvent = await kafkaConsumer.Consume("user-registered", stoppingToken);

                if (userCreatedEvent != null)
                {
                    var eventData = userCreatedEvent;
                    _logger.LogInformation("UserCreatedEvent alındı: UserId={UserId}, Email={Email}, FirstName={FirstName}, LastName={LastName}",
                        eventData.UserId, eventData.Email, eventData.FirstName, eventData.LastName);

                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            using var taskScope = _serviceProvider.CreateScope();
                            var taskAccountRepository = taskScope.ServiceProvider.GetRequiredService<IAccountRepository>();
                            var taskLogger = taskScope.ServiceProvider.GetRequiredService<ILogger<UserCreatedEventConsumerService>>();

                            var existingAccount = await taskAccountRepository.GetAsync(a => a.UserId == eventData.UserId);
                    if (existingAccount != null)
                    {
                                taskLogger.LogWarning("Kullanıcının zaten bir hesabı var: UserId={UserId}", eventData.UserId);
                                return;
                    }

                    Account account = new()
                    {
                        Balance = 0,
                                UserId = (int)eventData.UserId,
                                FirstName = eventData.FirstName,
                                LastName = eventData.LastName,
                                Email = eventData.Email,
                        AccountStatus = AccountStatus.ACTIVE
                    };

                            var createdAccount = await taskAccountRepository.AddAsync(account);
                            taskLogger.LogInformation("Hesap başarıyla oluşturuldu: AccountId={AccountId}, UserId={UserId}",
                        createdAccount.Id, createdAccount.UserId);
                            
                            try
                            {
                                await CreatePortfolioForAccount(createdAccount.Id, (int)eventData.UserId, taskLogger);
                            }
                            catch (Exception portfolioEx)
                            {
                                taskLogger.LogError(portfolioEx, "Portfolio oluşturulurken hata oluştu: AccountId={AccountId}, UserId={UserId}", 
                                    createdAccount.Id, eventData.UserId);
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "UserCreatedEvent işlenirken hata oluştu: UserId={UserId}", eventData.UserId);
                        }
                    }, stoppingToken);

                }
                else
                {
                    await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                }
            }
            catch (Confluent.Kafka.ConsumeException consumeEx)
            {
                _logger.LogError(consumeEx, "Kafka mesajı deserialize edilemedi (key/value hatası). Mesaj skip ediliyor, service devam ediyor: {Error}", consumeEx.Message);
                await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
            }
            catch (System.Text.Json.JsonException jsonEx)
            {
                _logger.LogError(jsonEx, "JSON deserialization hatası. Mesaj skip ediliyor: {Error}", jsonEx.Message);
                await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("UserCreatedEventConsumerService cancellation requested");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UserCreatedEvent işlenirken genel hata oluştu - Kafka down olabilir: {Error}", ex.Message);
                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
            }
        }

        _logger.LogInformation("UserCreatedEventConsumerService durduruldu");
    }

    private async Task CreatePortfolioForAccount(int accountId, int userId, ILogger logger)
    {
        try
        {
            logger.LogInformation("Portfolio oluşturuluyor: AccountId={AccountId}, UserId={UserId}", accountId, userId);
            
            var httpClient = _httpClientFactory.CreateClient("PortfolioService");
            
            var requestBody = new
            {
                userId = userId,
                accountId = accountId
            };
            
            var jsonContent = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
            
            var endpoint = "/api/portfolio/createPortfolio";
            logger.LogInformation("Portfolio-Service'e istek gönderiliyor: Endpoint={Endpoint}, Body={Body}", endpoint, jsonContent);
            
            var response = await httpClient.PostAsync(endpoint, content);
            var responseBody = await response.Content.ReadAsStringAsync();
            
            logger.LogInformation("Portfolio-Service yanıtı: StatusCode={StatusCode}, Body={Body}", 
                response.StatusCode, responseBody);
            
            if (response.IsSuccessStatusCode)
            {
                logger.LogInformation("Portfolio başarıyla oluşturuldu: AccountId={AccountId}, UserId={UserId}", accountId, userId);
            }
            else
            {
                logger.LogWarning("Portfolio oluşturulamadı: AccountId={AccountId}, UserId={UserId}, StatusCode={StatusCode}, Body={Body}", 
                    accountId, userId, response.StatusCode, responseBody);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Portfolio oluşturulurken exception: AccountId={AccountId}, UserId={UserId}, Error={Error}", 
                accountId, userId, ex.Message);
        }
    }
}
