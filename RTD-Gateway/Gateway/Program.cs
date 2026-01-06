using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using System.Linq;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

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
        policy.WithOrigins("http://localhost:3000", "http://localhost:5173")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

var app = builder.Build();

app.UseCors();
app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

app.MapReverseProxy();

app.Run();
