# Localhost Konfigürasyon Özeti

Proje artık localhost'ta çalışacak şekilde yapılandırıldı. Tüm Dockerfile'lar silindi ve tüm konfigürasyonlar localhost için güncellendi.

## Silinen Dosyalar

- Tüm `Dockerfile` dosyaları silindi (10 dosya)
- `docker-compose.yml` yedeklenebilir (opsiyonel)

## Güncellenen Konfigürasyonlar

### 1. Veritabanı Connection String'leri

Tüm servisler artık Windows Authentication kullanıyor:

**Format:**
```
Server=MetropolTilkisi;Database=<DatabaseName>;Integrated Security=SSPI;Persist Security Info=False;Trusted_Connection=True;Encrypt=false;TrustServerCertificate=True;
```

**Güncellenen Servisler:**
- **AccountService:** `Database=RtdAccount-Service`
- **PortfolioService:** `Database=RtdPortfolio-Service`
- **PaymentService:** `Database=RtdPayment-Service`
- **StrategyRuleService:** `Database=RtdStrategyRule-Service` (typo düzeltildi: Startegy → Strategy)

### 2. Gateway Konfigürasyonu

**RTD-Gateway/Gateway/appsettings.json** ve **appsettings.Development.json**:
- Tüm servis adresleri `localhost` olarak güncellendi:
  - `http://localhost:8081` (AuthUser-Service)
  - `http://localhost:5239` (Account-Service)
  - `http://localhost:5242` (Portfolio-Service)
  - `http://localhost:5231` (Payment-Service)
  - `http://localhost:5275` (MarketData-Service)
  - `http://localhost:5184` (StrategyRule-Service)
  - `http://localhost:9084` (Trading-Service)

### 3. Frontend Konfigürasyonu

**RTDFrontend/rtd/src/api/apiClient.ts:**
- API Base URL: `http://localhost:9082` (Gateway)

**RTDFrontend/rtd/src/api/strategyService.ts:**
- Strategy API Base URL: `http://localhost:9082` (Gateway)

### 4. gRPC Servis Adresleri

**StrategyRuleService/Api/appsettings.json:**
- AccountServiceAddress: `https://localhost:5001`
- PortfolioServiceAddress: `https://localhost:5002`
- MarketDataServiceAddress: `https://localhost:5004`
- TradeServiceAddress: `http://localhost:9084` (HTTP olarak güncellendi)

### 5. Servis İçi Bağlantılar

Tüm servisler artık birbirlerine `localhost` üzerinden bağlanıyor:
- AccountService → PortfolioService: `http://localhost:5242`
- StrategyRuleService → MarketDataService: `http://localhost:5275`
- StrategyRuleService → AuthUserService: `http://localhost:8081`
- TradingService → AccountService: `http://localhost:5239`
- TradingService → PortfolioService: `http://localhost:5242`

## Servis Portları

| Servis | Port | URL |
|--------|------|-----|
| AuthUser-Service | 8081 | http://localhost:8081 |
| Account-Service | 5239 | http://localhost:5239 |
| Portfolio-Service | 5242 | http://localhost:5242 |
| Payment-Service | 5231 | http://localhost:5231 |
| MarketData-Service | 5275 | http://localhost:5275 |
| StrategyRule-Service | 5184 | http://localhost:5184 |
| Trading-Service | 9084 | http://localhost:9084 |
| Gateway | 9082 | http://localhost:9082 |
| Frontend | 3000 | http://localhost:3000 |

## gRPC Portları

| Servis | gRPC Port |
|--------|-----------|
| Account-Service | 5001 |
| Portfolio-Service | 5002 |
| MarketData-Service | 5004 |

## Veritabanı Bilgileri

**SQL Server:**
- Server: `MetropolTilkisi`
- Authentication: Windows Authentication (Integrated Security=SSPI)
- Databases:
  - `RtdAccount-Service`
  - `RtdPortfolio-Service`
  - `RtdPayment-Service`
  - `RtdStrategyRule-Service`

**PostgreSQL:**
- Host: `localhost`
- Port: `5432`
- Database: `rtd_authservice`
- User: `metropol`
- Password: `20002002`

## Servisleri Başlatma

### Otomatik Başlatma

```powershell
.\start-all-services.ps1
```

### Manuel Başlatma

Her servisi ayrı terminal penceresinde başlatın:

1. **AuthUser-Service:**
   ```powershell
   cd RTD-AuthUser-Service
   mvn spring-boot:run
   ```

2. **Account-Service:**
   ```powershell
   cd AccountService\AccountService
   dotnet run
   ```

3. **Portfolio-Service:**
   ```powershell
   cd PortfolioService\PortfolioService
   dotnet run
   ```

4. **Payment-Service:**
   ```powershell
   cd PaymentService\PaymentService
   dotnet run
   ```

5. **MarketData-Service:**
   ```powershell
   cd MarketDataService\MarketDataService
   dotnet run
   ```

6. **StrategyRule-Service:**
   ```powershell
   cd StrategyRuleService\Api
   dotnet run
   ```

7. **Trading-Service:**
   ```powershell
   cd tradingService
   go run cmd/main.go
   ```

8. **Gateway:**
   ```powershell
   cd RTD-Gateway\Gateway
   dotnet run
   ```

9. **Frontend:**
   ```powershell
   cd RTDFrontend\rtd
   npm run dev
   ```

## Önemli Notlar

1. **SQL Server TCP/IP Protokolü:** Port 1433'ün açık olması gerekiyor (eğer SQL Authentication kullanılacaksa)
2. **Windows Authentication:** Localhost'ta Windows Authentication kullanılıyor
3. **Kafka:** `localhost:19092` üzerinden çalışıyor
4. **RabbitMQ:** CloudAMQP kullanılıyor (değişmedi)
5. **Frontend:** `http://localhost:3000` üzerinden çalışıyor ve Gateway'e (`http://localhost:9082`) bağlanıyor

## Kontrol Komutları

```powershell
# SQL Server servis durumu
Get-Service -Name "MSSQLSERVER"

# Port dinleme durumu
netstat -an | findstr "LISTENING" | findstr ":8081 :5239 :5242 :5231 :5275 :5184 :9084 :9082"

# Connection string kontrolü
.\verify-connection-strings.ps1
```

