# Gerçekten Çalışacak Strateji Örneği

## ✅ Yapılan Değişiklikler

1. **DurationHours sınırlaması kaldırıldı** - Artık sınırsız süre çalışabilir
2. **Piyasa saatleri kontrolü kaldırıldı** - 7/24 çalışır
3. **TimeCheckRule güncellendi** - Her zaman Step 1'e geçer

## 📋 Örnek Strateji İsteği

### POST Request
```
POST http://localhost:5184/api/Strategy/createStrategy
Content-Type: application/json
```

### Request Body
```json
{
  "strategyName": "THYAO Alım Stratejisi",
  "description": "THYAO hissesi için otomatik alım stratejisi - Fiyat düşüşünde alım yapar",
  "userId": 1,
  "stockSymbol": "THYAO",
  "timeTracking": 60,
  "lot": 10,
  "transactionAmount": null,
  "accountId": 1,
  "portfolioId": 1,
  "durationHours": null,
  "stopLossPercentage": 3.0,
  "takeProfitPercentage": 5.0,
  "entryThresholdPercentage": -2.0,
  "maxLossLimitPercentage": 10.0,
  "totalPercentLoss": 10.0
}
```

## 📝 Parametre Açıklamaları

- **strategyName**: Strateji adı (3-100 karakter)
- **userId**: Kullanıcı ID'si (veritabanınızdan alın)
- **stockSymbol**: Hisse senedi sembolü (örn: THYAO, AKBNK, GARAN)
- **timeTracking**: Zaman takibi (dakika cinsinden, 1-1440)
- **lot**: İşlem yapılacak lot sayısı
- **accountId**: Hesap ID'si (opsiyonel)
- **portfolioId**: Portföy ID'si (opsiyonel)
- **durationHours**: `null` = sınırsız çalışır
- **stopLossPercentage**: Stop Loss yüzdesi (örn: 3.0 = %3)
- **takeProfitPercentage**: Take Profit yüzdesi (örn: 5.0 = %5)
- **entryThresholdPercentage**: Giriş eşiği (örn: -2.0 = açılışın %2 altına düşerse al)
- **maxLossLimitPercentage**: Maksimum toplam zarar limiti (örn: 10.0 = %10)

## 🎯 Bu Strateji Ne Yapar?

1. **Step 0 (TimeCheckRule)**: Piyasa saatleri kontrolü yapmaz, direkt geçer
2. **Step 1 (PortfolioCheckRule)**: Portföyde THYAO var mı kontrol eder
3. **Step 2 (SellRule)**: Eğer portföyde varsa:
   - Take Profit (%5) veya Stop Loss (%3) kontrolü yapar
   - Koşul sağlanırsa satış emri gönderir
4. **Step 3 (BuyRule)**: Eğer portföyde yoksa:
   - Açılış fiyatının %2 altına düşerse alım emri gönderir

## 🔍 Stratejiyi Test Etmek İçin

### 1. Strateji Oluştur
```bash
curl -X POST http://localhost:5184/api/Strategy/createStrategy \
  -H "Content-Type: application/json" \
  -d @STRATEGY_EXAMPLE_REQUEST.json
```

### 2. Strateji Durumunu Kontrol Et
```bash
GET http://localhost:5184/api/Strategy/verifyExecution/{strategyId}/{userId}
```

### 3. Event'leri Görüntüle
```bash
GET http://localhost:5184/api/Strategy/events/{strategyId}/{userId}?limit=20
```

## ⚠️ Önemli Notlar

- **userId**, **accountId**, **portfolioId** değerlerini kendi veritabanınızdan alın
- **stockSymbol** değerini gerçek bir BIST hisse senedi sembolü ile değiştirin
- Strateji oluşturulduktan sonra **StrategyProcessingHostedService** her 5 saniyede bir kontrol eder
- Strateji sürekli çalışır, koşullar sağlandığında otomatik işlem yapar

## 🚀 Hızlı Test İçin

Eğer hemen test etmek istiyorsanız, `entryThresholdPercentage` değerini **0** yapın. Bu durumda mevcut fiyat açılış fiyatına eşit veya altındaysa hemen alım yapar.

```json
{
  "entryThresholdPercentage": 0.0
}
```

