# Strategy Rule Service

Bu proje, hisse senedi alım-satım stratejilerini ve kurallarını yöneten basit bir .NET 9.0 uygulamasıdır. Worker service ile sürekli çalışan stratejiler ve kurallar sistemi içerir.

## Proje Yapısı

### Katmanlar
- **Api**: REST API endpoints
- **Application**: İş mantığı ve CQRS pattern
- **Domain**: Entity'ler ve enum'lar
- **Core**: Temel entity'ler
- **Infrastructure**: Dış servis entegrasyonları
- **Persistence**: Veritabanı işlemleri
- **StrategyRuleService.Worker**: Sürekli çalışan worker service

### Ana Özellikler

#### 1. Strateji Yönetimi
- Strateji oluşturma ve yönetimi
- Otomatik kural oluşturma
- Strateji durumu takibi

#### 2. Kural Sistemi
- Dinamik kural oluşturma
- Kural değerlendirme motoru
- Sıralı kural çalıştırma

#### 3. Worker Service
- Sürekli çalışan strateji değerlendirme
- Piyasa verisi simülasyonu
- Otomatik işlem gerçekleştirme

## Kurulum ve Çalıştırma

### Gereksinimler
- .NET 9.0 SDK
- Visual Studio 2022 veya VS Code

### Çalıştırma

1. **API Projesini Çalıştırma:**
```bash
cd Api
dotnet run
```

2. **Worker Service'i Çalıştırma:**
```bash
cd StrategyRuleService.Worker
dotnet run
```

## API Endpoints

### Strateji İşlemleri

#### Strateji Oluşturma
```http
POST /api/strategy
Content-Type: application/json

{
  "strategyName": "THYAD Stratejisi",
  "description": "THYAD için alım-satım stratejisi",
  "userId": 1,
  "stockSymbol": "THYAD",
  "transactionAmount": 10000,
  "buyThresholdPercent": -5.0,
  "profitTargetPercent": 5.0,
  "stopLossPercent": 2.0
}
```

#### Tüm Stratejileri Listeleme
```http
GET /api/strategy
```

#### Strateji Detayı
```http
GET /api/strategy/{id}
```

### Kural İşlemleri

#### Kural Oluşturma
```http
POST /api/rule
Content-Type: application/json

{
  "strategyId": 1,
  "ruleName": "Ek Alış Kuralı",
  "field": "CurrentPrice",
  "operator": "LessThan",
  "action": "BUY",
  "orderIndex": 4,
  "description": "Fiyat 90'dan düşükse ek al",
  "compareValue": 90.0
}
```

## Strateji ve Kural Sistemi

### Strateji Yaşam Döngüsü
1. **Oluşturma**: Strateji ve kuralları oluşturulur
2. **Aktif**: Worker service stratejiyi sürekli izler
3. **Değerlendirme**: Kurallar piyasa verilerine göre değerlendirilir
4. **İşlem**: Koşullar sağlandığında alım/satım işlemi gerçekleştirilir
5. **Tamamlanma**: Strateji hedefine ulaştığında tamamlanır

### Kural Tipleri
- **FieldType**: Hangi alan kontrol edilecek (fiyat, hacim, vb.)
- **OperatorType**: Karşılaştırma operatörü (>, <, =, vb.)
- **ActionType**: Aksiyon tipi (BUY, SELL, WAIT, CLOSE)

### Default Kurallar
Her strateji oluşturulduğunda otomatik olarak 3 kural oluşturulur:
1. **Alış Kuralı**: Fiyat belirlenen yüzde düştüğünde al
2. **Kar Hedefi**: Belirlenen kar yüzdesine ulaşıldığında sat
3. **Stop Loss**: Belirlenen zarar yüzdesine ulaşıldığında sat

## Worker Service Özellikleri

### Sürekli İzleme
- Her 30 saniyede bir stratejileri kontrol eder
- Piyasa verilerini simüle eder
- Kuralları sıralı şekilde değerlendirir

### Loglama
- Detaylı işlem logları
- Hata durumlarında bilgilendirme
- Strateji durumu takibi

### Güvenlik
- Semaphore ile thread-safe işlemler
- Exception handling
- Graceful shutdown

## Test

### HTTP Dosyası
`Api/test-api.http` dosyasında tüm endpoint'ler için test örnekleri bulunur.

### Test Endpoint'leri
```http
GET /api/strategy/test
GET /api/rule/test
```

## Geliştirme

### Yeni Kural Ekleme
1. `FieldType` enum'una yeni alan ekle
2. `RuleEngine.GetFieldValue()` metodunu güncelle
3. Gerekirse yeni `ActionType` ekle

### Yeni Strateji Tipi
1. `ISimpleStrategyService` interface'ini genişlet
2. Yeni strateji servisi oluştur
3. Worker service'te entegre et

## Teknolojiler
- .NET 9.0
- MediatR (CQRS)
- Background Service
- REST API
- Dependency Injection
- Logging

## Lisans
Bu proje eğitim amaçlı geliştirilmiştir.
