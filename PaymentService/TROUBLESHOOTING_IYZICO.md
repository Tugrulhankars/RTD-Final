# Iyzico API Bağlantı Sorunları - Sorun Giderme Rehberi

## DNS/Network Hatası (SocketException 11001: HostNotFound)

### 1. BaseUrl Yapılandırması Kontrolü

`appsettings.json` dosyasında Iyzico BaseUrl'inin doğru olduğundan emin olun:

```json
{
  "Iyzico": {
    "ApiKey": "sandbox-...",
    "SecretKey": "sandbox-...",
    "BaseUrl": "https://sandbox-api.iyzipay.com",
    "CallbackUrl": "https://localhost:7009/api/payment/iyzico/callback"
  }
}
```

**Önemli:**
- BaseUrl'de başında/sonunda boşluk olmamalı
- Gizli karakterler (zero-width space, BOM vb.) olmamalı
- URL doğru format: `https://sandbox-api.iyzipay.com` (http:// değil https://)

### 2. DNS Testi

Windows'ta DNS çözümlemesini test etmek için:

```cmd
# DNS çözümleme testi
nslookup sandbox-api.iyzipay.com

# Ping testi (DNS çözümlenebiliyor mu kontrol et)
ping sandbox-api.iyzipay.com

# IP adresini görmek için
ping -n 1 sandbox-api.iyzipay.com
```

**Beklenen Sonuç:**
- DNS çözümlemesi başarılı olmalı
- IP adresi dönmeli (örn: `104.19.159.xxx` veya benzeri)

**Hata Durumunda:**
- DNS sunucunuzu değiştirin (Google DNS: 8.8.8.8, 8.8.4.4)
- VPN kullanıyorsanız kapatın ve tekrar deneyin
- Firewall/antivirus yazılımını kontrol edin

### 3. İnternet Bağlantısı Testi

Tarayıcıdan manuel olarak test edin:
```
https://sandbox-api.iyzipay.com
```

Eğer tarayıcıdan erişilemiyorsa:
- İnternet bağlantınızı kontrol edin
- Proxy ayarlarınızı kontrol edin
- Firewall kurallarını kontrol edin

### 4. Proxy Yapılandırması (Gerekirse)

Eğer kurumsal ağda proxy kullanıyorsanız, `Program.cs`'de HttpClient yapılandırmasına proxy eklenebilir:

```csharp
// Program.cs içinde (Iyzico için özel HttpClient - şu anda SDK kullanılıyor)
// Not: Iyzico SDK kendi HttpClient'ını kullanıyor, bu yüzden proxy ayarı SDK seviyesinde yapılamaz
// Proxy gerekiyorsa, sistem genelinde proxy ayarlarını yapılandırın:

// Windows'ta sistem proxy ayarları:
// Settings > Network & Internet > Proxy
```

**Alternatif:** Eğer Iyzico SDK'sı proxy desteklemiyorsa, sistem genelinde proxy ayarlarını yapılandırın veya Iyzico desteğiyle iletişime geçin.

### 5. Firewall/Antivirus Kontrolü

- Windows Defender Firewall'dan `PaymentService.exe`'ye izin verin
- Antivirus yazılımınızın ağ trafiğini engellemediğinden emin olun
- Kurumsal güvenlik duvarı varsa, IT departmanıyla iletişime geçin

### 6. Geliştirme Ortamı Özel Durumları

**VPN Kullanımı:**
- VPN aktifken DNS çözümlemesi başarısız olabilir
- VPN'i kapatıp test edin

**Kurumsal Ağ:**
- Kurumsal proxy/firewall Iyzico API'sine erişimi engelliyor olabilir
- IT departmanından `sandbox-api.iyzipay.com` için beyaz liste isteyin

### 7. Hata Mesajları

Kod şu hata mesajlarını döndürür:

**DNS Hatası:**
```
Iyzico API sunucusu bulunamadı (DNS hatası). Lütfen internet bağlantınızı kontrol edin ve DNS ayarlarınızı doğrulayın.
```

**Ağ Hatası:**
```
Iyzico API'sine bağlanılamıyor (Ağ hatası: HostNotFound). Lütfen internet bağlantınızı ve firewall ayarlarınızı kontrol edin.
```

### 8. Log Kontrolü

Uygulama loglarında şu bilgileri kontrol edin:
- BaseUrl değeri ve uzunluğu
- SocketErrorCode ve ErrorCode
- DNS çözümleme detayları

Log örneği:
```
Iyzico BaseUrl detayları: BaseUrl='https://sandbox-api.iyzipay.com', Length=28, BytesLength=28, StartsWithHttps=True, EndsWithCom=True
SocketException detayları: SocketErrorCode=HostNotFound, ErrorCode=11001, NativeErrorCode=11001
```

### 9. Geçici Çözüm (Test İçin)

Eğer Iyzico API'sine erişemiyorsanız:
1. Mock/test mode ekleyebilirsiniz
2. Alternatif ödeme gateway'i kullanabilirsiniz
3. VPN/proxy kullanarak erişimi test edebilirsiniz

## İletişim

Sorun devam ederse:
1. Log dosyalarını toplayın
2. DNS test sonuçlarını kaydedin
3. Iyzico destek ekibiyle iletişime geçin
4. IT departmanınızla görüşün (kurumsal ağda çalışıyorsanız)

