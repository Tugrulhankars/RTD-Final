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
        using var producer = new ProducerBuilder<TKey, string>(_producerConfig)
            .SetValueSerializer(Serializers.Utf8)
            .Build();

        var message = new Message<TKey, string>
        {
            Key = key,
            Value = JsonConvert.SerializeObject(value)
        };

        await producer.ProduceAsync(topic, message);
    }
}
