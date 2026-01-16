using Confluent.Kafka;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text.Json;
namespace PaymentService.Services;
public interface IKafkaProducerService
{
    Task PublishAsync<T>(string topic, T message) where T : class;
}
public class KafkaProducerService : IKafkaProducerService
{
    private readonly ProducerConfig _producerConfig;
    private readonly ILogger<KafkaProducerService> _logger;
    public KafkaProducerService(IConfiguration configuration, ILogger<KafkaProducerService> logger)
    {
        _logger = logger;
        var bootstrapServers = configuration["Kafka:BootstrapServers"] ?? "localhost:9092";
        _producerConfig = new ProducerConfig
        {
            BootstrapServers = bootstrapServers,
            RequestTimeoutMs = 10000,
            MessageTimeoutMs = 30000,
            RetryBackoffMs = 100,
            EnableIdempotence = false
        };
        _logger.LogInformation("KafkaProducerService initialized with BootstrapServers: {BootstrapServers}", bootstrapServers);
    }
    public async Task PublishAsync<T>(string topic, T message) where T : class
    {
        try
        {
            using var producer = new ProducerBuilder<string, string>(_producerConfig)
                .SetValueSerializer(Serializers.Utf8)
                .SetErrorHandler((producer, error) =>
                {
                    _logger.LogWarning("Kafka producer error: {Error}", error.Reason);
                })
                .Build();
            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
            var messageJson = JsonSerializer.Serialize(message, options);
            var kafkaMessage = new Message<string, string>
            {
                Key = null,
                Value = messageJson
            };
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            var deliveryResult = await producer.ProduceAsync(topic, kafkaMessage, cts.Token);
            _logger.LogInformation("Kafka message published successfully: Topic={Topic}, Partition={Partition}, Offset={Offset}", 
                deliveryResult.Topic, deliveryResult.Partition, deliveryResult.Offset);
        }
        catch (KafkaException kafkaEx)
        {
            _logger.LogError(kafkaEx, "Kafka produce error (non-critical): {Error}", kafkaEx.Message);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Kafka produce timeout - Kafka might be unavailable");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Kafka produce error: {Error}", ex.Message);
        }
    }
}
