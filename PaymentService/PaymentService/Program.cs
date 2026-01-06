using PaymentService.Repositories;
using PaymentService.Services;
using PaymentService.Services.Impl;
using Microsoft.EntityFrameworkCore;
using System.Linq;
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.Configure<Iyzipay.Options>(options =>
{
    var apiKey = builder.Configuration["Iyzico:ApiKey"]?.Trim();
    var secretKey = builder.Configuration["Iyzico:SecretKey"]?.Trim();
    var baseUrlRaw = builder.Configuration["Iyzico:BaseUrl"];
    var baseUrl = baseUrlRaw?.Trim() ?? string.Empty;
    if (!string.IsNullOrEmpty(baseUrl))
    {
        baseUrl = new string(baseUrl.Where(c => !char.IsControl(c) && c != '\u200B' && c != '\uFEFF').ToArray()).Trim();
    }
    if (string.IsNullOrWhiteSpace(apiKey))
    {
        throw new InvalidOperationException("Iyzico:ApiKey configuration değeri bulunamadı veya boş. Lütfen appsettings.json dosyasına Iyzico:ApiKey ekleyin.");
    }
    if (string.IsNullOrWhiteSpace(secretKey))
    {
        throw new InvalidOperationException("Iyzico:SecretKey configuration değeri bulunamadı veya boş. Lütfen appsettings.json dosyasına Iyzico:SecretKey ekleyin.");
    }
    if (string.IsNullOrWhiteSpace(baseUrl))
    {
        throw new InvalidOperationException("Iyzico:BaseUrl configuration değeri bulunamadı veya boş. Lütfen appsettings.json dosyasına Iyzico:BaseUrl ekleyin.");
    }
    if (!baseUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
        !baseUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
    {
        throw new InvalidOperationException($"Iyzico:BaseUrl geçersiz format. URL 'http://' veya 'https://' ile başlamalı. Mevcut değer: '{baseUrl}' (Uzunluk: {baseUrl.Length})");
    }
    var expectedUrl = "https://sandbox-api.iyzipay.com";
    if (!baseUrl.Equals(expectedUrl, StringComparison.OrdinalIgnoreCase))
    {
        Console.WriteLine($"[Iyzico Config WARNING] BaseUrl beklenen değerden farklı: Beklenen='{expectedUrl}', Mevcut='{baseUrl}'");
    }
    options.ApiKey = apiKey;
    options.SecretKey = secretKey;
    options.BaseUrl = baseUrl;
    var baseUrlBytes = System.Text.Encoding.UTF8.GetBytes(baseUrl);
    Console.WriteLine($"[Iyzico Config] BaseUrl yüklendi: '{baseUrl}' (Uzunluk: {baseUrl.Length}, Bytes: {baseUrlBytes.Length}, " +
        $"StartsWithHttps: {baseUrl.StartsWith("https:
        $"EndsWithCom: {baseUrl.EndsWith(".com", StringComparison.OrdinalIgnoreCase)})");
});
builder.Services.AddDbContext<DatabaseContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
        ?? "Server=localhost;Database=PaymentServiceDb;Trusted_Connection=True;TrustServerCertificate=True;";
    options.UseSqlServer(connectionString, sqlOptions =>
    {
        sqlOptions.MaxBatchSize(100);
        sqlOptions.CommandTimeout(60);
        sqlOptions.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(30),
            errorNumbersToAdd: null
        );
    });
});
builder.Services.AddScoped<IPaymentRepository, PaymentRepository>();
builder.Services.AddScoped<IIyzicoPaymentService, IyzicoPaymentServiceImpl>();
builder.Services.AddScoped<IRabbitMQPublisher, RabbitMQPublisher>();
builder.Services.AddScoped<IKafkaProducerService, KafkaProducerService>();
builder.Services.AddHttpClient("AccountService", client =>
{
    var accountServiceUrl = builder.Configuration["AccountService:BaseUrl"] ?? "http://localhost:5239";
    client.BaseAddress = new Uri(accountServiceUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
})
.ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler()
{
    UseProxy = true,
    Proxy = System.Net.WebRequest.GetSystemWebProxy(),
});
:
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins("http://localhost:3000", "http://localhost:5173", "http://localhost:5286")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});
var app = builder.Build();
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseCors();
if (app.Environment.IsProduction())
{
    app.UseHttpsRedirection();
}
app.UseAuthorization();
app.MapControllers();
using (var scope = app.Services.CreateScope())
{
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    try
    {
        var db = scope.ServiceProvider.GetRequiredService<DatabaseContext>();
        logger.LogInformation("Veritabanı bağlantısı kontrol ediliyor: Database=RtdPayment-Service, Server=MetropolTilkisi");
        var canConnect = db.Database.CanConnect();
        logger.LogInformation("Veritabanı bağlantı kontrolü: CanConnect={CanConnect}", canConnect);
        if (!canConnect)
        {
            logger.LogWarning("Veritabanına bağlanılamadı. Veritabanı oluşturulmaya çalışılıyor...");
            db.Database.EnsureCreated();
            logger.LogInformation("Veritabanı başarıyla oluşturuldu (EnsureCreated).");
        }
        else
        {
            logger.LogInformation("Veritabanı mevcut. Migration'lar uygulanıyor...");
            db.Database.Migrate();
            logger.LogInformation("Migration'lar başarıyla uygulandı.");
        }
        logger.LogInformation("Veritabanı başarıyla hazırlandı.");
    }
    catch (Microsoft.Data.SqlClient.SqlException sqlEx)
    {
        logger.LogError(sqlEx, 
            "SQL Server hatası: ErrorCode={ErrorCode}, Number={Number}, State={State}, Class={Class}, Server={Server}, Message={Message}", 
            sqlEx.ErrorCode, sqlEx.Number, sqlEx.State, sqlEx.Class, sqlEx.Server, sqlEx.Message);
        if (sqlEx.Number == 18456)
        {
            logger.LogError("KULLANICI YETKİSİ HATASI: Windows kullanıcısı 'MetropolTilkisi\\karsl' SQL Server'a bağlanamıyor. " +
                "Lütfen SQL Server Management Studio'da kullanıcıya login ve database permission verin.");
        }
        else if (sqlEx.Number == 4060)
        {
            logger.LogError("VERİTABANI ERİŞİM HATASI: 'RtdPayment-Service' veritabanına erişilemiyor. " +
                "Kullanıcı 'MetropolTilkisi\\karsl' için db_owner veya db_datareader/db_datawriter yetkisi verin.");
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Veritabanı oluşturma/migration sırasında beklenmeyen hata: {Error}, InnerException={InnerException}", 
            ex.Message, ex.InnerException?.Message);
    }
}
app.Run();
