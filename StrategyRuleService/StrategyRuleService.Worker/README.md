# Strategy Rule Service Worker

Bu worker service, finansal strateji kurallarını sürekli olarak çalıştıran ve otomatik alım-satım işlemleri gerçekleştiren bir background service'dir.

## Özellikler

- **Sürekli Çalışma**: Stratejiler 7/24 sürekli olarak çalışır
- **Gerçek Zamanlı Veri**: Piyasa verilerini sürekli günceller
- **Kural Değerlendirme**: Strateji kurallarını sırayla değerlendirir
- **Otomatik İşlem**: Koşullar sağlandığında otomatik alım-satım yapar
- **Risk Yönetimi**: Stop-loss ve take-profit seviyelerini kontrol eder
- **Hata Yönetimi**: Hata durumlarında güvenli şekilde devam eder

## Çalışma Mantığı

### 1. Strateji Yükleme
- Aktif stratejileri veritabanından yükler
- Her strateji için çalıştırma durumu oluşturur

### 2. Piyasa Verisi Güncelleme
- Her strateji için güncel piyasa verilerini alır
- Fiyat, hacim, zaman bilgilerini günceller

### 3. Kural Değerlendirme
- Strateji kurallarını `OrderIndex` sırasına göre değerlendirir
- Her kural için koşul kontrolü yapar
- Koşul sağlandığında aksiyon belirler

### 4. Aksiyon Gerçekleştirme
- **BUY**: Alış emri gönderir
- **SELL**: Satış emri gönderir
- **WAIT**: Bekler
- **CLOSE**: Stratejiyi kapatır
- **ALERT**: Uyarı gönderir

## Konfigürasyon

### appsettings.json

```json
{
  "StrategyService": {
    "ProcessingIntervalSeconds": 1,
    "MaxConcurrentStrategies": 100,
    "DefaultTransactionAmount": 10000,
    "DefaultTransactionPercentage": 30,
    "DefaultStopLossPercentage": 4,
    "DefaultTakeProfitPercentage": 4
  },
  "MarketData": {
    "ApiUrl": "https://api.example.com/market-data",
    "ApiKey": "your-api-key-here"
  },
  "Broker": {
    "ApiUrl": "https://broker-api.example.com",
    "IsSimulationMode": true
  }
}
```

## Çalıştırma

### Development
```bash
cd StrategyRuleService.Worker
dotnet run
```

### Production
```bash
dotnet publish -c Release
dotnet StrategyRuleService.Worker.dll
```

### Windows Service Olarak
```bash
sc create "StrategyRuleService" binPath="C:\path\to\StrategyRuleService.Worker.exe"
sc start "StrategyRuleService"
```

## Loglar

Worker service aşağıdaki log seviyelerini kullanır:

- **Information**: Genel bilgiler, işlem başarıları
- **Warning**: Uyarılar, beklenmeyen durumlar
- **Error**: Hatalar, işlem başarısızlıkları
- **Debug**: Detaylı debug bilgileri

### Log Örnekleri

```
[INFO] Strategy Rule Service Worker başlatıldı
[INFO] Strateji 1 için yeni çalıştırma başlatıldı
[INFO] Strateji 1 için alış işlemi gerçekleştirildi: Miktar=3000, Fiyat=150.50, Adet=19.93
[INFO] Strateji 1 için satış işlemi gerçekleştirildi: Kar/Zarar=150.25, Fiyat=158.25
[WARN] Strateji 1 için uyarı: StopLoss tetiklendi - Fiyat: 144.50
```

## Güvenlik

- **API Anahtarları**: Güvenli şekilde saklanır
- **Simülasyon Modu**: Test için güvenli mod
- **Hata Yönetimi**: Güvenli hata durumu yönetimi
- **Log Güvenliği**: Hassas bilgiler loglanmaz

## Performans

- **Eşzamanlı İşlem**: Birden fazla stratejiyi aynı anda çalıştırır
- **Bellek Optimizasyonu**: Verimli bellek kullanımı
- **CPU Optimizasyonu**: Düşük CPU kullanımı
- **Ağ Optimizasyonu**: Verimli API çağrıları

## Monitoring

### Health Check
```bash
curl http://localhost:5000/health
```

### Metrics
- Aktif strateji sayısı
- İşlem sayısı
- Başarı oranı
- Ortalama işlem süresi

## Troubleshooting

### Yaygın Sorunlar

1. **Piyasa Verisi Alınamıyor**
   - API anahtarını kontrol edin
   - Ağ bağlantısını kontrol edin
   - API limitlerini kontrol edin

2. **Strateji Çalışmıyor**
   - Strateji durumunu kontrol edin
   - Kuralları kontrol edin
   - Logları inceleyin

3. **İşlem Başarısız**
   - Broker API'sini kontrol edin
   - Hesap bakiyesini kontrol edin
   - Piyasa saatlerini kontrol edin

### Debug Modu

```bash
set ASPNETCORE_ENVIRONMENT=Development
dotnet run --verbosity detailed
```

## Gelecek Geliştirmeler

1. **WebSocket Desteği**: Gerçek zamanlı veri
2. **Machine Learning**: AI destekli stratejiler
3. **Backtesting**: Geçmiş verilerle test
4. **Portfolio Yönetimi**: Çoklu hisse desteği
5. **Mobile App**: Mobil uygulama desteği
