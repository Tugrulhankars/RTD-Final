namespace PortfolioService.Services.Abstracts;

public interface IKafkaService
{
    Task ProduceAsync<TKey, TValue>(string topic, TKey key, TValue value);
}
