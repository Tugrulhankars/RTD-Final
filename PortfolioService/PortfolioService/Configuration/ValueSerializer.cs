using Confluent.Kafka;
using System.Text.Json;

namespace PortfolioService.Configuration;

public class ValueSerializer<T> : IDeserializer<T>
{
    public T Deserialize(ReadOnlySpan<byte> data, bool isNull, SerializationContext context)
    {
        return JsonSerializer.Deserialize<T>(data);
    }
}
