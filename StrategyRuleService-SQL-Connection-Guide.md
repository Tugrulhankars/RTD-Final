# StrategyRuleService SQL Server Bağlantı Sorunu Çözüm Rehberi

## 1. Connection String Formatı Kontrolü

### Mevcut Durum
- **docker-compose.yml**: `Server=host.docker.internal,1433;Database=RtdStartegyRule-Service;User Id=metropol;Password=20002002;TrustServerCertificate=True;Encrypt=false;Connection Timeout=30;`
- **appsettings.json**: `Server=localhost;Database=RtdStartegyRule-Service;User Id=sa;Password=20002002.;Encrypt=False;TrustServerCertificate=True;`

### Önerilen Connection String Formatı

Docker içinde çalışırken:
```
Server=host.docker.internal,1433;Database=RtdStartegyRule-Service;User Id=metropol;Password=20002002;TrustServerCertificate=True;Encrypt=false;Connection Timeout=30;
```

Yerel geliştirme için (appsettings.json):
```
Server=localhost,1433;Database=RtdStartegyRule-Service;User Id=metropol;Password=20002002;TrustServerCertificate=True;Encrypt=false;Connection Timeout=30;
```

**Önemli Notlar:**
- Port numarasını açıkça belirtin: `,1433` (varsayılan SQL Server portu)
- `Encrypt=false` kullanıyorsanız `TrustServerCertificate=True` olmalı
- `Connection Timeout=30` ekleyin (saniye cinsinden)

## 2. SQL Server Express vs LocalDB Kontrolü

### SQL Server Express Kullanıyorsanız:
- Connection String: `Server=localhost,1433` veya `Server=.\SQLEXPRESS,1433`
- Instance adı: `SQLEXPRESS` (varsayılan)

### LocalDB Kullanıyorsanız:
- Connection String: `Server=(localdb)\MSSQLLocalDB`
- LocalDB Docker'dan erişilemez! Docker kullanıyorsanız SQL Server Express veya Full SQL Server kullanmalısınız.

### Hangi SQL Server Sürümünü Kullandığınızı Kontrol Etme:

```powershell
# SQL Server instance'larını listele
Get-Service -Name "*SQL*" | Select-Object Name, Status, DisplayName

# SQL Server instance'larını detaylı listele
Get-ItemProperty 'HKLM:\SOFTWARE\Microsoft\Microsoft SQL Server\Instance Names\SQL' | Select-Object *
```

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

### PowerShell ile TCP/IP Protokolünü Açma (Alternatif):

```powershell
# SQL Server WMI kullanarak TCP/IP'yi etkinleştir
$wmi = Get-WmiObject -Namespace "root\Microsoft\SqlServer\ComputerManagement15" -Class ServerNetworkProtocol -Filter "InstanceName = 'MSSQLSERVER' AND ProtocolName = 'Tcp'"
$wmi.SetEnable()

# Veya SQL Server Express için:
$wmi = Get-WmiObject -Namespace "root\Microsoft\SqlServer\ComputerManagement15" -Class ServerNetworkProtocol -Filter "InstanceName = 'SQLEXPRESS' AND ProtocolName = 'Tcp'"
$wmi.SetEnable()

# Servisi yeniden başlat
Restart-Service -Name "MSSQLSERVER"
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
docker exec -it rtd-finalproject-strategyrule-service-1 powershell -Command "Test-NetConnection -ComputerName host.docker.internal -Port 1433"
```

## 7. Sorun Giderme Adımları

1. **SQL Server servisinin çalıştığından emin olun:**
   ```powershell
   Get-Service -Name "*SQL*" | Where-Object {$_.Status -ne 'Running'}
   ```

2. **TCP/IP protokolünün etkin olduğunu doğrulayın:**
   - SQL Server Configuration Manager'da kontrol edin

3. **Port 1433'ün dinlendiğini kontrol edin:**
   ```powershell
   netstat -an | findstr "1433"
   ```

4. **Connection string formatını doğrulayın:**
   - Port numarasını açıkça belirtin: `,1433`
   - Kullanıcı adı ve şifrenin doğru olduğundan emin olun

5. **Docker'dan host'a erişimi test edin:**
   ```powershell
   docker exec -it rtd-finalproject-strategyrule-service-1 ping host.docker.internal
   ```

## 8. Önerilen Connection String (Güncellenmiş)

### appsettings.json için:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost,1433;Database=RtdStartegyRule-Service;User Id=metropol;Password=20002002;TrustServerCertificate=True;Encrypt=false;Connection Timeout=30;"
  }
}
```

### docker-compose.yml için (zaten doğru):
```yaml
ConnectionStrings__DefaultConnection=Server=host.docker.internal,1433;Database=RtdStartegyRule-Service;User Id=metropol;Password=20002002;TrustServerCertificate=True;Encrypt=false;Connection Timeout=30;
```

