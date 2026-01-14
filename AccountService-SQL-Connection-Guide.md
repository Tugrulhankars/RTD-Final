# AccountService SQL Server Bağlantı Sorunu Çözüm Rehberi

## 1. Connection String Formatı Kontrolü

### Mevcut Durum
- **appsettings.json**: Güncellendi - Port eklendi, SQL Authentication kullanılıyor
- **docker-compose.yml**: Zaten doğru yapılandırılmış

### Önerilen Connection String Formatı

**Yerel geliştirme için (appsettings.json):**
```
Server=localhost,1433;Database=RTD-AccountService;User Id=metropol;Password=20002002;TrustServerCertificate=True;Encrypt=false;Connection Timeout=30;Max Pool Size=100;
```

**Docker içinde çalışırken (docker-compose.yml):**
```
Server=host.docker.internal,1433;Database=RTD-AccountService;User Id=metropol;Password=20002002;TrustServerCertificate=True;Encrypt=false;Connection Timeout=30;
```

**Önemli Notlar:**
- ✅ Port numarasını açıkça belirtin: `,1433` (varsayılan SQL Server portu)
- ✅ `User Id` ve `Password` kullanın (Docker'da Windows Authentication çalışmaz)
- ✅ `Encrypt=false` kullanıyorsanız `TrustServerCertificate=True` olmalı
- ✅ `Connection Timeout=30` ekleyin (saniye cinsinden)
- ✅ `Max Pool Size=100` connection pool için

## 2. Program.cs AddDbContext Yapılandırması

### Mevcut Yapılandırma (Güncellendi)

```csharp
builder.Services.AddDbContext<DatabaseContext>(op =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
        ?? "Server=localhost,1433;Database=RTD-AccountService;User Id=metropol;Password=20002002;TrustServerCertificate=True;Encrypt=false;Connection Timeout=30;";
    
    if (string.IsNullOrWhiteSpace(connectionString))
    {
        throw new InvalidOperationException("Database connection string is null or empty. Please check appsettings.json ConnectionStrings:DefaultConnection");
    }
    
    op.UseSqlServer(connectionString, sqlOptions =>
    {
        sqlOptions.MaxBatchSize(100);
        sqlOptions.CommandTimeout(60);
        
        sqlOptions.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(30),
            errorNumbersToAdd: null
        );
    });
});
```

**Özellikler:**
- ✅ Connection string validation
- ✅ Retry policy (5 deneme, 30 saniye max delay)
- ✅ Command timeout (60 saniye)
- ✅ Max batch size (100)

## 3. SQL Server Servislerinin Çalışıp Çalışmadığını Kontrol Etme

### PowerShell Komutları:

```powershell
# Tüm SQL Server servislerini listele
Get-Service -Name "*SQL*" | Format-Table -AutoSize

# SQL Server servislerinin durumunu kontrol et
Get-Service -Name "*SQL*" | Where-Object {$_.Status -ne 'Running'} | Select-Object Name, Status

# SQL Server Browser servisini başlat (gerekirse)
Start-Service -Name "SQLBrowser"

# SQL Server servisini başlat (örnek: MSSQLSERVER)
Start-Service -Name "MSSQLSERVER"

# SQL Server Express servisini başlat (örnek: MSSQL$SQLEXPRESS)
Start-Service -Name "MSSQL`$SQLEXPRESS"

# Servislerin çalıştığını doğrula
Get-Service -Name "*SQL*" | Where-Object {$_.Status -eq 'Running'}
```

### SQL Server Portunu Kontrol Etme:

```powershell
# SQL Server'ın dinlediği portları kontrol et
Get-NetTCPConnection -LocalPort 1433 -ErrorAction SilentlyContinue | Select-Object LocalAddress, LocalPort, State

# Alternatif: netstat kullanarak
netstat -an | findstr "1433"
```

### SQL Server'a Bağlantı Testi:

```powershell
# Test-NetConnection ile port kontrolü
Test-NetConnection -ComputerName localhost -Port 1433

# SQL Server'a bağlantı testi (SqlServer PowerShell modülü gerekli)
# Install-Module -Name SqlServer -Force (ilk kez kullanıyorsanız)
Import-Module SqlServer
Test-SqlConnection -ServerInstance "localhost,1433" -Username "metropol" -Password "20002002"
```

## 4. TCP/IP Protokolünü Açma (SQL Server Configuration Manager)

### Adım Adım Rehber:

1. **SQL Server Configuration Manager'ı Açın:**
   - Windows tuşuna basın ve "SQL Server Configuration Manager" yazın
   - Veya: `C:\Windows\SysWOW64\SQLServerManager*.msc` (x64 için)
   - Veya: `C:\Windows\System32\SQLServerManager*.msc` (x86 için)

2. **SQL Server Network Configuration'ı Genişletin:**
   - Sol panelde "SQL Server Network Configuration" > "Protocols for [INSTANCE_NAME]" seçin
   - Örnek: "Protocols for MSSQLSERVER" veya "Protocols for SQLEXPRESS"

3. **TCP/IP Protokolünü Etkinleştirin:**
   - Sağ panelde "TCP/IP" öğesine sağ tıklayın
   - "Enable" seçeneğini tıklayın
   - Uyarı mesajını onaylayın

4. **TCP/IP Özelliklerini Yapılandırın:**
   - "TCP/IP" öğesine çift tıklayın
   - "IP Addresses" sekmesine gidin
   - Aşağı kaydırın ve "IPAll" bölümünü bulun
   - "TCP Dynamic Ports" boş bırakın veya silin
   - "TCP Port" alanına `1433` yazın
   - "OK" butonuna tıklayın

5. **SQL Server Servisini Yeniden Başlatın:**
   ```powershell
   # SQL Server servisini yeniden başlat
   Restart-Service -Name "MSSQLSERVER"
   # veya SQL Server Express için:
   Restart-Service -Name "MSSQL`$SQLEXPRESS"
   ```

## 5. Windows Firewall Kurallarını Kontrol Etme

```powershell
# SQL Server için firewall kuralını kontrol et
Get-NetFirewallRule -DisplayName "*SQL*" | Select-Object DisplayName, Enabled, Direction

# SQL Server için gelen bağlantıları aç (gerekirse)
New-NetFirewallRule -DisplayName "SQL Server" -Direction Inbound -LocalPort 1433 -Protocol TCP -Action Allow
```

## 6. Docker'dan Host'a Bağlantı Testi

```powershell
# Docker container içinden SQL Server'a bağlantı testi
docker exec -it rtd-finalproject-account-service-1 powershell -Command "Test-NetConnection -ComputerName host.docker.internal -Port 1433"
```

## 7. Sorun Giderme Adımları

### Adım 1: SQL Server Servisinin Çalıştığını Kontrol Edin
```powershell
Get-Service -Name "*SQL*" | Where-Object {$_.Status -ne 'Running'}
```

### Adım 2: TCP/IP Protokolünün Etkin Olduğunu Doğrulayın
- SQL Server Configuration Manager'da kontrol edin

### Adım 3: Port 1433'ün Dinlendiğini Kontrol Edin
```powershell
netstat -an | findstr "1433"
```

### Adım 4: Connection String Formatını Doğrulayın
- Port numarasını açıkça belirtin: `,1433`
- Kullanıcı adı ve şifrenin doğru olduğundan emin olun
- `TrustServerCertificate=True` ve `Encrypt=false` kullanın

### Adım 5: Docker'dan Host'a Erişimi Test Edin
```powershell
docker exec -it rtd-finalproject-account-service-1 ping host.docker.internal
```

## 8. Yapılan Değişiklikler

### appsettings.json
- ✅ `Integrated Security=True` → `User Id=metropol;Password=20002002` (SQL Authentication)
- ✅ Port numarası eklendi: `localhost,1433`
- ✅ `Connection Timeout=30` eklendi
- ✅ `TrustServerCertificate=True` ve `Encrypt=false` korundu

### Program.cs
- ✅ Fallback connection string güncellendi
- ✅ SQL Authentication kullanılıyor
- ✅ Port numarası eklendi

## 9. Test Komutları

### Yerel Geliştirme İçin:
```powershell
# Connection string testi
$connectionString = "Server=localhost,1433;Database=RTD-AccountService;User Id=metropol;Password=20002002;TrustServerCertificate=True;Encrypt=false;Connection Timeout=30;"
Test-NetConnection -ComputerName localhost -Port 1433
```

### Docker İçin:
```powershell
# Container loglarını kontrol et
docker-compose logs account-service --tail 50

# Container içinden bağlantı testi
docker exec -it rtd-finalproject-account-service-1 powershell -Command "Test-NetConnection -ComputerName host.docker.internal -Port 1433"
```

## 10. Önemli Notlar

1. **Windows Authentication vs SQL Authentication:**
   - Docker container'lar Windows Authentication kullanamaz
   - SQL Authentication (`User Id` ve `Password`) kullanmalısınız
   - Yerel geliştirme için de SQL Authentication kullanın (tutarlılık için)

2. **Port Numarası:**
   - Her zaman port numarasını açıkça belirtin: `,1433`
   - Varsayılan port 1433'tür, ancak belirtmek daha güvenilirdir

3. **TrustServerCertificate:**
   - Geliştirme ortamında `TrustServerCertificate=True` kullanın
   - Production'da SSL sertifikası kullanmalısınız

4. **Connection Timeout:**
   - Varsayılan 15 saniyedir
   - 30 saniye daha güvenilir bir değerdir

