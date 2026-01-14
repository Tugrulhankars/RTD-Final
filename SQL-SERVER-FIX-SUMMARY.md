# SQL Server Bağlantı Sorunu - Hızlı Çözüm

## Sorun
Docker container'larından (`account-service`, `strategyrule-service`, `portfolio-service`, `payment-service`) SQL Server'a bağlanılamıyor.

**Hata:**
```
error: 40 - Could not open a connection to SQL Server
```

## Hızlı Çözüm (3 Adım)

### 1. SQL Server Configuration Manager'ı Açın
- Windows tuşuna basın
- "SQL Server Configuration Manager" yazın ve açın

### 2. TCP/IP Protokolünü Açın
1. Sol panel: **SQL Server Network Configuration** > **Protocols for MSSQLSERVER**
2. Sağ panel: **"TCP/IP"** öğesine sağ tıklayın > **Enable**
3. **"TCP/IP"** öğesine çift tıklayın
4. **"IP Addresses"** sekmesine gidin
5. En alta kaydırın ve **"IPAll"** bölümünü bulun
6. **"TCP Dynamic Ports"** alanını **BOŞALTIN** (silin)
7. **"TCP Port"** alanına **`1433`** yazın
8. **"OK"** butonuna tıklayın

### 3. SQL Server Servisini Yeniden Başlatın

PowerShell'i **YÖNETİCİ olarak** açın ve çalıştırın:

```powershell
Restart-Service -Name "MSSQLSERVER"
```

### 4. Firewall Kuralını Ekleyin (Opsiyonel)

```powershell
New-NetFirewallRule -DisplayName "SQL Server" -Direction Inbound -LocalPort 1433 -Protocol TCP -Action Allow
```

## Test

Bağlantıyı test edin:

```powershell
Test-NetConnection -ComputerName localhost -Port 1433
```

**Başarılı olursa:** `TcpTestSucceeded : True` görmelisiniz.

## Docker Container'larını Yeniden Başlatın

```powershell
docker-compose restart account-service strategyrule-service portfolio-service payment-service
```

## Connection String Kontrolü

Docker container'larındaki connection string'ler doğru:

- **AccountService:** `Server=host.docker.internal,1433;Database=RTD-AccountService;User Id=metropol;Password=20002002;TrustServerCertificate=True;Encrypt=false;Connection Timeout=30;`
- **StrategyRuleService:** `Server=host.docker.internal,1433;Database=RtdStartegyRule-Service;User Id=metropol;Password=20002002;TrustServerCertificate=True;Encrypt=false;Connection Timeout=30;`
- **PortfolioService:** `Server=host.docker.internal,1433;Database=RtdPortfolio-Service;User Id=metropol;Password=20002002;TrustServerCertificate=True;Encrypt=false;Connection Timeout=30;`
- **PaymentService:** `Server=host.docker.internal,1433;Database=PaymentServiceDb;User Id=metropol;Password=20002002;TrustServerCertificate=True;Encrypt=false;Connection Timeout=30;`

## Sorun Giderme

### Port 1433 hala dinlenmiyorsa:

1. **SQL Server Browser servisini başlatın:**
   ```powershell
   Start-Service -Name "SQLBrowser"
   ```

2. **SQL Server'ın hangi portta dinlediğini kontrol edin:**
   ```powershell
   Get-NetTCPConnection | Where-Object {$_.LocalPort -like "14*"} | Select-Object LocalPort, State
   ```

3. **Alternatif: Named Pipes kullanın** (sadece localhost için):
   Connection string'de `Server=host.docker.internal` yerine `Server=.\SQLEXPRESS` kullanabilirsiniz, ancak bu Docker'dan çalışmaz.

### host.docker.internal çalışmıyorsa:

Windows'ta Docker Desktop kullanıyorsanız, `host.docker.internal` otomatik olarak çalışmalı. Eğer çalışmıyorsa:

```powershell
# Docker network'ü kontrol edin
docker network inspect rtd-finalproject_rtd-network
```

## Önemli Notlar

- **TCP/IP protokolü mutlaka açık olmalı**
- **Port 1433 açıkça belirtilmiş olmalı** (Dynamic Ports kapalı)
- **SQL Server servisi yeniden başlatılmalı** (TCP/IP değişiklikleri için)
- **Windows Firewall port 1433'ü engellememeli**

