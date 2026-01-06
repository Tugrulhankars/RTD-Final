using Confluent.Kafka;
using Confluent.Kafka.Admin;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AccountService.Configuration;

public class KafkaTopicInitializer
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<KafkaTopicInitializer> _logger;

    public KafkaTopicInitializer(IConfiguration configuration, ILogger<KafkaTopicInitializer> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task InitializeTopicsAsync()
    {
        var bootstrapServers = _configuration["Kafka:BootstrapServers"] ?? "localhost:19092";
        
        _logger.LogInformation("Kafka topic initialization başlatılıyor: BootstrapServers={BootstrapServers}", bootstrapServers);

        var adminConfig = new AdminClientConfig
        {
            BootstrapServers = bootstrapServers
        };

        using var adminClient = new AdminClientBuilder(adminConfig)
            .SetErrorHandler((client, error) =>
            {
                _logger.LogWarning("Kafka AdminClient error: {Error}", error.Reason);
            })
            .Build();

        try
        {
            var metadata = adminClient.GetMetadata(TimeSpan.FromSeconds(10));
            var existingTopics = metadata.Topics.Select(t => t.Topic).ToHashSet();
            _logger.LogInformation("Mevcut Kafka topic'leri: {Topics}", string.Join(", ", existingTopics));

            var topicsToCreate = new List<TopicSpecification>
            {
                new TopicSpecification
                {
                    Name = "payment-success",
                    NumPartitions = 1,
                    ReplicationFactor = 1,
                    Configs = new Dictionary<string, string>
                    {
                        { "retention.ms", "604800000" },
                        { "cleanup.policy", "delete" }
                    }
                }
            };

            var topicsToCreateFiltered = topicsToCreate
                .Where(t => !existingTopics.Contains(t.Name))
                .ToList();

            if (topicsToCreateFiltered.Count == 0)
            {
                _logger.LogInformation("Tüm gerekli topic'ler zaten mevcut. Topic oluşturma gerekmiyor.");
                return;
            }

            _logger.LogInformation("Oluşturulacak topic'ler: {Topics}", 
                string.Join(", ", topicsToCreateFiltered.Select(t => t.Name)));

            var createTopicsOptions = new CreateTopicsOptions
            {
                RequestTimeout = TimeSpan.FromSeconds(30),
                OperationTimeout = TimeSpan.FromSeconds(30)
            };

            try
            {
                adminClient.CreateTopicsAsync(
                    topicsToCreateFiltered, 
                    createTopicsOptions);
                
                await Task.Delay(TimeSpan.FromSeconds(2));
                
                foreach (var topic in topicsToCreateFiltered)
                {
                    _logger.LogInformation("Topic başarıyla oluşturuldu: {TopicName}", topic.Name);
                }
            }
            catch (CreateTopicsException ex)
            {
                foreach (var topic in topicsToCreateFiltered)
                {
                    var topicReport = ex.Results?.FirstOrDefault(r => r.Topic == topic.Name);
                    if (topicReport != null)
                    {
                        if (topicReport.Error.Code == ErrorCode.TopicAlreadyExists)
                        {
                            _logger.LogInformation("Topic zaten mevcut (concurrent creation): {TopicName}", topic.Name);
                        }
                        else
                        {
                            _logger.LogError("Topic oluşturulurken hata: {TopicName}, Error={Error}", 
                                topic.Name, topicReport.Error.Reason);
                        }
                    }
                    else
                    {
                        _logger.LogWarning("Topic oluşturulurken hata: {TopicName}, Exception={Exception}", 
                            topic.Name, ex.Message);
                    }
                }
            }

            _logger.LogInformation("Kafka topic initialization tamamlandı.");
        }
        catch (KafkaException kafkaEx)
        {
            _logger.LogError(kafkaEx, 
                "Kafka topic initialization hatası - Kafka'ya bağlanılamıyor olabilir: {Error}", 
                kafkaEx.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Kafka topic initialization beklenmeyen hatası: {Error}", ex.Message);
        }
    }
}
