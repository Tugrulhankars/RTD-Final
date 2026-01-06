using Confluent.Kafka;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace AccountService.Configuration;

public class ValueSerializer<T> : IDeserializer<T> where T : class
{
    private static readonly ILogger? _logger;

    public T Deserialize(ReadOnlySpan<byte> data, bool isNull, SerializationContext context)
    {
        if (isNull)
        {
            Console.WriteLine("[ValueSerializer] Received null data");
            return null!;
        }

        if (data.IsEmpty)
        {
            Console.WriteLine("[ValueSerializer] Received empty data buffer");
            return null!;
        }

        try
        {
            var jsonString = System.Text.Encoding.UTF8.GetString(data);
            
            Console.WriteLine($"[ValueSerializer] Raw JSON received ({data.Length} bytes): {jsonString}");
            
            if (string.IsNullOrWhiteSpace(jsonString))
            {
                Console.WriteLine("[ValueSerializer] Empty or whitespace JSON string received");
                return null!;
            }
            
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                AllowTrailingCommas = true,
                ReadCommentHandling = JsonCommentHandling.Skip
            };
            
            var result = JsonSerializer.Deserialize<T>(jsonString, options);
            if (result == null)
            {
                Console.WriteLine($"[ValueSerializer] Deserialization returned null for type {typeof(T).Name}. JSON: {jsonString}");
                return null!;
            }
            
            Console.WriteLine($"[ValueSerializer] Successfully deserialized to {typeof(T).Name}");
            return result;
        }
        catch (JsonException ex)
        {
            var jsonString = System.Text.Encoding.UTF8.GetString(data);
            Console.WriteLine($"[ValueSerializer] JSON deserialization error: {ex.Message}");
            Console.WriteLine($"[ValueSerializer] JSON Path: {ex.Path}, Line: {ex.LineNumber}, BytePosition: {ex.BytePositionInLine}");
            Console.WriteLine($"[ValueSerializer] Full JSON content: {jsonString}");
            Console.WriteLine($"[ValueSerializer] Data length: {data.Length} bytes");
            throw;
        }
        catch (Exception ex)
        {
            var jsonString = System.Text.Encoding.UTF8.GetString(data);
            Console.WriteLine($"[ValueSerializer] Unexpected deserialization error: {ex.Message}");
            Console.WriteLine($"[ValueSerializer] JSON content: {jsonString}");
            Console.WriteLine($"[ValueSerializer] Exception type: {ex.GetType().Name}");
            throw;
        }
    }
}
