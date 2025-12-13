# StrategyRuleService - MarketDataService Entegrasyonu

Bu doküman, StrategyRuleService'in MarketDataService ile nasıl entegre edildiğini açıklar.

## Genel Bakış

StrategyRuleService artık MarketDataService'den gerçek hisse senedi verilerini alabilir. Bu entegrasyon sayesinde stratejileriniz gerçek piyasa verilerine dayalı olarak çalışır.

## Entegrasyon Mimarisi

```
StrategyRuleService.Worker
    ↓ (HTTP Request)
MarketDataHttpClient
    ↓ (HTTP Request)
MarketDataService
    ↓ (API Call)
Finnhub API
```

## Yapılan Değişiklikler

### 1. MarketDataHttpClient
- MarketDataService'e HTTP istekleri yapan client
- Error handling ve timeout yönetimi
- JSON deserialization

### 2. MarketDataService Güncellemesi
- Gerçek veri öncelikli, simüle edilmiş veri fallback
- MarketDataService'den gelen veriyi internal format'a dönüştürme
- Detaylı logging

### 3. Worker Service Güncellemesi
- Simüle edilmiş veri yerine gerçek veri kullanımı
- MarketDataService entegrasyonu
- Veri format dönüşümü

### 4. Konfigürasyon
- MarketDataService URL konfigürasyonu
- Timeout ayarları
- Development ve Production ortamları için ayrı ayarlar

## Konfigürasyon

### appsettings.json
```json
{
  "MarketDataService": {
    "BaseUrl": "http://localhost:5001",
    "TimeoutSeconds": 30
  }
}
```

### appsettings.Development.json
```json
{
  "MarketDataService": {
    "BaseUrl": "http://localhost:5001",
    "TimeoutSeconds": 30
  }
}
```

## Çalıştırma

### 1. MarketDataService'i Başlatın
```bash
cd MarketDataService/MarketDataService
dotnet run
```

### 2. StrategyRuleService.Worker'ı Başlatın
```bash
cd StrategyRuleService/StrategyRuleService.Worker
dotnet run
```

## Veri Akışı

1. **Worker Service** her 30 saniyede bir aktif stratejileri kontrol eder
2. **MarketDataService**'den gerçek hisse senedi verisi alınır
3. Veri **StockMarketData** formatına dönüştürülür
4. **RuleEngine** kuralları değerlendirir
5. Koşullar sağlandığında işlem gerçekleştirilir

## Error Handling

- MarketDataService erişilemezse simüle edilmiş veri kullanılır
- HTTP timeout'ları yönetilir
- Detaylı error logging
- Graceful degradation

## Loglama

Entegrasyon detaylı loglar sağlar:
- MarketDataService'den veri alma durumu
- Veri dönüşüm işlemleri
- Error durumları
- Fallback mekanizması kullanımı

## Test

Entegrasyonu test etmek için:

1. MarketDataService'in çalıştığından emin olun
2. StrategyRuleService.Worker'ı başlatın
3. Logları kontrol edin - gerçek veri alındığını görmelisiniz
4. MarketDataService'i durdurun ve fallback mekanizmasının çalıştığını kontrol edin

## Sorun Giderme

### MarketDataService'e Bağlanamıyor
- MarketDataService'in çalıştığından emin olun
- URL konfigürasyonunu kontrol edin
- Port çakışması olup olmadığını kontrol edin

### Veri Alınamıyor
- Finnhub API key'inin doğru olduğundan emin olun
- Network bağlantısını kontrol edin
- Logları inceleyin

### Timeout Hataları
- TimeoutSeconds değerini artırın
- Network gecikmesini kontrol edin
