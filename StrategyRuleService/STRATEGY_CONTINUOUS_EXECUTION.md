# Strateji Sürekli Çalışma Mekanizması

## 🎯 Soru: Strateji 3 Saat Sonra Gerçekleşecek, Gerçekten Çalışacak mı?

**CEVAP: EVET!** ✅ Strateji sürekli çalışır ve koşullar sağlandığında otomatik olarak işlem yapar.

---

## 🔄 Nasıl Çalışıyor?

### 1. **Worker Service Sürekli Çalışır**

```csharp
// StrategyProcessingHostedService.cs
protected override async Task ExecuteAsync(CancellationToken stoppingToken)
{
    // Her 5 saniyede bir (varsayılan) stratejileri işle
    while (!stoppingToken.IsCancellationRequested)
    {
        await _rulesService.ProcessRulesAsync(); // Tüm aktif stratejileri işle
        await Task.Delay(_interval, stoppingToken); // 5 saniye bekle
    }
}
```

**Önemli:** Worker service başlatıldığında veritabanından **tüm aktif stratejileri otomatik yükler**.

### 2. **Her 5 Saniyede Bir Kontrol**

Worker service her 5 saniyede bir:
- ✅ Tüm aktif stratejileri işler
- ✅ Piyasa verilerini günceller
- ✅ Kuralları değerlendirir
- ✅ Koşullar sağlandığında işlem yapar

### 3. **Strateji Yaşam Döngüsü**

```
Strateji Oluşturuldu
    ↓
Worker Service'e Eklendi (_strategySessions)
    ↓
Her 5 Saniyede Bir İşleniyor
    ↓
[3 Saat Sonra] Koşullar Sağlandı
    ↓
Alım/Satım Emri Gönderildi
    ↓
Strateji Tamamlandı (Step = -1)
```

---

## ✅ Garantiler

### Strateji Çalışır Eğer:

1. ✅ **Worker Service Çalışıyor**
   - StrategyProcessingHostedService başlatıldı
   - Loglarda "StrategyProcessingHostedService started" mesajı var

2. ✅ **Strateji Aktif**
   - `IsActive = true`
   - `Status = Active`
   - `ExpiryDate` dolmamış (varsa)

3. ✅ **Strateji Worker Service'de**
   - `_strategySessions` dictionary'sinde var
   - Worker service başlatıldığında otomatik yüklenir

4. ✅ **Piyasa Açık** (Step 0 için)
   - Saat 10:00 - 17:59 arası

---

## 🔧 Yapılan İyileştirmeler

### 1. **Otomatik Strateji Yükleme**

Worker service başlatıldığında veritabanından aktif stratejileri otomatik yükler:

```csharp
private async Task LoadActiveStrategiesFromDatabase(CancellationToken cancellationToken)
{
    // Veritabanından aktif stratejileri getir
    var activeStrategies = await strategyRepository.GetAllAsync(
        predicate: s => s.IsActive && 
                       s.Status == StrategyStatus.Active &&
                       (!s.ExpiryDate.HasValue || s.ExpiryDate.Value > now),
        cancellationToken: cancellationToken);

    // Her stratejiyi NRulesService'e ekle
    foreach (var strategy in activeStrategies)
    {
        await nRulesService.AddStrategyAsync(strategyKey, strategyContext);
    }
}
```

**Fayda:** Worker service yeniden başlatılsa bile, aktif stratejiler otomatik yüklenir.

### 2. **Sürekli İşleme**

```csharp
// Her 5 saniyede bir (varsayılan)
while (!stoppingToken.IsCancellationRequested)
{
    await _rulesService.ProcessRulesAsync(); // Tüm stratejileri işle
    await Task.Delay(_interval, stoppingToken);
}
```

**Fayda:** Stratejiler sürekli izlenir, koşullar anında kontrol edilir.

---

## 📊 Örnek Senaryo: 3 Saat Sonra Alım

### Senaryo:
1. **10:00** - Strateji oluşturuldu
   - Entry Threshold: -5% (Açılış fiyatının %5 altı)
   - Açılış Fiyatı: ₺100
   - Entry Fiyatı: ₺95 (₺100 * 0.95)

2. **10:00 - 13:00** - Worker service sürekli çalışıyor
   - Her 5 saniyede bir fiyat kontrol ediliyor
   - Fiyat hala ₺95'ten yüksek
   - BuyRule tetiklenmiyor

3. **13:00** - Fiyat ₺94'e düştü
   - Worker service fiyatı kontrol ediyor
   - Entry fiyatına (₺95) ulaşıldı
   - BuyRule tetikleniyor
   - Bakiye kontrol ediliyor
   - **Alım emri TradeService'e gönderiliyor** ✅

4. **13:00:05** - İşlem tamamlandı
   - Step = -1 (Strateji sonlandı)
   - Event kaydedildi
   - Notification gönderildi

---

## 🔍 Doğrulama

### Stratejinin Sürekli Çalıştığını Nasıl Anlarsınız?

1. **Event Loglarını Kontrol Edin:**
```bash
GET /api/Strategy/events/{strategyId}/{userId}
```

**Beklenen:** Her 5 saniyede bir (veya daha sık) event kaydı olmalı

2. **Doğrulama Endpoint'ini Kullanın:**
```bash
GET /api/Strategy/verifyExecution/{strategyId}/{userId}
```

**Kontrol Noktaları:**
- `isWorking` = `true`
- `timeSinceLastEvent` < 300 saniye (5 dakika)
- `totalEvents` > 0

3. **Worker Service Loglarını İnceleyin:**
```
[INFO] StrategyProcessingHostedService started. Interval=5s
[INFO] ProcessRulesAsync başladı - Toplam 3 strateji işlenecek
[INFO] Strateji işleniyor: Strategy_1
[INFO] Strateji işleniyor: Strategy_2
[INFO] Strateji işleniyor: Strategy_3
[INFO] ProcessRulesAsync tamamlandı - 3 strateji işlendi
```

---

## ⚠️ Dikkat Edilmesi Gerekenler

### 1. **Worker Service Çalışmalı**

Eğer worker service durursa:
- ❌ Stratejiler işlenmez
- ❌ Koşullar kontrol edilmez
- ❌ İşlemler yapılmaz

**Çözüm:** Worker service'i sürekli çalışır durumda tutun.

### 2. **ExpiryDate Kontrolü**

Strateji süresi dolduğunda:
- ❌ Otomatik pasif yapılır
- ❌ İşlenmez

**Çözüm:** `durationHours` parametresini doğru ayarlayın.

### 3. **Piyasa Saatleri**

Piyasa kapalıyken (10:00-17:59 dışında):
- ❌ Step 0'da takılı kalır
- ❌ İşlem yapılmaz

**Çözüm:** Piyasa açık saatlerinde strateji oluşturun.

---

## 📝 Özet

### ✅ EVET, Strateji Sürekli Çalışır!

1. **Worker service her 5 saniyede bir stratejileri işler**
2. **Koşullar sürekli kontrol edilir**
3. **Koşullar sağlandığında anında işlem yapılır**
4. **Worker service yeniden başlatılsa bile aktif stratejiler otomatik yüklenir**

### Örnek:
- **10:00** - Strateji oluşturuldu
- **10:00 - 13:00** - Sürekli çalışıyor (her 5 saniyede bir kontrol)
- **13:00** - Koşullar sağlandı → **Alım emri gönderildi** ✅

---

## 🔗 İlgili Dosyalar

- `StrategyProcessingHostedService.cs` - Worker service
- `NRulesService.cs` - Kural işleme motoru
- `CreateStrategyCommandHandler.cs` - Strateji oluşturma
- `STRATEGY_VERIFICATION_GUIDE.md` - Doğrulama rehberi

---

## 💡 İpuçları

1. **Interval Ayarlama:**
```json
{
  "StrategyProcessing": {
    "IntervalSeconds": 5  // Varsayılan 5 saniye
  }
}
```

2. **Logları İzleme:**
```bash
# Worker service loglarını izle
tail -f logs/strategy-worker.log | grep "ProcessRulesAsync"
```

3. **Strateji Durumunu Kontrol:**
```bash
# Her 10 saniyede bir kontrol et
watch -n 10 'curl http://localhost:5184/api/Strategy/verifyExecution/1/1'
```

