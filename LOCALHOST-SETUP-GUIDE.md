# Localhost Servis Başlatma Rehberi

Bu rehber, Docker kullanmadan localhost'ta servisleri başlatmak için hazırlanmıştır.

## Ön Hazırlık

### 1. SQL Server Servislerini Kontrol Et

```powershell
.\check-sql-server-services.ps1
```

Veya manuel olarak:
- `Win + R` → `services.msc` → Enter
- `SQL Server (MSSQLSERVER)` veya `SQL Server (SQLEXPRESS)` servisini bulun
- Durumunun **"Running"** olduğundan emin olun

### 2. TCP/IP Protokolünü Aktif Et

```powershell
.\check-tcpip-protocol.ps1
```

Veya manuel olarak:
1. **SQL Server Configuration Manager**'ı açın
2. **SQL Server Network Configuration** → **Protocols for MSSQLSERVER**
3. **TCP/IP** öğesine sağ tıklayın → **Enable**
4. **TCP/IP** öğesine çift tıklayın
5. **IP Addresses** sekmesine gidin
6. En alta kaydırın → **IPAll** bölümü:
   - **TCP Dynamic Ports** alanını **BOŞALTIN**
   - **TCP Port** alanına **`1433`** yazın
   - **OK** butonuna tıklayın
7. **SQL Server servisini yeniden başlatın:**
   ```powershell
   Restart-Service -Name "MSSQLSERVER"
   ```

### 3. Connection String'leri Kontrol Et

```powershell
.\verify-connection-strings.ps1
```

**Doğru Connection String Formatı:**

**Windows Authentication için:**
```json
"ConnectionStrings": {
  "DefaultConnection": "Server=localhost;Database=RtdAccount-Service;Integrated Security=True;TrustServerCertificate=True;Encrypt=false;"
}
```

**SQL Authentication için:**
```json
"ConnectionStrings": {
  "DefaultConnection": "Server=localhost,1433;Database=RtdAccount-Service;User Id=metropol;Password=20002002;TrustServerCertificate=True;Encrypt=false;Connection Timeout=30;"
}
```

**SQL Express için:**
```json
"ConnectionStrings": {
  "DefaultConnection": "Server=.\\SQLEXPRESS;Database=RtdAccount-Service;Integrated Security=True;TrustServerCertificate=True;Encrypt=false;"
}
```

## Servisleri Başlatma

### Otomatik Başlatma (Önerilen)

```powershell
.\start-all-services.ps1
```

Bu script:
1. SQL Server servisini kontrol eder ve başlatır
2. 3 saniye bekler (SQL Server'in tamamen başlaması için)
3. Tüm servisleri sırayla başlatır (her biri arasında 2 saniye bekler)

### Manuel Başlatma

Her servisi ayrı PowerShell penceresinde başlatın:

#### 1. AuthUser-Service
```powershell
cd RTD-AuthUser-Service
mvn spring-boot:run
```

#### 2. Account-Service
```powershell
cd AccountService\AccountService
dotnet run
```

#### 3. Portfolio-Service
```powershell
cd PortfolioService\PortfolioService
dotnet run
```

#### 4. Payment-Service
```powershell
cd PaymentService\PaymentService
dotnet run
```

#### 5. MarketData-Service
```powershell
cd MarketDataService\MarketDataService
dotnet run
```

#### 6. StrategyRule-Service
```powershell
cd StrategyRuleService\Api
dotnet run
```

#### 7. Trading-Service
```powershell
cd tradingService
go run .
```

#### 8. Gateway
```powershell
cd RTD-Gateway\Gateway
dotnet run
```

## Servis Portları

- **AuthUser-Service:** http://localhost:8081
- **Account-Service:** http://localhost:5239
- **Portfolio-Service:** http://localhost:5242
- **Payment-Service:** http://localhost:5231
- **MarketData-Service:** http://localhost:5275
- **StrategyRule-Service:** http://localhost:5184
- **Trading-Service:** http://localhost:9084
- **Gateway:** http://localhost:9082

## Sorun Giderme

### Error 40 - Could not open a connection to SQL Server

**Nedenler:**
1. SQL Server servisi çalışmıyor
2. TCP/IP protokolü kapalı
3. Port 1433 dinlenmiyor
4. Connection string yanlış

**Çözüm:**
1. `check-sql-server-services.ps1` çalıştırın
2. `check-tcpip-protocol.ps1` çalıştırın ve manuel adımları uygulayın
3. `verify-connection-strings.ps1` çalıştırın ve connection string'leri düzeltin

### Servisler çok hızlı başlatılıyor

**Sorun:** SQL Server henüz tamamen başlamadan servisler bağlanmaya çalışıyor.

**Çözüm:** `start-all-services.ps1` script'ini kullanın. Bu script servisler arasında bekleme süresi ekler.

### Connection String Formatı

**Yanlış:**
```json
"Server=db;Database=MyDb;..."  // Docker için
"Server=sqlserver;Database=MyDb;..."  // Docker için
```

**Doğru:**
```json
"Server=localhost;Database=MyDb;..."  // Localhost için
"Server=127.0.0.1;Database=MyDb;..."  // Localhost için
"Server=.\\SQLEXPRESS;Database=MyDb;..."  // SQL Express için
```

## Önemli Notlar

1. **SQL Server servisi mutlaka çalışıyor olmalı**
2. **TCP/IP protokolü mutlaka açık olmalı**
3. **Port 1433 dinleniyor olmalı**
4. **Connection string'lerde `localhost` veya `127.0.0.1` kullanılmalı** (Docker değil!)
5. **Servisler arasında 2-3 saniye bekleme süresi olmalı**

## Hızlı Kontrol Komutları

```powershell
# SQL Server servis durumu
Get-Service -Name "MSSQLSERVER"

# Port 1433 kontrolü
Test-NetConnection -ComputerName localhost -Port 1433

# Connection string kontrolü
.\verify-connection-strings.ps1
```

