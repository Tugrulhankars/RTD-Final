using Confluent.Kafka;
using System.Text.Json;

namespace AccountService.Configuration;

public class KeySerializer<T> : IDeserializer<T>
{
    public T Deserialize(ReadOnlySpan<byte> data, bool isNull, SerializationContext context)
    {
        if (isNull)
        {
            Console.WriteLine("[KeySerializer] Received null key - returning default");
            return default(T)!;
        }

        if (data.IsEmpty)
        {
            Console.WriteLine("[KeySerializer] Received empty key - returning default");
            return default(T)!;
        }

        try
        {
            if (typeof(T) == typeof(int))
            {
                var stringValue = System.Text.Encoding.UTF8.GetString(data);
                if (int.TryParse(stringValue, out int intValue))
                {
                    return (T)(object)intValue;
                }
            }

            if (typeof(T) == typeof(string))
            {
                var stringValue = System.Text.Encoding.UTF8.GetString(data);
                return (T)(object)stringValue;
            }

            return JsonSerializer.Deserialize<T>(data) ?? default(T)!;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[KeySerializer] Error deserializing key: {ex.Message}. Returning default. Data: {System.Text.Encoding.UTF8.GetString(data)}");
            return default(T)!;
        }
    }
}
