# StrategyRuleService SQL Server Bağlantı Kontrol Scripti
# Bu script SQL Server servislerinin durumunu ve bağlantı ayarlarını kontrol eder

Write-Host "=== SQL Server Bağlantı Kontrol Scripti ===" -ForegroundColor Cyan
Write-Host ""

# 1. SQL Server Servislerini Kontrol Et
Write-Host "1. SQL Server Servislerinin Durumu:" -ForegroundColor Yellow
$sqlServices = Get-Service -Name "*SQL*" -ErrorAction SilentlyContinue
if ($sqlServices) {
    $sqlServices | Format-Table -AutoSize Name, Status, DisplayName
} else {
    Write-Host "SQL Server servisi bulunamadı!" -ForegroundColor Red
}

Write-Host ""

# 2. Çalışmayan SQL Server Servislerini Listele
Write-Host "2. Çalışmayan SQL Server Servisleri:" -ForegroundColor Yellow
$stoppedServices = $sqlServices | Where-Object {$_.Status -ne 'Running'}
if ($stoppedServices) {
    $stoppedServices | Format-Table -AutoSize Name, Status, DisplayName
    Write-Host "UYARI: Çalışmayan servisler var! Lütfen bu servisleri başlatın." -ForegroundColor Red
} else {
    Write-Host "Tüm SQL Server servisleri çalışıyor." -ForegroundColor Green
}

Write-Host ""

# 3. Port 1433 Kontrolü
Write-Host "3. Port 1433 Dinleme Durumu:" -ForegroundColor Yellow
$port1433 = Get-NetTCPConnection -LocalPort 1433 -ErrorAction SilentlyContinue
if ($port1433) {
    Write-Host "Port 1433 dinleniyor:" -ForegroundColor Green
    $port1433 | Select-Object LocalAddress, LocalPort, State | Format-Table -AutoSize
} else {
    Write-Host "Port 1433 dinlenmiyor! SQL Server TCP/IP protokolü kapalı olabilir." -ForegroundColor Red
}

Write-Host ""

# 4. Test-NetConnection ile Bağlantı Testi
Write-Host "4. localhost:1433 Bağlantı Testi:" -ForegroundColor Yellow
$testConnection = Test-NetConnection -ComputerName localhost -Port 1433 -WarningAction SilentlyContinue
if ($testConnection.TcpTestSucceeded) {
    Write-Host "Bağlantı başarılı! Port 1433 erişilebilir." -ForegroundColor Green
} else {
    Write-Host "Bağlantı başarısız! Port 1433 erişilemiyor." -ForegroundColor Red
    Write-Host "Lütfen SQL Server Configuration Manager'da TCP/IP protokolünü açın." -ForegroundColor Yellow
}

Write-Host ""

# 5. SQL Server Instance'larını Listele
Write-Host "5. SQL Server Instance'ları:" -ForegroundColor Yellow
try {
    $instances = Get-ItemProperty 'HKLM:\SOFTWARE\Microsoft\Microsoft SQL Server\Instance Names\SQL' -ErrorAction SilentlyContinue
    if ($instances) {
        $instances.PSObject.Properties | Where-Object {$_.Name -ne 'PSPath' -and $_.Name -ne 'PSParentPath' -and $_.Name -ne 'PSChildName' -and $_.Name -ne 'PSDrive' -and $_.Name -ne 'PSProvider'} | ForEach-Object {
            Write-Host "  - $($_.Name): $($_.Value)" -ForegroundColor Cyan
        }
    } else {
        Write-Host "SQL Server instance bulunamadı." -ForegroundColor Red
    }
} catch {
    Write-Host "SQL Server instance bilgisi alınamadı: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host ""

# 6. Windows Firewall Kuralları
Write-Host "6. Windows Firewall SQL Server Kuralları:" -ForegroundColor Yellow
$firewallRules = Get-NetFirewallRule -DisplayName "*SQL*" -ErrorAction SilentlyContinue
if ($firewallRules) {
    $firewallRules | Select-Object DisplayName, Enabled, Direction | Format-Table -AutoSize
} else {
    Write-Host "SQL Server için firewall kuralı bulunamadı." -ForegroundColor Yellow
    Write-Host "Gerekirse şu komutu çalıştırın:" -ForegroundColor Yellow
    Write-Host "New-NetFirewallRule -DisplayName 'SQL Server' -Direction Inbound -LocalPort 1433 -Protocol TCP -Action Allow" -ForegroundColor Cyan
}

Write-Host ""

# 7. Öneriler
Write-Host "=== Öneriler ===" -ForegroundColor Cyan
Write-Host ""

if ($stoppedServices) {
    Write-Host "1. Çalışmayan SQL Server servislerini başlatın:" -ForegroundColor Yellow
    $stoppedServices | ForEach-Object {
        Write-Host "   Start-Service -Name '$($_.Name)'" -ForegroundColor Cyan
    }
    Write-Host ""
}

if (-not $port1433) {
    Write-Host "2. SQL Server Configuration Manager'da TCP/IP protokolünü açın:" -ForegroundColor Yellow
    Write-Host "   - SQL Server Network Configuration > Protocols for [INSTANCE]" -ForegroundColor Cyan
    Write-Host "   - TCP/IP'ye sağ tıklayın > Enable" -ForegroundColor Cyan
    Write-Host "   - TCP/IP Properties > IP Addresses > IPAll > TCP Port: 1433" -ForegroundColor Cyan
    Write-Host "   - SQL Server servisini yeniden başlatın" -ForegroundColor Cyan
    Write-Host ""
}

Write-Host "3. Connection String formatı:" -ForegroundColor Yellow
Write-Host "   Server=localhost,1433;Database=RtdStartegyRule-Service;User Id=metropol;Password=20002002;TrustServerCertificate=True;Encrypt=false;Connection Timeout=30;" -ForegroundColor Cyan
Write-Host ""

Write-Host "4. Docker'dan host'a bağlantı testi:" -ForegroundColor Yellow
Write-Host "   docker exec -it rtd-finalproject-strategyrule-service-1 powershell -Command 'Test-NetConnection -ComputerName host.docker.internal -Port 1433'" -ForegroundColor Cyan
Write-Host ""

Write-Host "=== Kontrol Tamamlandı ===" -ForegroundColor Cyan

