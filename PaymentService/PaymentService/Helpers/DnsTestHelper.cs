using System.Net;
using Microsoft.Extensions.Logging;
namespace PaymentService.Helpers;
public static class DnsTestHelper
{
    public static async Task<bool> TestDnsResolutionAsync(string hostname, ILogger? logger = null)
    {
        try
        {
            logger?.LogInformation("DNS çözümleme testi başlatılıyor: Hostname={Hostname}", hostname);
            var addresses = await Dns.GetHostAddressesAsync(hostname);
            if (addresses == null || addresses.Length == 0)
            {
                logger?.LogWarning("DNS çözümlemesi başarısız: Hostname={Hostname}, IP adresi bulunamadı", hostname);
                return false;
            }
            logger?.LogInformation("DNS çözümlemesi başarılı: Hostname={Hostname}, IP Adresleri={IpAddresses}", 
                hostname, string.Join(", ", addresses.Select(a => a.ToString())));
            return true;
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "DNS çözümleme testi başarısız: Hostname={Hostname}, Hata={ErrorMessage}", 
                hostname, ex.Message);
            return false;
        }
    }
    public static async Task<(bool Success, List<string> IpAddresses, string ErrorMessage)> TestDnsResolutionDetailedAsync(
        string hostname, ILogger? logger = null)
    {
        try
        {
            logger?.LogInformation("DNS çözümleme testi (detaylı) başlatılıyor: Hostname={Hostname}", hostname);
            var addresses = await Dns.GetHostAddressesAsync(hostname);
            if (addresses == null || addresses.Length == 0)
            {
                var errorMessage = $"DNS çözümlemesi başarısız: {hostname} için IP adresi bulunamadı";
                logger?.LogWarning(errorMessage);
                return (false, new List<string>(), errorMessage);
            }
            var ipAddresses = addresses.Select(a => a.ToString()).ToList();
            var successMessage = $"DNS çözümlemesi başarılı: {hostname} -> {string.Join(", ", ipAddresses)}";
            logger?.LogInformation(successMessage);
            return (true, ipAddresses, string.Empty);
        }
        catch (System.Net.Sockets.SocketException socketEx)
        {
            var errorMessage = $"DNS çözümleme hatası (SocketException): {socketEx.SocketErrorCode} - {socketEx.Message}";
            logger?.LogError(socketEx, errorMessage);
            return (false, new List<string>(), errorMessage);
        }
        catch (Exception ex)
        {
            var errorMessage = $"DNS çözümleme hatası: {ex.GetType().Name} - {ex.Message}";
            logger?.LogError(ex, errorMessage);
            return (false, new List<string>(), errorMessage);
        }
    }
}
