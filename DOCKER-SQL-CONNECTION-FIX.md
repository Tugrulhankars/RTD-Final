# Docker'dan SQL Server Bağlantı Sorunu Çözüm Rehberi

## Sorun
Docker container'larından (`account-service`, `portfolio-service`, `payment-service`, `strategyrule-service`) localhost'taki SQL Server'a bağlanılamıyor.

**Hata:**
```
A network-related or instance-specific error occurred while establishing a connection to SQL Server. 
The server was not found or was not accessible. (provider: TCP Provider, error: 40 - Could not open a connection to SQL Server)
```

## Çözüm Adımları

### 1. SQL Server TCP/IP Protokolünü Açın

**SQL Server Configuration Manager ile:**

1. Windows tuşuna basın ve "SQL Server Configuration Manager" yazın
2. Sol panelde: **SQL Server Network Configuration** > **Protocols for MSSQLSERVER**
3. Sağ panelde **"TCP/IP"** öğesine sağ tıklayın > **Enable**
4. **"TCP/IP"** öğesine çift tıklayın
5. **"IP Addresses"** sekmesine gidin
6. En alta kaydırın ve **"IPAll"** bölümünü bulun
7. **"TCP Dynamic Ports"** alanını boşaltın (silin)
8. **"TCP Port"** alanına `1433` yazın
9. **"OK"** butonuna tıklayın

### 2. SQL Server Servisini Yeniden Başlatın

PowerShell'i **Yönetici olarak** çalıştırın:

```powershell
Restart-Service -Name "MSSQLSERVER"
```

### 3. Windows Firewall Kuralını Ekleyin

PowerShell'i **Yönetici olarak** çalıştırın:

```powershell
New-NetFirewallRule -DisplayName "SQL Server" -Direction Inbound -LocalPort 1433 -Protocol TCP -Action Allow
```

### 4. host.docker.internal Erişimini Test Edin

Docker container'ından test:

```powershell
# AccountService container'ından test
docker exec -it rtd-finalproject-account-service-1 sh -c "ping -c 3 host.docker.internal"
```

### 5. Port 1433'ün Dinlendiğini Doğrulayın

PowerShell'de:

```powershell
netstat -an | findstr "1433"
```

Çıktıda `LISTENING` görmelisiniz.

### 6. Docker Container'larını Yeniden Başlatın

```powershell
docker-compose restart account-service portfolio-service payment-service strategyrule-service
```

## Alternatif Çözüm: SQL Server Express Instance Adı

Eğer SQL Server Express kullanıyorsanız, connection string'de instance adını belirtin:

```yaml
ConnectionStrings__DefaultConnection=Server=host.docker.internal,1433;Database=RTD-AccountService;User Id=metropol;Password=20002002;TrustServerCertificate=True;Encrypt=false;Connection Timeout=30;
```

Veya instance adı ile:

```yaml
ConnectionStrings__DefaultConnection=Server=host.docker.internal\SQLEXPRESS,1433;Database=RTD-AccountService;User Id=metropol;Password=20002002;TrustServerCertificate=True;Encrypt=false;Connection Timeout=30;
```

## Kontrol Komutları

### SQL Server Durumunu Kontrol Etme:

```powershell
# SQL Server servislerini kontrol et
Get-Service -Name "*SQL*" | Format-Table -AutoSize

# Port 1433'ü kontrol et
Test-NetConnection -ComputerName localhost -Port 1433
```

### Docker Container Loglarını Kontrol Etme:

```powershell
# AccountService logları
docker-compose logs account-service --tail 50

# Tüm servislerin logları
docker-compose logs --tail 50
```

## Önemli Notlar

1. **TCP/IP Protokolü**: SQL Server'da TCP/IP protokolü mutlaka açık olmalı
2. **Port 1433**: Varsayılan port 1433'tür, farklı bir port kullanıyorsanız connection string'de belirtin
3. **Windows Firewall**: Port 1433'ün gelen bağlantılara açık olduğundan emin olun
4. **host.docker.internal**: Docker Desktop'ta otomatik olarak çalışır, ancak bazı durumlarda manuel olarak eklenmesi gerekebilir

## Sorun Giderme

### Eğer hala bağlanamıyorsa:

1. **SQL Server Browser servisini başlatın:**
   ```powershell
   Start-Service -Name "SQLBrowser"
   ```

2. **Docker network'ü kontrol edin:**
   ```powershell
   docker network inspect rtd-finalproject_rtd-network
   ```

3. **host.docker.internal IP adresini kontrol edin:**
   ```powershell
   # Windows'ta
   ping host.docker.internal
   ```

4. **Connection string'i doğrulayın:**
   - Port numarası açıkça belirtilmiş mi? (`,1433`)
   - Kullanıcı adı ve şifre doğru mu?
   - `TrustServerCertificate=True` var mı?

## Test

Bağlantıyı test etmek için:

```powershell
# AccountService endpoint'ini test et
Invoke-WebRequest -Uri "http://localhost:5239/api/account/getAccountByUser/26" -UseBasicParsing
```

Başarılı olursa, veritabanı bağlantısı çalışıyor demektir.

