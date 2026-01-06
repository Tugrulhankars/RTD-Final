# Strateji Doğrulama Rehberi

## 🎯 Stratejinin Gerçekten Çalışıp Çalışmadığını Nasıl Anlarsınız?

Bu rehber, oluşturduğunuz bir stratejinin gerçekten çalışıp çalışmadığını doğrulamak için adım adım kontrol listesi sağlar.

---

## ✅ Hızlı Kontrol Listesi

### 1. **Strateji Oluşturuldu mu?**
```bash
GET /api/Strategy/getStrategyDetail/{strategyId}/{userId}
```
- Strateji bilgilerini kontrol edin
- `Status` = `Active` olmalı
- `IsActive` = `true` olmalı

### 2. **Worker Service Çalışıyor mu?**
- StrategyRuleService.Worker çalışıyor olmalı
- Veya API projesinde `StrategyProcessingHostedService` çalışıyor olmalı
- Loglarda `"StrategyProcessingHostedService started"` mesajını arayın

### 3. **Event'ler Kaydediliyor mu?**
```bash
GET /api/Strategy/events/{strategyId}/{userId}
```
- Event listesi boş olmamalı
- Son event son 5 dakika içinde olmalı

### 4. **Step İlerlemesi Var mı?**
```bash
GET /api/Strategy/verifyExecution/{strategyId}/{userId}
```
- Step 0 → Step 1 → Step 2/3 → Step -1 ilerlemesi olmalı

---

## 🔍 Detaylı Doğrulama

### Adım 1: Strateji Detayını Kontrol Et

```http
GET http://localhost:5184/api/Strategy/getStrategyDetail/1/1
```

**Beklenen Response:**
```json
{
  "strategy": {
    "id": 1,
    "strategyName": "Test Strategy",
    "status": "Active",
    "isActive": true,
    "currentStep": 1,
    "startDate": "2024-01-01T10:00:00",
    ...
  },
  "events": [
    {
      "step": 0,
      "ruleName": "TimeCheckRule",
      "action": "CHECK",
      "timestamp": "2024-01-01T10:00:00"
    },
    ...
  ]
}
```

**Kontrol Noktaları:**
- ✅ `status` = `"Active"`
- ✅ `isActive` = `true`
- ✅ `currentStep` değeri var (0, 1, 2, 3 veya -1)
- ✅ `events` array'i boş değil

---

### Adım 2: Event Loglarını İncele

```http
GET http://localhost:5184/api/Strategy/events/1/1?limit=20
```

**Beklenen Event Sırası:**

1. **Step 0 - TimeCheckRule**
   - Piyasa saatini kontrol eder (10:00-17:59)
   - Piyasa açıksa → Step 1'e geçer
   - Piyasa kapalıysa → Step -1 (sonlandırır)

2. **Step 1 - PortfolioCheckRule**
   - Portföyde hisse var mı kontrol eder
   - Varsa → Step 2 (SellRule)
   - Yoksa → Step 3 (BuyRule)

3. **Step 2 - SellRule** (Portföyde hisse varsa)
   - Take Profit kontrolü
   - Stop Loss kontrolü
   - Koşul sağlanırsa → TradeService'e satış emri
   - Sonra → Step -1

4. **Step 3 - BuyRule** (Portföyde hisse yoksa)
   - Entry threshold kontrolü
   - Bakiye kontrolü
   - Koşul sağlanırsa → TradeService'e alım emri
   - Sonra → Step -1

**Kontrol Noktaları:**
- ✅ Step 0 event'i var mı?
- ✅ Step 1 event'i var mı?
- ✅ Step 2 veya Step 3 event'i var mı?
- ✅ Son event son 5 dakika içinde mi?

---

### Adım 3: Otomatik Doğrulama Endpoint'ini Kullan

```http
GET http://localhost:5184/api/Strategy/verifyExecution/1/1
```

**Bu endpoint şunları kontrol eder:**
- ✅ Strateji aktif mi?
- ✅ Event'ler kaydediliyor mu?
- ✅ Step ilerlemesi doğru mu?
- ✅ Son event ne zaman?
- ✅ Worker service çalışıyor mu?
- ✅ İşlemler (BUY/SELL) gerçekleşti mi?

**Örnek Response:**
```json
{
  "success": true,
  "verification": {
    "strategyExists": true,
    "status": "Active",
    "isActive": true,
    "currentStep": 2,
    "totalEvents": 15,
    "workerServiceRunning": true,
    "isWorking": true,
    "hasTradeActions": true,
    "lastActionTime": "2024-01-01T10:30:00",
    "timeSinceLastEvent": 45.5
  },
  "recommendations": [],
  "conclusion": "✅ Strateji çalışıyor görünüyor"
}
```

---

## 🚨 Sorun Giderme

### Problem 1: Hiç Event Yok

**Belirtiler:**
- `events` array'i boş
- `totalEvents` = 0

**Olası Nedenler:**
1. Worker service çalışmıyor
2. Strateji aktif değil
3. Piyasa kapalı (10:00-17:59 dışında)

**Çözüm:**
```bash
# 1. Worker service'i kontrol et
# StrategyRuleService.Worker projesini çalıştır veya
# API projesinde StrategyProcessingHostedService'in çalıştığından emin ol

# 2. Logları kontrol et
# "StrategyProcessingHostedService started" mesajını ara

# 3. Strateji durumunu kontrol et
GET /api/Strategy/getStrategyDetail/{strategyId}/{userId}
# isActive = true ve status = "Active" olmalı
```

---

### Problem 2: Step İlerlemiyor

**Belirtiler:**
- `currentStep` hep aynı kalıyor (örn: hep 0)
- Event'lerde sadece Step 0 var

**Olası Nedenler:**
1. Piyasa kapalı (Step 0'da takılı kalır)
2. MarketDataService'e erişilemiyor
3. PortfolioService'e erişilemiyor

**Çözüm:**
```bash
# 1. Piyasa saatini kontrol et
# Şu anki saat 10:00-17:59 arasında mı?

# 2. MarketDataService'i kontrol et
GET http://localhost:5275/api/quotes/{stockSymbol}

# 3. PortfolioService'i kontrol et
GET http://localhost:5242/api/portfolio/getActiveStocks/{userId}
```

---

### Problem 3: İşlemler (BUY/SELL) Gerçekleşmiyor

**Belirtiler:**
- Event'ler var ama `action` = "BUY" veya "SELL" yok
- Step -1'e geçmiyor

**Olası Nedenler:**
1. Koşullar sağlanmıyor (fiyat, bakiye, vb.)
2. TradeService'e erişilemiyor
3. Bakiye yetersiz

**Çözüm:**
```bash
# 1. Strateji tercihlerini kontrol et
GET /api/Strategy/getStrategyDetail/{strategyId}/{userId}
# stopLossPercentage, takeProfitPercentage, entryThresholdPercentage

# 2. Mevcut fiyatı kontrol et
GET http://localhost:5275/api/quotes/{stockSymbol}

# 3. Bakiyeyi kontrol et
GET http://localhost:5239/api/account/getAccountByUser/{userId}

# 4. Event loglarını detaylı incele
GET /api/Strategy/events/{strategyId}/{userId}
# Hangi step'te takılı kaldığını gör
```

---

### Problem 4: Son Event Çok Eski

**Belirtiler:**
- `timeSinceLastEvent` > 300 saniye (5 dakika)
- Son event saatler önce

**Olası Nedenler:**
1. Worker service durdu
2. Strateji süresi doldu (expiryDate)
3. Strateji pasif yapıldı

**Çözüm:**
```bash
# 1. Worker service'i yeniden başlat

# 2. Strateji durumunu kontrol et
GET /api/Strategy/getStrategyDetail/{strategyId}/{userId}
# expiryDate kontrol et

# 3. Stratejiyi yeniden aktif et (gerekirse)
PUT /api/Strategy/updatePreferences
# veya yeni strateji oluştur
```

---

## 📊 Monitoring Dashboard (Frontend)

Frontend'de strateji durumunu görmek için:

```typescript
// Strategy detail sayfasında
const verifyStrategy = async () => {
  const response = await fetch(
    `/api/Strategy/verifyExecution/${strategyId}/${userId}`
  );
  const data = await response.json();
  
  if (data.verification.isWorking) {
    console.log("✅ Strateji çalışıyor");
  } else {
    console.log("❌ Strateji çalışmıyor:", data.recommendations);
  }
};

// Real-time event monitoring
const pollEvents = async () => {
  setInterval(async () => {
    const response = await fetch(
      `/api/Strategy/events/${strategyId}/${userId}?limit=10`
    );
    const data = await response.json();
    setEvents(data.events);
  }, 5000); // Her 5 saniyede bir
};
```

---

## 🎯 Özet: Strateji Çalışıyor mu?

**Evet, çalışıyor eğer:**
- ✅ `isActive` = `true`
- ✅ `status` = `"Active"`
- ✅ Event'ler kaydediliyor
- ✅ Son event son 5 dakika içinde
- ✅ Step ilerlemesi var (0 → 1 → 2/3 → -1)
- ✅ `verifyExecution` endpoint'i `"✅ Strateji çalışıyor görünüyor"` diyor

**Hayır, çalışmıyor eğer:**
- ❌ `isActive` = `false`
- ❌ `status` ≠ `"Active"`
- ❌ Hiç event yok
- ❌ Son event 5 dakikadan eski
- ❌ Step ilerlemiyor
- ❌ `verifyExecution` endpoint'i `"❌ Strateji çalışmıyor"` diyor

---

## 🔗 İlgili Endpoint'ler

- `GET /api/Strategy/getStrategyDetail/{strategyId}/{userId}` - Strateji detayı
- `GET /api/Strategy/events/{strategyId}/{userId}` - Event logları
- `GET /api/Strategy/verifyExecution/{strategyId}/{userId}` - Otomatik doğrulama
- `GET /api/Strategy/workerStatus` - Worker service durumu
- `GET /api/Strategy/steps/{strategyId}/{userId}` - Step detayları

---

## 📝 Notlar

- Worker service her 5 saniyede bir (varsayılan) stratejileri işler
- Event'ler her işlemde kaydedilir
- Step -1 = Strateji tamamlandı/sonlandırıldı
- Piyasa saatleri: 10:00 - 17:59 (Türkiye saati)

