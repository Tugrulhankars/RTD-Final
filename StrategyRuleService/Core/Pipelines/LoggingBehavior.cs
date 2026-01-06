using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
namespace Core.Pipelines;
public class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly RecyclableMemoryStreamManager _recyclableMemoryStreamManager;
    public LoggingBehavior(IHttpContextAccessor httpContextAccessor, RecyclableMemoryStreamManager recyclableMemoryStream)
    {
        _httpContextAccessor = httpContextAccessor;
        _recyclableMemoryStreamManager = recyclableMemoryStream;
    }
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var context = _httpContextAccessor.HttpContext;
        if (context != null)
        {
            await AddRequestContentLog(context);
            var originalResponse = context.Response.Body;
            await using var responseBodyMemoryStream = _recyclableMemoryStreamManager.GetStream();
            context.Response.Body = responseBodyMemoryStream;
            var response = await next();
            responseBodyMemoryStream.Position = 0;
            using var reader = new StreamReader(responseBodyMemoryStream);
            var responseBodyText = await reader.ReadToEndAsync();
            Console.WriteLine($"Response: {responseBodyText}");
            responseBodyMemoryStream.Position = 0;
            await responseBodyMemoryStream.CopyToAsync(originalResponse);
            context.Response.Body = originalResponse;
            return response;
        }
        return await next();
    }
    private async Task AddRequestContentLog(HttpContext context)
    {
        context.Request.EnableBuffering();
        var requestBodyStreamReader = new StreamReader(context.Request.Body);
        var requestBodyContent = await requestBodyStreamReader.ReadToEndAsync();
        context.Request.Body.Position = 0;
    }
    private async Task AddResponse(IHttpContextAccessor context)
    {
        var originalResponse = context.HttpContext.Response.Body;
        await using var responseBodyMemoryStream = _recyclableMemoryStreamManager.GetStream();
        context.HttpContext.Response.Body = responseBodyMemoryStream;
        responseBodyMemoryStream.Position = 0;
        await responseBodyMemoryStream.CopyToAsync(originalResponse);
    }
}
