using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PortfolioService.Configuration;
using PortfolioService.Events;
using PortfolioService.Repositories.Abstracts;
using PortfolioService.Services.Abstracts;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;

namespace PortfolioService.Services;

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
                var portfolioService = scope.ServiceProvider.GetRequiredService<IPortfolioService>();

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
                            var taskPortfolioService = taskScope.ServiceProvider.GetRequiredService<IPortfolioService>();
                            var taskLogger = taskScope.ServiceProvider.GetRequiredService<ILogger<UserCreatedEventConsumerService>>();

                            int accountId = 0;
                            try
                            {
                                accountId = await GetAccountIdFromAccountService((int)eventData.UserId, taskLogger);
                                if (accountId == 0)
                                {
                                    taskLogger.LogWarning("AccountId alınamadı veya 0 döndü: UserId={UserId}. Portfolio oluşturulamıyor.", eventData.UserId);
                                    return;
                                }
                                taskLogger.LogInformation("AccountId alındı: UserId={UserId}, AccountId={AccountId}", eventData.UserId, accountId);
                            }
                            catch (Exception accountEx)
                            {
                                taskLogger.LogError(accountEx, "Account-Service'den AccountId alınırken hata: UserId={UserId}, Error={Error}", 
                                    eventData.UserId, accountEx.Message);
                                return;
                            }

                            try
                            {
                                var createPortfolioRequest = new Dtos.Request.CreatePortfolioRequest
                                {
                                    UserId = (int)eventData.UserId,
                                    AccountId = accountId
                                };

                                await taskPortfolioService.CreatePortfolio(createPortfolioRequest);
                                taskLogger.LogInformation("Portfolio başarıyla oluşturuldu: UserId={UserId}, AccountId={AccountId}",
                                    eventData.UserId, accountId);
                            }
                            catch (Exception portfolioEx)
                            {
                                taskLogger.LogError(portfolioEx, "Portfolio oluşturulurken hata: UserId={UserId}, AccountId={AccountId}, Error={Error}", 
                                    eventData.UserId, accountId, portfolioEx.Message);
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

    private async Task<int> GetAccountIdFromAccountService(int userId, ILogger logger)
    {
        try
        {
            logger.LogInformation("Account-Service'den AccountId alınıyor: UserId={UserId}", userId);
            
            var httpClient = _httpClientFactory.CreateClient("AccountService");
            var endpoint = $"/api/account/getAccountByUser/{userId}";
            
            logger.LogInformation("Account-Service'e istek gönderiliyor: Endpoint={Endpoint}", endpoint);
            
            var response = await httpClient.GetAsync(endpoint);
            var responseBody = await response.Content.ReadAsStringAsync();
            
            logger.LogInformation("Account-Service yanıtı: StatusCode={StatusCode}, Body={Body}", 
                response.StatusCode, responseBody);
            
            if (response.IsSuccessStatusCode)
            {
                using var jsonDoc = JsonDocument.Parse(responseBody);
                var root = jsonDoc.RootElement;
                
                if (root.TryGetProperty("accountId", out var accountIdElement))
                {
                    var accountId = accountIdElement.GetInt32();
                    logger.LogInformation("AccountId başarıyla alındı: UserId={UserId}, AccountId={AccountId}", userId, accountId);
                    return accountId;
                }
                else
                {
                    logger.LogWarning("Account-Service yanıtında accountId bulunamadı: UserId={UserId}, Response={Response}", userId, responseBody);
                    return 0;
                }
            }
            else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                logger.LogWarning("Account bulunamadı (404): UserId={UserId}. Account henüz oluşturulmamış olabilir.", userId);
                return 0;
            }
            else
            {
                logger.LogWarning("Account-Service yanıt hatası: UserId={UserId}, StatusCode={StatusCode}, Body={Body}", 
                    userId, response.StatusCode, responseBody);
                return 0;
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Account-Service'den AccountId alınırken exception: UserId={UserId}, Error={Error}", 
                userId, ex.Message);
            return 0;
        }
    }
}
