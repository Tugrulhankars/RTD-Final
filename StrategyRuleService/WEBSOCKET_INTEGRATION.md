# WebSocket Entegrasyonu - StrategyRuleService

Bu doküman, StrategyRuleService'in MarketDataService ile WebSocket üzerinden nasıl entegre edildiğini açıklar.

## WebSocket vs HTTP Karşılaştırması

### HTTP Yaklaşımı (Önceki)
- ❌ Her veri için ayrı HTTP isteği
- ❌ Yüksek latency (request/response döngüsü)
- ❌ Daha fazla network overhead
- ❌ Polling gereksinimi

### WebSocket Yaklaşımı (Yeni)
- ✅ Sürekli bağlantı
- ✅ Düşük latency (gerçek zamanlı)
- ✅ Daha az network overhead
- ✅ Push-based veri akışı
- ✅ Otomatik reconnection

## WebSocket Entegrasyon Mimarisi

```
StrategyRuleService.Worker
    ↓ (WebSocket Connection)
MarketDataWebSocketClient
    ↓ (WebSocket Connection)
MarketDataService (WebSocket Endpoint)
    ↓ (WebSocket Connection)
Finnhub API (Real-time Stream)
```

## Yeni Bileşenler

### 1. MarketDataWebSocketClient
- WebSocket bağlantı yönetimi
- Otomatik reconnection
- Veri cache'leme
- Error handling

### 2. Güncellenmiş MarketDataService
- WebSocket verilerini öncelikli kullanma
- HTTP fallback mekanizması
- Veri cache'leme
- Abonelik yönetimi

## Veri Akışı

1. **İlk Bağlantı**: StrategyRuleService, MarketDataService'e WebSocket bağlantısı kurar
2. **Abonelik**: Belirli ticker'lar için abonelik başlatılır
3. **Veri Akışı**: MarketDataService, Finnhub'dan gelen verileri WebSocket üzerinden iletir
4. **Cache**: Gelen veriler cache'lenir ve anında kullanılabilir
5. **Fallback**: WebSocket bağlantısı kesilirse HTTP'ye geçer

## Konfigürasyon

### appsettings.json
```json
{
  "MarketDataService": {
    "BaseUrl": "http://localhost:5001",
    "WebSocketBaseUrl": "ws://localhost:5001",
    "TimeoutSeconds": 30,
    "ConnectionTimeoutSeconds": 30,
    "ReconnectDelaySeconds": 5
  }
}
```

## Avantajlar

### 1. Gerçek Zamanlı Veri
- WebSocket bağlantısı sayesinde anlık veri akışı
- HTTP polling'e gerek yok
- Daha hızlı strateji tepkisi

### 2. Daha Az Network Overhead
- Tek bağlantı ile sürekli veri akışı
- HTTP header'larına gerek yok
- Daha verimli bandwidth kullanımı

### 3. Otomatik Reconnection
- Bağlantı kesilirse otomatik yeniden bağlanma
- Graceful degradation
- Sistem sürekliliği

### 4. Cache Mekanizması
- Gelen veriler cache'lenir
- Anında erişim
- Fallback mekanizması

## Error Handling

### 1. WebSocket Bağlantı Hataları
- Otomatik reconnection
- Exponential backoff
- Fallback to HTTP

### 2. Veri Parse Hataları
- JSON parse hatalarını handle etme
- Logging ve monitoring
- Graceful degradation

### 3. Network Hataları
- Timeout yönetimi
- Connection retry
- Service availability check

## Monitoring ve Logging

### Log Seviyeleri
- **Information**: Bağlantı durumu, abonelik başlatma
- **Debug**: Veri akışı, cache işlemleri
- **Warning**: Fallback kullanımı, bağlantı sorunları
- **Error**: Kritik hatalar, bağlantı kesintileri

### Metrikler
- WebSocket bağlantı sayısı
- Veri akış hızı
- Cache hit/miss oranları
- Reconnection sayıları

## Test Senaryoları

### 1. Normal Çalışma
1. MarketDataService'i başlatın
2. StrategyRuleService.Worker'ı başlatın
3. WebSocket bağlantısının kurulduğunu kontrol edin
4. Veri akışının çalıştığını doğrulayın

### 2. Bağlantı Kesintisi
1. MarketDataService'i durdurun
2. Fallback mekanizmasının çalıştığını kontrol edin
3. MarketDataService'i yeniden başlatın
4. Otomatik reconnection'ı doğrulayın

### 3. Veri Akışı
1. WebSocket üzerinden veri akışını izleyin
2. Cache'lenen verilerin güncel olduğunu kontrol edin
3. Strateji kurallarının gerçek veriyle çalıştığını doğrulayın

## Performans Optimizasyonları

### 1. Connection Pooling
- Tek WebSocket bağlantısı ile multiple ticker
- Resource sharing
- Memory optimization

### 2. Data Caching
- In-memory cache
- TTL (Time To Live) yönetimi
- Cache invalidation

### 3. Async Processing
- Non-blocking operations
- Background tasks
- Concurrent processing

## Sorun Giderme

### WebSocket Bağlantısı Kurulamıyor
1. MarketDataService'in çalıştığını kontrol edin
2. Port çakışması olup olmadığını kontrol edin
3. Firewall ayarlarını kontrol edin
4. URL konfigürasyonunu doğrulayın

### Veri Akışı Yok
1. Finnhub API key'inin doğru olduğunu kontrol edin
2. WebSocket endpoint'inin çalıştığını doğrulayın
3. Network bağlantısını kontrol edin
4. Logları inceleyin

### Yüksek Memory Kullanımı
1. Cache boyutunu kontrol edin
2. Connection sayısını sınırlayın
3. Garbage collection'ı optimize edin
4. Memory leak'leri araştırın

## Gelecek Geliştirmeler

### 1. Load Balancing
- Multiple MarketDataService instance'ları
- Connection distribution
- Failover mekanizması

### 2. Message Queuing
- Redis/RabbitMQ entegrasyonu
- Message persistence
- Guaranteed delivery

### 3. Advanced Caching
- Distributed cache
- Cache warming
- Intelligent invalidation

### 4. Monitoring Dashboard
- Real-time metrics
- Connection status
- Performance analytics
