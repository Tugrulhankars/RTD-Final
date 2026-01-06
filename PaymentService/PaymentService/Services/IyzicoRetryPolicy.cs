using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;
namespace PaymentService.Services;
public static class IyzicoRetryPolicy
{
    public static AsyncRetryPolicy CreateRetryPolicy(ILogger logger)
    {
        return Policy
            .Handle<System.Net.Http.HttpRequestException>()
            .Or<System.Net.Sockets.SocketException>()
            .Or<TaskCanceledException>()
            .OrInner<System.Net.Http.HttpRequestException>()
            .OrInner<System.Net.Sockets.SocketException>()
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: retryAttempt =>
                {
                    var delay = TimeSpan.FromSeconds(Math.Pow(2, retryAttempt));
                    logger?.LogWarning(
                        "Iyzico API çağrısı başarısız, retry {RetryAttempt}/3 yapılıyor. {Delay} saniye bekleniyor...",
                        retryAttempt, delay.TotalSeconds);
                    return delay;
                },
                onRetry: (exception, timeSpan, retryCount, context) =>
                {
                    logger?.LogWarning(
                        exception,
                        "Iyzico API retry {RetryCount}/3: {ExceptionType} - {Message}. {Delay} saniye sonra tekrar denenecek.",
                        retryCount, exception.GetType().Name, exception.Message, timeSpan.TotalSeconds);
                }
            );
    }
    public static AsyncRetryPolicy CreateDnsRetryPolicy(ILogger logger)
    {
        return Policy
            .Handle<System.Net.Sockets.SocketException>(ex => 
                ex.SocketErrorCode == System.Net.Sockets.SocketError.HostNotFound ||
                ex.ErrorCode == 11001)
            .OrInner<System.Net.Sockets.SocketException>(ex => 
                ex.SocketErrorCode == System.Net.Sockets.SocketError.HostNotFound ||
                ex.ErrorCode == 11001)
            .WaitAndRetryAsync(
                retryCount: 2,
                sleepDurationProvider: retryAttempt =>
                {
                    var delay = TimeSpan.FromSeconds(retryAttempt);
                    logger?.LogWarning(
                        "DNS hatası tespit edildi, retry {RetryAttempt}/2 yapılıyor. {Delay} saniye bekleniyor...",
                        retryAttempt, delay.TotalSeconds);
                    return delay;
                },
                onRetry: (exception, timeSpan, retryCount, context) =>
                {
                    logger?.LogWarning(
                        exception,
                        "DNS hatası retry {RetryCount}/2: HostNotFound. {Delay} saniye sonra tekrar denenecek.",
                        retryCount, timeSpan.TotalSeconds);
                }
            );
    }
}
