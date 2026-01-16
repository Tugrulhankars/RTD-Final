using AccountService.Configuration;
using AccountService.Events;
using AccountService.Repositories;
using AccountService.Models;
using Confluent.Kafka;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AccountService.Services;

public class PaymentSuccessEventConsumerService : BackgroundService
{
    private readonly ILogger<PaymentSuccessEventConsumerService> _logger;
    private readonly IServiceProvider _serviceProvider;
    private bool _topicInitialized = false;
    private int _consecutiveKafkaFailures = 0;
    private DateTime? _lastKafkaFailure = null;

    public PaymentSuccessEventConsumerService(
        ILogger<PaymentSuccessEventConsumerService> logger,
        IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("PaymentSuccessEventConsumerService başlatıldı");

        if (!_topicInitialized)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var topicInitializer = scope.ServiceProvider.GetRequiredService<KafkaTopicInitializer>();
                await topicInitializer.InitializeTopicsAsync();
                
                await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
                
                _topicInitialized = true;
                _logger.LogInformation("Kafka topic initialization tamamlandı.");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Topic initialization başarısız, devam ediliyor: {Error}", ex.Message);
            }
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var kafkaConsumer = scope.ServiceProvider.GetRequiredService<KafkaConsumerService<PaymentSuccessEvent>>();

                _logger.LogInformation("Kafka'dan PaymentSuccessEvent bekleniyor...");

                try
                {
                    var paymentSuccessEvent = await kafkaConsumer.Consume("payment-success", stoppingToken);

                    if (paymentSuccessEvent != null)
                    {
                        // Reset failure counter on successful consume
                        _consecutiveKafkaFailures = 0;
                        _lastKafkaFailure = null;
                        
                        var eventData = paymentSuccessEvent;
                        _logger.LogInformation("PaymentSuccessEvent alındı: UserId={UserId}, AccountId={AccountId}, Amount={Amount}, TransactionId={TransactionId}",
                            eventData.UserId, eventData.AccountId, eventData.Amount, eventData.PaymentTransactionId);

                        _ = Task.Run(async () =>
                        {
                            try
                            {
                                using var taskScope = _serviceProvider.CreateScope();
                                var taskAccountRepository = taskScope.ServiceProvider.GetRequiredService<IAccountRepository>();
                                var taskLogger = taskScope.ServiceProvider.GetRequiredService<ILogger<PaymentSuccessEventConsumerService>>();

                                var account = await taskAccountRepository.GetAsync(a => a.Id == eventData.AccountId && a.UserId == eventData.UserId);
                                if (account == null)
                                {
                                    taskLogger.LogWarning("PaymentSuccessEvent için hesap bulunamadı: AccountId={AccountId}, UserId={UserId}", 
                                        eventData.AccountId, eventData.UserId);
                                    return;
                                }

                                account.Balance += eventData.Amount;
                                await taskAccountRepository.UpdateAsync(account);

                                taskLogger.LogInformation("Bakiye başarıyla güncellendi: AccountId={AccountId}, UserId={UserId}, Amount={Amount}, NewBalance={NewBalance}, TransactionId={TransactionId}",
                                    account.Id, account.UserId, eventData.Amount, account.Balance, eventData.PaymentTransactionId);
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "PaymentSuccessEvent işlenirken hata oluştu: AccountId={AccountId}, UserId={UserId}, Amount={Amount}, TransactionId={TransactionId}", 
                                    eventData.AccountId, eventData.UserId, eventData.Amount, eventData.PaymentTransactionId);
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
                    if (consumeEx.Error.Code == ErrorCode.UnknownTopicOrPart)
                    {
                        _logger.LogWarning(consumeEx, "Topic bulunamadı: payment-success. Topic oluşturulmaya çalışılıyor...");
                        try
                        {
                            using var initScope = _serviceProvider.CreateScope();
                            var topicInitializer = initScope.ServiceProvider.GetRequiredService<KafkaTopicInitializer>();
                            await topicInitializer.InitializeTopicsAsync();
                            _logger.LogInformation("Topic başarıyla oluşturuldu. Consumer devam ediyor...");
                            await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
                        }
                        catch (Exception initEx)
                        {
                            _logger.LogError(initEx, "Topic oluşturma başarısız: {Error}", initEx.Message);
                            await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
                        }
                    }
                    else
                    {
                        _logger.LogError(consumeEx, "Kafka mesajı deserialize edilemedi (key/value hatası). Mesaj skip ediliyor: {Error}", consumeEx.Message);
                        await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
                    }
                }
                catch (System.Text.Json.JsonException jsonEx)
                {
                    _logger.LogError(jsonEx, "JSON deserialization hatası. Mesaj skip ediliyor: {Error}", jsonEx.Message);
                    await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
                }
                catch (Confluent.Kafka.KafkaException kafkaEx)
                {
                    if (kafkaEx.Error.Code == ErrorCode.UnknownTopicOrPart)
                    {
                        _logger.LogWarning(kafkaEx, "Topic bulunamadı: payment-success. Topic oluşturulmaya çalışılıyor...");
                        try
                        {
                            using var initScope2 = _serviceProvider.CreateScope();
                            var topicInitializer = initScope2.ServiceProvider.GetRequiredService<KafkaTopicInitializer>();
                            await topicInitializer.InitializeTopicsAsync();
                            _logger.LogInformation("Topic başarıyla oluşturuldu. Consumer devam ediyor...");
                            await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
                            _consecutiveKafkaFailures = 0; // Reset on success
                        }
                        catch (Exception initEx)
                        {
                            _logger.LogError(initEx, "Topic oluşturma başarısız: {Error}", initEx.Message);
                            await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
                        }
                    }
                    else if (kafkaEx.Error.Code == ErrorCode.Local_Transport || 
                             
                             kafkaEx.Message?.Contains("brokers are down", StringComparison.OrdinalIgnoreCase) == true ||
                             kafkaEx.Message?.Contains("Connection setup timed out", StringComparison.OrdinalIgnoreCase) == true)
                    {
                        // Broker down hatası - exponential backoff
                        _consecutiveKafkaFailures++;
                        _lastKafkaFailure = DateTime.UtcNow;
                        
                        // Exponential backoff: 5, 10, 20, 30, max 60 seconds
                        int delaySeconds = Math.Min(5 * (int)Math.Pow(2, Math.Min(_consecutiveKafkaFailures - 1, 4)), 60);
                        
                        // Her 20 hatada bir log et (spam'i azalt)
                        if (_consecutiveKafkaFailures % 20 == 0)
                        {
                            _logger.LogWarning("Kafka broker unavailable (consecutive failures: {Failures}). Retrying in {Delay} seconds. Service continues without Kafka events. Payment balance updates will be handled via gRPC fallback.", 
                                _consecutiveKafkaFailures, delaySeconds);
                        }
                        else
                        {
                            _logger.LogDebug("Kafka broker unavailable (silenced log #{Failure}). Retrying in {Delay} seconds.", 
                                _consecutiveKafkaFailures, delaySeconds);
                        }
                        
                        await Task.Delay(TimeSpan.FromSeconds(delaySeconds), stoppingToken);
                    }
                    else
                    {
                        _logger.LogError(kafkaEx, "Kafka exception: {Error}", kafkaEx.Message);
                        await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                        _consecutiveKafkaFailures = 0; // Reset on other errors
                    }
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("PaymentSuccessEventConsumerService cancellation requested");
                break;
            }
            catch (Exception ex)
            {
                // Kafka bağlantı hatalarını kontrol et
                if (ex.Message?.Contains("brokers are down", StringComparison.OrdinalIgnoreCase) == true ||
                    ex.Message?.Contains("Connection setup timed out", StringComparison.OrdinalIgnoreCase) == true ||
                    ex.InnerException?.Message?.Contains("brokers are down", StringComparison.OrdinalIgnoreCase) == true ||
                    ex.InnerException?.Message?.Contains("Connection setup timed out", StringComparison.OrdinalIgnoreCase) == true)
                {
                    _consecutiveKafkaFailures++;
                    _lastKafkaFailure = DateTime.UtcNow;
                    
                    int delaySeconds = Math.Min(5 * (int)Math.Pow(2, Math.Min(_consecutiveKafkaFailures - 1, 4)), 60);
                    
                    if (_consecutiveKafkaFailures % 20 == 0)
                    {
                        _logger.LogWarning(ex, "Kafka connection error in PaymentSuccessEventConsumerService (consecutive failures: {Failures}). Retrying in {Delay} seconds. Service continues.", 
                            _consecutiveKafkaFailures, delaySeconds);
                    }
                    else
                    {
                        _logger.LogDebug(ex, "Kafka connection error (silenced log #{Failure}). Retrying in {Delay} seconds.", 
                            _consecutiveKafkaFailures, delaySeconds);
                    }
                    
                    await Task.Delay(TimeSpan.FromSeconds(delaySeconds), stoppingToken);
                }
                else
                {
                    _logger.LogError(ex, "PaymentSuccessEventConsumerService genel hata: {Error}", ex.Message);
                    await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
                    _consecutiveKafkaFailures = 0; // Reset on other errors
                }
            }
        }

        _logger.LogInformation("PaymentSuccessEventConsumerService durduruldu");
    }
}
