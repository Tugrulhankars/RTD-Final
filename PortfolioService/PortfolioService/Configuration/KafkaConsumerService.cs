using Confluent.Kafka;
using Confluent.Kafka.Admin;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace PortfolioService.Configuration;

public class KafkaConsumerService<T> where T : class
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<KafkaConsumerService<T>>? _logger;
    private readonly HashSet<string> _initializedTopics = new HashSet<string>();

    public KafkaConsumerService(IConfiguration configuration, ILogger<KafkaConsumerService<T>>? logger = null)
    {
        _configuration = configuration;
        _logger = logger;
    }
    
    public async Task EnsureTopicInitializedAsync(string topicName, CancellationToken cancellationToken = default)
    {
        if (_initializedTopics.Contains(topicName))
        {
            return;
        }

        var bootstrapServers = _configuration["Kafka:BootstrapServers"] ?? "localhost:19092";
        
        try
        {
            await EnsureTopicExistsAsync(bootstrapServers, topicName, cancellationToken);
            
            await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
            
            _initializedTopics.Add(topicName);
            _logger?.LogInformation("Topic initialization tamamlandı: {TopicName}", topicName);
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Topic initialization başarısız (non-critical): {TopicName}, Error={Error}", 
                topicName, ex.Message);
        }
    }

    public async Task<T?> Consume(string topicName, CancellationToken cancellationToken = default)
    {
        IConsumer<int, T>? consumer = null;
        try
        {
            var bootstrapServers = _configuration["Kafka:BootstrapServers"] ?? "localhost:19092";
            var groupId = _configuration["Kafka:GroupId"] ?? "PortfolioService";
            
            _logger?.LogInformation("Kafka consumer başlatılıyor: BootstrapServers={BootstrapServers}, Topic={Topic}, GroupId={GroupId}", 
                bootstrapServers, topicName, groupId);

            await EnsureTopicInitializedAsync(topicName, cancellationToken);

            var config = new ConsumerConfig()
            {
                BootstrapServers = bootstrapServers,
                GroupId = groupId,
                AutoOffsetReset = Enum.TryParse<AutoOffsetReset>(_configuration["Kafka:AutoOffsetReset"], out var offsetReset) 
                    ? offsetReset 
                    : AutoOffsetReset.Latest,
                SessionTimeoutMs = 10000,
                ApiVersionRequestTimeoutMs = 10000,
                MetadataMaxAgeMs = 300000,
                SocketConnectionSetupTimeoutMs = 2000,
                MaxPollIntervalMs = 300000,
                EnableAutoCommit = true
            };

            consumer = new ConsumerBuilder<int, T>(config)
                .SetKeyDeserializer(new KeySerializer<int>())
                .SetValueDeserializer(new ValueSerializer<T>())
                .SetErrorHandler((consumer, error) =>
                {
                    _logger?.LogWarning("Kafka consumer error: {Error}", error.Reason);
                })
                .Build();

            consumer.Subscribe(topicName);
            _logger?.LogInformation("Kafka topic'e subscribe edildi: {Topic}", topicName);

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(30));

            try
            {
                var consumerResult = consumer.Consume(cts.Token);
                if (consumerResult != null && !consumerResult.IsPartitionEOF)
                {
                    _logger?.LogInformation("Kafka mesajı alındı: Topic={Topic}, Partition={Partition}, Offset={Offset}", 
                        consumerResult.Topic, consumerResult.Partition, consumerResult.Offset);
                    
                    try
                    {
                        T @event = consumerResult.Message.Value;
                        if (@event == null)
                        {
                            _logger?.LogWarning("Kafka mesajı deserialize edildi ama null döndü.");
                        }
                        else
                        {
                            _logger?.LogInformation("Kafka mesajı başarıyla deserialize edildi: {EventType}", typeof(T).Name);
                        }
                        return @event;
                    }
                    catch (Exception deserializeEx)
                    {
                        _logger?.LogError(deserializeEx, "Kafka mesajı deserialize edilirken hata: {Error}", 
                            deserializeEx.Message);
                        return null;
                    }
                }
            }
            catch (OperationCanceledException)
            {
                _logger?.LogDebug("Kafka consume timeout - mesaj bekleniyor...");
                return null;
            }
        }
        catch (Confluent.Kafka.ConsumeException consumeEx)
        {
            _logger?.LogError(consumeEx, "Kafka consume exception (key/value deserialization hatası olabilir): {Error}", consumeEx.Message);
            return null;
        }
        catch (KafkaException kafkaEx)
        {
            _logger?.LogError(kafkaEx, "Kafka bağlantı hatası - Kafka down olabilir: {Error}", kafkaEx.Message);
            return null;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Kafka consume hatası: {Error}", ex.Message);
            return null;
        }
        finally
        {
            try
            {
                consumer?.Close();
                consumer?.Dispose();
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Kafka consumer kapatılırken hata: {Error}", ex.Message);
            }
        }

        return null;
    }

    private async Task EnsureTopicExistsAsync(string bootstrapServers, string topicName, CancellationToken cancellationToken)
    {
        try
        {
            var adminConfig = new AdminClientConfig
            {
                BootstrapServers = bootstrapServers
            };

            using var adminClient = new AdminClientBuilder(adminConfig)
                .SetErrorHandler((client, error) =>
                {
                    _logger?.LogWarning("Kafka AdminClient error while checking topic: {Error}", error.Reason);
                })
                .Build();

            var metadata = adminClient.GetMetadata(TimeSpan.FromSeconds(10));
            var topicExists = metadata.Topics.Any(t => t.Topic == topicName);

            if (!topicExists)
            {
                _logger?.LogInformation("Topic bulunamadı, oluşturuluyor: {TopicName}", topicName);

                var topicSpec = new TopicSpecification
                {
                    Name = topicName,
                    NumPartitions = 1,
                    ReplicationFactor = 1,
                    Configs = new Dictionary<string, string>
                    {
                        { "retention.ms", "604800000" },
                        { "cleanup.policy", "delete" }
                    }
                };

                var createOptions = new CreateTopicsOptions
                {
                    RequestTimeout = TimeSpan.FromSeconds(30),
                    OperationTimeout = TimeSpan.FromSeconds(30)
                };

                try
                {
                    adminClient.CreateTopicsAsync(
                        new[] { topicSpec }, 
                        createOptions);
                    
                    await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
                    _logger?.LogInformation("Topic başarıyla oluşturuldu: {TopicName}", topicName);
                }
                catch (CreateTopicsException ex)
                {
                    var topicReport = ex.Results?.FirstOrDefault(r => r.Topic == topicName);
                    if (topicReport != null)
                    {
                        if (topicReport.Error.Code == ErrorCode.TopicAlreadyExists)
                        {
                            _logger?.LogInformation("Topic zaten mevcut (concurrent creation): {TopicName}", topicName);
                        }
                        else
                        {
                            _logger?.LogWarning("Topic oluşturulurken hata: {TopicName}, Error={Error}", 
                                topicName, topicReport.Error.Reason);
                        }
                    }
                    else
                    {
                        _logger?.LogWarning("Topic oluşturulurken hata: {TopicName}, Exception={Exception}", 
                            topicName, ex.Message);
                    }
                }
            }
            else
            {
                _logger?.LogDebug("Topic zaten mevcut: {TopicName}", topicName);
            }
        }
        catch (KafkaException kafkaEx)
        {
            _logger?.LogWarning(kafkaEx, 
                "Topic kontrolü sırasında Kafka hatası (non-critical, devam ediliyor): {Error}", 
                kafkaEx.Message);
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, 
                "Topic kontrolü sırasında beklenmeyen hata (non-critical, devam ediliyor): {Error}", 
                ex.Message);
        }
    }
}
