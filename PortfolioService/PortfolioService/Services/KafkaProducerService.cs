using Confluent.Kafka;
using Newtonsoft.Json;
using PortfolioService.Services.Abstracts;

namespace PortfolioService.Services;

public class KafkaProducerService : IKafkaService
{
    private readonly ProducerConfig _producerConfig;

    public KafkaProducerService(ProducerConfig producerConfig)
    {
        _producerConfig = producerConfig;
    }

    public async Task ProduceAsync<TKey, TValue>(string topic, TKey key, TValue value)
    {
        try
        {
            using var producer = new ProducerBuilder<TKey, string>(_producerConfig)
                .SetValueSerializer(Serializers.Utf8)
                .SetErrorHandler((producer, error) =>
                {
                    Console.WriteLine($"Kafka producer error: {error.Reason}");
                })
                .Build();

            var message = new Message<TKey, string>
            {
                Key = key,
                Value = JsonConvert.SerializeObject(value)
            };

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            await producer.ProduceAsync(topic, message, cts.Token);
        }
        catch (KafkaException kafkaEx)
        {
            Console.WriteLine($"Kafka produce error (non-critical): {kafkaEx.Message}");
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine($"Kafka produce timeout - Kafka might be unavailable");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Kafka produce error: {ex.Message}");
        }
    }
}
