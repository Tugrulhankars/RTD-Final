using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using System.Linq;
using System.Threading.RateLimiting;
using Yarp.ReverseProxy.Forwarder;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));
builder.Services.AddHttpForwarder();

// Error policy for handling connection failures
// Note: IForwarderErrorPolicy is not available in YARP 2.3.0, removed for compatibility
// builder.Services.AddSingleton<IForwarderErrorPolicy, CustomErrorPolicy>();

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(opt =>
{
    var jwtKey = builder.Configuration["Jwt:Key"] ?? "";
    var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "";
    var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "";
    
    byte[] keyBytes;
    try
    {
        if (jwtKey.Length == 64 && System.Text.RegularExpressions.Regex.IsMatch(jwtKey, @"^[0-9a-fA-F]+$"))
        {
            keyBytes = Convert.FromHexString(jwtKey);
        }
        else
        {
            keyBytes = Convert.FromBase64String(jwtKey);
        }
    }
    catch
    {
        keyBytes = System.Text.Encoding.UTF8.GetBytes(jwtKey);
    }
    
    opt.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
    {
        ValidateIssuer = jwtIssuer != "*",
        ValidateAudience = jwtAudience != "*",
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtIssuer != "*" ? jwtIssuer : null,
        ValidAudience = jwtAudience != "*" ? jwtAudience : null,
        IssuerSigningKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(keyBytes)
    };
    var publicPaths = new[] { "/api/v1/auth/register", "/api/v1/auth/login", "/api/v1/auth/login/otp", 
                              "/api/v1/users/verifyuser", "/api/v1/users/forgot-password", 
                              "/api/v1/users/reset-password-otp", "/api/v1/users/resend-otp" };
    
    opt.Events = new Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var path = context.Request.Path.Value?.ToLower() ?? "";
            if (publicPaths.Any(p => path.Contains(p)))
            {
                context.Token = null;
            }
            return System.Threading.Tasks.Task.CompletedTask;
        },
        OnAuthenticationFailed = context =>
        {
            var path = context.Request.Path.Value?.ToLower() ?? "";
            if (publicPaths.Any(p => path.Contains(p)))
            {
                context.NoResult();
                return System.Threading.Tasks.Task.CompletedTask;
            }
            
            System.Console.WriteLine($"JWT Authentication failed: {context.Exception?.Message}");
            return System.Threading.Tasks.Task.CompletedTask;
        },
        OnChallenge = context =>
        {
            var path = context.Request.Path.Value?.ToLower() ?? "";
            if (publicPaths.Any(p => path.Contains(p)))
            {
                context.HandleResponse();
                context.Response.Clear();
                context.Response.StatusCode = 200;
                return System.Threading.Tasks.Task.CompletedTask;
            }
            
            return System.Threading.Tasks.Task.CompletedTask;
        }
    };
    opt.RequireHttpsMetadata = false;
});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("Authenticated", policy => policy.RequireAuthenticatedUser());
});

builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("fixed", options =>
    {
        options.AutoReplenishment = true;
        options.PermitLimit = 50;
        options.Window = TimeSpan.FromMinutes(1);
    });
});

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins(
                "http://localhost:3000", 
                "http://localhost:5173",
                "http://localhost:5174",
                "http://localhost:5175",
                "http://127.0.0.1:3000",
                "http://127.0.0.1:5173",
                "http://127.0.0.1:5174",
                "http://127.0.0.1:5175"
              )
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

var app = builder.Build();

app.UseCors();
app.UseRateLimiter();

// Error handling middleware for reverse proxy
app.Use(async (context, next) =>
{
    try
    {
        await next();
        
        // Check if response is 502 Bad Gateway and convert to 503 with better message
        if (context.Response.StatusCode == 502 && !context.Response.HasStarted)
        {
            var path = context.Request.Path.Value ?? "";
            var serviceName = GetServiceNameFromPath(path);
            var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
            
            logger.LogWarning(
                "502 Bad Gateway - Backend servisine bağlanılamadı: {ServiceName}, Path: {Path}",
                serviceName, path);
            
            context.Response.StatusCode = 503;
            context.Response.ContentType = "application/json";
            
            var errorResponse = new
            {
                success = false,
                message = $"{serviceName} servisine bağlanılamadı. Servis çalışmıyor olabilir.",
                service = serviceName,
                path = path,
                error = "Service Unavailable",
                suggestion = $"{serviceName} servisinin çalıştığından emin olun."
            };
            
            await context.Response.WriteAsJsonAsync(errorResponse);
            return;
        }
    }
    catch (System.Net.Http.HttpRequestException httpEx)
    {
        var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
        
        if (httpEx.InnerException is System.Net.Sockets.SocketException socketEx && 
            (socketEx.SocketErrorCode == System.Net.Sockets.SocketError.ConnectionRefused || 
             socketEx.ErrorCode == 10061))
        {
            var path = context.Request.Path.Value ?? "";
            var serviceName = GetServiceNameFromPath(path);
            
            logger.LogWarning(
                "Backend servisine bağlanılamadı: {ServiceName}, Path: {Path}, Error: {Error}",
                serviceName, path, httpEx.Message);
            
            if (!context.Response.HasStarted)
            {
                context.Response.StatusCode = 503;
                context.Response.ContentType = "application/json";
                
                var errorResponse = new
                {
                    success = false,
                    message = $"{serviceName} servisine bağlanılamadı. Servis çalışmıyor olabilir.",
                    service = serviceName,
                    path = path,
                    error = "Service Unavailable",
                    suggestion = $"{serviceName} servisinin çalıştığından emin olun."
                };
                
                await context.Response.WriteAsJsonAsync(errorResponse);
            }
            return;
        }
        
        throw;
    }
    catch (Exception ex)
    {
        var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Beklenmeyen hata: {Error}", ex.Message);
        
        if (!context.Response.HasStarted)
        {
            context.Response.StatusCode = 500;
            context.Response.ContentType = "application/json";
            
            var errorResponse = new
            {
                success = false,
                message = "Sunucu hatası oluştu.",
                error = ex.Message
            };
            
            await context.Response.WriteAsJsonAsync(errorResponse);
        }
    }
});

app.UseAuthentication();
app.UseAuthorization();

app.MapReverseProxy();

app.Run();

static string GetServiceNameFromPath(string path)
{
    if (string.IsNullOrEmpty(path)) return "Unknown";
    
    path = path.ToLower();
    
    if (path.StartsWith("/api/v1/auth") || path.StartsWith("/api/v1/users"))
        return "AuthUser-Service";
    if (path.StartsWith("/api/payment"))
        return "Payment-Service";
    if (path.StartsWith("/api/portfolio"))
        return "Portfolio-Service";
    if (path.StartsWith("/api/account"))
        return "Account-Service";
    if (path.StartsWith("/api/marketdata") || path.StartsWith("/api/strategy"))
        return "StrategyRule-Service";
    if (path.StartsWith("/api/v1/trade"))
        return "Trading-Service";
    
    return "Backend-Service";
}

// Custom error policy for YARP
// Note: IForwarderErrorPolicy is not available in YARP 2.3.0, removed for compatibility
/*
class CustomErrorPolicy : IForwarderErrorPolicy
{
    public ForwarderErrorPolicyAction HandleError(ForwarderErrorContext errorContext)
    {
        var exception = errorContext.Exception;
        var logger = errorContext.HttpContext.RequestServices.GetRequiredService<ILogger<CustomErrorPolicy>>();
        
        if (exception is System.Net.Http.HttpRequestException httpEx)
        {
            if (httpEx.InnerException is System.Net.Sockets.SocketException socketEx && 
                (socketEx.SocketErrorCode == System.Net.Sockets.SocketError.ConnectionRefused || 
                 socketEx.ErrorCode == 10061))
            {
                var path = errorContext.HttpContext.Request.Path.Value ?? "";
                var serviceName = GetServiceNameFromPath(path);
                
                logger.LogWarning(
                    "Backend servisine bağlanılamadı: {ServiceName}, Path: {Path}, Error: {Error}",
                    serviceName, path, httpEx.Message);
                
                errorContext.HttpContext.Response.StatusCode = 503;
                errorContext.HttpContext.Response.ContentType = "application/json";
                
                var errorResponse = new
                {
                    success = false,
                    message = $"{serviceName} servisine bağlanılamadı. Servis çalışmıyor olabilir.",
                    service = serviceName,
                    path = path,
                    error = "Service Unavailable",
                    suggestion = $"{serviceName} servisinin çalıştığından emin olun."
                };
                
                // Use GetAwaiter().GetResult() instead of Wait() to avoid potential deadlocks
                errorContext.HttpContext.Response.WriteAsJsonAsync(errorResponse).GetAwaiter().GetResult();
                
                return ForwarderErrorPolicyAction.Response;
            }
        }
        
        return ForwarderErrorPolicyAction.Default;
    }
}
*/
