using System.Net;
using System.Text.Json;
using Microsoft.Data.SqlClient;
namespace Api.Middleware;
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;
    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Bir hata oluştu: {Message}", ex.Message);
            await HandleExceptionAsync(context, ex);
        }
    }
    private static Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var code = HttpStatusCode.InternalServerError;
        var result = string.Empty;
        
        // Veritabanı bağlantı hatası kontrolü
        var isDbError = exception is SqlException || 
                       (exception.InnerException is SqlException) ||
                       exception.Message.Contains("network-related", StringComparison.OrdinalIgnoreCase) ||
                       exception.Message.Contains("instance-specific", StringComparison.OrdinalIgnoreCase) ||
                       exception.Message.Contains("server was not found", StringComparison.OrdinalIgnoreCase) ||
                       exception.Message.Contains("could not open a connection", StringComparison.OrdinalIgnoreCase) ||
                       exception.Message.Contains("A network-related or instance-specific error", StringComparison.OrdinalIgnoreCase);
        
        if (isDbError)
        {
            // Strategy endpoint'leri için boş liste döndür
            var path = context.Request.Path.Value?.ToLower() ?? "";
            if (path.Contains("getstrategiesbyuserid") || 
                path.Contains("getincompletestrategies") ||
                path.Contains("getcompletedstrategies"))
            {
                code = HttpStatusCode.OK;
                if (path.Contains("incomplete"))
                {
                    result = JsonSerializer.Serialize(new { IncompleteStrategies = new List<object>() });
                }
                else
                {
                    result = JsonSerializer.Serialize(new { Strategies = new List<object>() });
                }
            }
            else
            {
                code = HttpStatusCode.ServiceUnavailable;
                result = JsonSerializer.Serialize(new { error = "Veritabanı bağlantı hatası. Lütfen daha sonra tekrar deneyin." });
            }
        }
        else
        {
            switch (exception)
            {
                case ArgumentNullException:
                case ArgumentException:
                    code = HttpStatusCode.BadRequest;
                    result = JsonSerializer.Serialize(new { error = exception.Message });
                    break;
                case UnauthorizedAccessException:
                    code = HttpStatusCode.Unauthorized;
                    result = JsonSerializer.Serialize(new { error = "Yetkisiz erişim." });
                    break;
                case KeyNotFoundException:
                    code = HttpStatusCode.NotFound;
                    result = JsonSerializer.Serialize(new { error = exception.Message });
                    break;
                default:
                    result = JsonSerializer.Serialize(new 
                    { 
                        error = "Bir hata oluştu. Lütfen daha sonra tekrar deneyin.",
                        detail = exception.Message 
                    });
                    break;
            }
        }
        
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)code;
        return context.Response.WriteAsync(result);
    }
}
