# Iyzico API DNS/Network Sorun Giderme Rehberi

## Hata: SocketException (11001): HostNotFound

Bu hata, `sandbox-api.iyzipay.com` hostname'inin DNS tarafından çözümlenemediğini gösterir.

---

## 1. Bağlantı Teşhisi - PowerShell/CMD Komutları

### A. DNS Çözümleme Testi (nslookup)

```powershell
# PowerShell veya CMD'de çalıştırın
nslookup sandbox-api.iyzipay.com
```

**Beklenen Sonuç (Başarılı):**
```
Server:  UnKnown
Address:  192.168.1.1

Non-authoritative answer:
Name:    sandbox-api.iyzipay.com
Addresses:  104.19.158.xxx
          104.19.159.xxx
```

**Hata Durumu:**
```
Server:  UnKnown
Address:  192.168.1.1

*** UnKnown can't find sandbox-api.iyzipay.com: Non-existent domain
```

### B. Ping Testi

```powershell
# Ping testi (4 paket gönderir)
ping sandbox-api.iyzipay.com

# Tek paket ping (hızlı test)
ping -n 1 sandbox-api.iyzipay.com

# IPv6'yı devre dışı bırakarak ping (IPv4 zorlamak için)
ping -n 1 -4 sandbox-api.iyzipay.com
```

**Beklenen Sonuç (Başarılı):**
```
Pinging sandbox-api.iyzipay.com [104.19.158.xxx] with 32 bytes of data:
Reply from 104.19.158.xxx: bytes=32 time=45ms TTL=57
```

**Hata Durumu:**
```
Ping request could not find host sandbox-api.iyzipay.com. Please check the name and try again.
```

### C. Traceroute (Yol İzleme)

```powershell
# Windows'ta tracert komutu
tracert sandbox-api.iyzipay.com

# Timeout süresini azaltarak (daha hızlı)
tracert -d -h 15 sandbox-api.iyzipay.com
```

**Beklenen Sonuç:**
- İlk birkaç hop local network/router'ınızı göstermeli
- Sonraki hop'lar internet üzerinden Iyzico sunucusuna giden yolu göstermeli

**Hata Durumu:**
```
Unable to resolve target system name sandbox-api.iyzipay.com
```

### D. DNS Sunucu Testi

```powershell
# Google DNS (8.8.8.8) ile test
nslookup sandbox-api.iyzipay.com 8.8.8.8

# Cloudflare DNS (1.1.1.1) ile test
nslookup sandbox-api.iyzipay.com 1.1.1.1

# Mevcut DNS sunucunuzu görmek için
ipconfig /all | findstr "DNS Servers"
```

### E. Host Dosyası Kontrolü

```powershell
# Host dosyasında Iyzico için manuel entry var mı kontrol et
notepad C:\Windows\System32\drivers\etc\hosts

# PowerShell ile kontrol
Get-Content C:\Windows\System32\drivers\etc\hosts | Select-String "iyzipay"
```

**Not:** Host dosyasında `sandbox-api.iyzipay.com` için yanlış veya eski bir IP adresi varsa, bu DNS çözümlemesini bozabilir.

### F. PowerShell ile DNS Çözümleme Testi

```powershell
# PowerShell DNS çözümleme testi
[System.Net.Dns]::GetHostAddresses("sandbox-api.iyzipay.com")

# Detaylı bilgi için
$result = Resolve-DnsName -Name sandbox-api.iyzipay.com -Type A
$result | Format-List
```

---

## 2. DNS Çözümleri

### A. DNS Sunucusu Değiştirme

**Windows'ta DNS Ayarları:**

1. **GUI ile:**
   - Settings > Network & Internet > Network and Sharing Center
   - Active network connection'a tıklayın (Wi-Fi veya Ethernet)
   - Properties > Internet Protocol Version 4 (TCP/IPv4) > Properties
   - "Use the following DNS server addresses" seçin
   - Preferred DNS: `8.8.8.8` (Google DNS)
   - Alternate DNS: `8.8.4.4` (Google DNS)
   - OK > Close

2. **PowerShell ile (Admin olarak):**
```powershell
# Wi-Fi için DNS değiştirme
Set-DnsClientServerAddress -InterfaceAlias "Wi-Fi" -ServerAddresses ("8.8.8.8","8.8.4.4")

# Ethernet için DNS değiştirme
Set-DnsClientServerAddress -InterfaceAlias "Ethernet" -ServerAddresses ("8.8.8.8","8.8.4.4")

# Mevcut DNS ayarlarını görmek için
Get-DnsClientServerAddress
```

**Alternatif DNS Sunucuları:**
- Google DNS: `8.8.8.8`, `8.8.4.4`
- Cloudflare DNS: `1.1.1.1`, `1.0.0.1`
- OpenDNS: `208.67.222.222`, `208.67.220.220`

### B. DNS Cache Temizleme

```powershell
# DNS cache'i temizle (Admin olarak)
ipconfig /flushdns

# PowerShell ile
Clear-DnsClientCache

# Sonra test et
nslookup sandbox-api.iyzipay.com
```

### C. Windows DNS Client Service'i Yeniden Başlatma

```powershell
# Admin PowerShell'de
Restart-Service -Name Dnscache -Force

# Veya Services.msc üzerinden
# "DNS Client" servisini bulup yeniden başlatın
```

---

## 3. VPN/Proxy Sorunları

### A. VPN Kontrolü

```powershell
# Aktif VPN bağlantıları
Get-VpnConnection

# VPN'i geçici olarak kapatıp test edin
```

**VPN Kullanıyorsanız:**
- VPN'in DNS ayarlarını kontrol edin
- VPN'i kapatıp Iyzico API'sine bağlanmayı test edin
- VPN'in Iyzico API'sine erişimi engellemediğinden emin olun

### B. Proxy Kontrolü

```powershell
# Sistem proxy ayarlarını kontrol et
netsh winhttp show proxy

# Proxy ayarlarını görmek için
[System.Net.WebRequest]::GetSystemWebProxy().GetProxy("https://sandbox-api.iyzipay.com")
```

**Proxy Kullanıyorsanız:**
- Iyzico SDK kendi HttpClient'ını kullandığı için, sistem proxy ayarları otomatik kullanılır
- Proxy'nin `sandbox-api.iyzipay.com` için erişime izin verdiğinden emin olun
- Kurumsal proxy kullanıyorsanız, IT departmanından `sandbox-api.iyzipay.com` için beyaz liste isteyin

### C. HttpClient Proxy Yapılandırması (AccountService için)

AccountService HttpClient'ı zaten proxy desteği ile yapılandırılmış:
```csharp
.ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler()
{
    UseProxy = true,
    Proxy = System.Net.WebRequest.GetSystemWebProxy(),
});
```

**Iyzico SDK için:**
- Iyzico SDK kendi HttpClient'ını kullanır
- Sistem genelinde proxy ayarları otomatik kullanılır
- Eğer proxy bypass gerekiyorsa, sistem proxy ayarlarından exception ekleyin

---

## 4. Firewall/Antivirus Kontrolü

### A. Windows Defender Firewall

```powershell
# Firewall kurallarını kontrol et
Get-NetFirewallRule | Where-Object {$_.DisplayName -like "*PaymentService*"}

# İç giden bağlantıları kontrol et (DNS için)
netsh advfirewall firewall show rule name=all | findstr "DNS"
```

**Çözüm:**
- PaymentService.exe için firewall exception ekleyin
- Geliştirme sırasında firewall'u geçici olarak kapatıp test edin

### B. Antivirus Kontrolü

- Antivirus yazılımınızın network trafiğini engellemediğinden emin olun
- PaymentService.exe için exception ekleyin
- Geliştirme sırasında antivirus'u geçici olarak kapatıp test edin

---

## 5. Geçici Çözümler

### A. Host Dosyası ile Manuel DNS Mapping (Geçici Test İçin)

**NOT:** Bu sadece test için, production'da kullanmayın!

```powershell
# Admin olarak notepad aç
notepad C:\Windows\System32\drivers\etc\hosts

# Host dosyasına şu satırı ekle (IP adresi güncel olmalı - nslookup ile bulun)
# 104.19.158.xxx    sandbox-api.iyzipay.com
```

**IP Adresini Bulmak İçin:**
```powershell
# İnternet bağlantısı olan başka bir makineden veya online DNS lookup servisinden
nslookup sandbox-api.iyzipay.com 8.8.8.8
```

### B. Mock Mode Kullanımı (Geliştirme İçin)

`appsettings.Development.json` dosyasında:
```json
{
  "Iyzico": {
    "EnableMockMode": true
  }
}
```

Bu modda gerçek Iyzico API çağrıları yapılmaz, mock response döndürülür.

---

## 6. Kod Kontrolü - BaseUrl Validation

Kod zaten BaseUrl validation içeriyor:
- Trim() ile başında/sonunda boşluk temizleniyor
- Gizli karakterler kontrol ediliyor (zero-width space, BOM)
- Hostname formatı doğrulanıyor

**Manuel Kontrol:**
```csharp
// appsettings.json içinde
"Iyzico": {
  "BaseUrl": "https://sandbox-api.iyzipay.com"  // Boşluk olmamalı
}
```

**PowerShell ile appsettings.json'ı kontrol et:**
```powershell
# JSON dosyasını oku ve BaseUrl'i kontrol et
$config = Get-Content PaymentService/PaymentService/appsettings.json | ConvertFrom-Json
$baseUrl = $config.Iyzico.BaseUrl
Write-Host "BaseUrl: '$baseUrl'"
Write-Host "Length: $($baseUrl.Length)"
Write-Host "Bytes: $([System.Text.Encoding]::UTF8.GetBytes($baseUrl).Length)"

# Gizli karakterleri görmek için
$baseUrl.ToCharArray() | ForEach-Object { Write-Host "$([int][char]$_) - $_" }
```

---

## 7. Network Bağlantı Testi

### A. HTTPS Bağlantı Testi

```powershell
# PowerShell ile HTTPS bağlantı testi
$uri = "https://sandbox-api.iyzipay.com"
try {
    $response = Invoke-WebRequest -Uri $uri -Method Head -TimeoutSec 10
    Write-Host "Bağlantı başarılı: $($response.StatusCode)"
} catch {
    Write-Host "Bağlantı hatası: $($_.Exception.Message)"
}
```

### B. Port Kontrolü

```powershell
# Port 443 (HTTPS) açık mı kontrol et
Test-NetConnection -ComputerName sandbox-api.iyzipay.com -Port 443
```

---

## 8. Çözüm Özeti

1. **DNS Sunucusu Değiştir:** Google DNS (8.8.8.8) veya Cloudflare DNS (1.1.1.1) kullan
2. **DNS Cache Temizle:** `ipconfig /flushdns`
3. **VPN'i Kapat:** Test için VPN'i geçici olarak kapat
4. **Firewall/Antivirus Kontrolü:** Exception ekle veya geçici olarak kapat
5. **Mock Mode Kullan:** Geliştirme için `EnableMockMode: true` yap
6. **Network Bağlantısı Test Et:** `ping` ve `nslookup` komutları ile test et

---

## 9. Destek İçin Toplanacak Bilgiler

Sorun devam ederse, aşağıdaki bilgileri toplayın:

```powershell
# Sistem bilgileri
ipconfig /all | Out-File network-config.txt
nslookup sandbox-api.iyzipay.com | Out-File dns-test.txt
ping -n 4 sandbox-api.iyzipay.com | Out-File ping-test.txt
tracert sandbox-api.iyzipay.com | Out-File tracert-test.txt

# PowerShell DNS testi
[System.Net.Dns]::GetHostAddresses("sandbox-api.iyzipay.com") | Out-File powershell-dns.txt
```

Bu dosyaları destek ekibine gönderin.

