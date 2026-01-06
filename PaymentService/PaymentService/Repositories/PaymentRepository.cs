using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PaymentService.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
namespace PaymentService.Repositories;
public class PaymentRepository : IPaymentRepository
{
    private readonly DatabaseContext _databaseContext;
    private readonly ILogger<PaymentRepository>? _logger;
    private readonly string _databaseName;
    public PaymentRepository(DatabaseContext databaseContext, ILogger<PaymentRepository>? logger = null, IConfiguration? configuration = null)
    {
        _databaseContext = databaseContext;
        _logger = logger;
        _databaseName = ExtractDatabaseName(configuration);
    }
    private string ExtractDatabaseName(IConfiguration? configuration)
    {
        try
        {
            var connectionString = configuration?.GetConnectionString("DefaultConnection");
            if (string.IsNullOrEmpty(connectionString))
            {
                return "PaymentServiceDb";
            }
            var parts = connectionString.Split(';', StringSplitOptions.RemoveEmptyEntries);
            foreach (var part in parts)
            {
                var keyValue = part.Split('=', 2, StringSplitOptions.RemoveEmptyEntries);
                if (keyValue.Length == 2)
                {
                    var key = keyValue[0].Trim();
                    var value = keyValue[1].Trim();
                    if (key.Equals("Database", StringComparison.OrdinalIgnoreCase) ||
                        key.Equals("Initial Catalog", StringComparison.OrdinalIgnoreCase))
                    {
                        return value;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Connection string'den database adı parse edilemedi, default kullanılıyor");
        }
        return "PaymentServiceDb";
    }
    public async Task CreatePayment(Payment payment)
    {
        const int maxRetries = 3;
        int attempt = 0;
        while (attempt < maxRetries)
        {
            try
            {
                _logger?.LogDebug("Payment kaydı oluşturuluyor: PaymentId={PaymentId}, UserId={UserId}, Amount={Amount}, Attempt={Attempt}", 
                    payment.Id, payment.UserId, payment.Amount, attempt + 1);
                await _databaseContext.AddAsync(payment);
                await _databaseContext.SaveChangesAsync();
                _logger?.LogInformation("Payment başarıyla kaydedildi: PaymentId={PaymentId}, UserId={UserId}, Amount={Amount}", 
                    payment.Id, payment.UserId, payment.Amount);
                return;
            }
            catch (Microsoft.Data.SqlClient.SqlException sqlEx) when (
                sqlEx.Number == 4060 ||
                sqlEx.Number == 18456 ||
                sqlEx.Number == 2 ||
                sqlEx.Number == -2)
            {
                attempt++;
                _logger?.LogWarning(sqlEx, 
                    "Veritabanı bağlantı hatası (Attempt {Attempt}/{MaxRetries}): ErrorCode={ErrorCode}, Message={Message}, Server={Server}, Database={Database}", 
                    attempt, maxRetries, sqlEx.Number, sqlEx.Message, sqlEx.Server ?? "Unknown", _databaseName);
                if (attempt >= maxRetries)
                {
                    _logger?.LogError(sqlEx, 
                        "Payment kaydı {MaxRetries} denemeden sonra başarısız: PaymentId={PaymentId}, UserId={UserId}, Server={Server}, Database={Database}", 
                        maxRetries, payment.Id, payment.UserId, sqlEx.Server ?? "Unknown", _databaseName);
                    throw new InvalidOperationException(
                        $"Veritabanına bağlanılamadı. Lütfen veritabanının çalıştığından ve '{_databaseName}' veritabanının mevcut olduğundan emin olun. " +
                        $"SQL Error: {sqlEx.Message} (Error Code: {sqlEx.Number}, Server: {sqlEx.Server ?? "Unknown"}, Database: {_databaseName})", sqlEx);
                }
                var delay = TimeSpan.FromSeconds(Math.Pow(2, attempt - 1));
                _logger?.LogInformation("Retry öncesi bekleme: {Delay}ms", delay.TotalMilliseconds);
                await Task.Delay(delay);
                _databaseContext.Entry(payment).State = EntityState.Detached;
            }
            catch (DbUpdateException dbEx)
            {
                _logger?.LogError(dbEx, "Database update hatası: PaymentId={PaymentId}, InnerException={InnerException}", 
                    payment.Id, dbEx.InnerException?.Message);
                throw;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Beklenmeyen hata: PaymentId={PaymentId}, Error={Error}", 
                    payment.Id, ex.Message);
                throw;
            }
        }
    }
    public Task DeletePayment(Payment payment)
    {
        throw new NotImplementedException();
    }
    public async Task<List<Payment>> GetAllPaymentByUser(int userId)
    {
        List<Payment> payments = await _databaseContext.Set<Payment>().Where(p => p.UserId == userId).ToListAsync();
        return payments;
    }
    public Task<List<Payment>> GetAllPayments()
    {
        throw new NotImplementedException();
    }
    public Task<Payment> GetPaymentById(int id)
    {
        throw new NotImplementedException();
    }
    public Task<Payment> UpdatePayment(Payment payment)
    {
        throw new NotImplementedException();
    }
}
