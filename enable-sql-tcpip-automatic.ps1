# SQL Server TCP/IP Protokolünü Kontrol ve Yapılandırma Scripti
# PowerShell'i YÖNETİCİ olarak çalıştırın!

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "SQL Server TCP/IP Yapılandırma Scripti" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# SQL Server servisini kontrol et
Write-Host "[1/4] SQL Server servisini kontrol ediliyor..." -ForegroundColor Yellow
$sqlService = Get-Service -Name "MSSQLSERVER" -ErrorAction SilentlyContinue
if (-not $sqlService) {
    Write-Host "HATA: MSSQLSERVER servisi bulunamadı!" -ForegroundColor Red
    exit 1
}

if ($sqlService.Status -ne "Running") {
    Write-Host "SQL Server servisi çalışmıyor. Başlatılıyor..." -ForegroundColor Yellow
    Start-Service -Name "MSSQLSERVER"
    Start-Sleep -Seconds 5
}

Write-Host "✓ SQL Server servisi çalışıyor" -ForegroundColor Green

# Port 1433 kontrolü
Write-Host ""
Write-Host "[2/4] Port 1433 kontrol ediliyor..." -ForegroundColor Yellow
$port1433 = Get-NetTCPConnection -LocalPort 1433 -ErrorAction SilentlyContinue
if ($port1433) {
    Write-Host "✓ Port 1433 zaten dinleniyor" -ForegroundColor Green
} else {
    Write-Host "✗ Port 1433 dinlenmiyor - TCP/IP protokolü kapalı!" -ForegroundColor Red
}

# Firewall kuralı
Write-Host ""
Write-Host "[3/4] Windows Firewall kuralı kontrol ediliyor..." -ForegroundColor Yellow
$firewallRule = Get-NetFirewallRule -DisplayName "SQL Server" -ErrorAction SilentlyContinue
if ($firewallRule) {
    Write-Host "✓ Firewall kuralı mevcut" -ForegroundColor Green
} else {
    Write-Host "Firewall kuralı oluşturuluyor..." -ForegroundColor Yellow
    try {
        New-NetFirewallRule -DisplayName "SQL Server" -Direction Inbound -LocalPort 1433 -Protocol TCP -Action Allow -ErrorAction Stop | Out-Null
        Write-Host "✓ Firewall kuralı oluşturuldu" -ForegroundColor Green
    } catch {
        Write-Host "⚠ Firewall kuralı oluşturulamadı: $_" -ForegroundColor Yellow
        Write-Host "   Lütfen yönetici olarak çalıştırdığınızdan emin olun" -ForegroundColor Yellow
    }
}

# Test
Write-Host ""
Write-Host "[4/4] Port 1433 test ediliyor..." -ForegroundColor Yellow
$testResult = Test-NetConnection -ComputerName localhost -Port 1433 -WarningAction SilentlyContinue
if ($testResult.TcpTestSucceeded) {
    Write-Host "✓ Port 1433 başarıyla erişilebilir!" -ForegroundColor Green
} else {
    Write-Host "✗ Port 1433 erişilemiyor" -ForegroundColor Red
    Write-Host ""
    Write-Host "========================================" -ForegroundColor Red
    Write-Host "MANUEL ADIMLAR GEREKLİ:" -ForegroundColor Red
    Write-Host "========================================" -ForegroundColor Red
    Write-Host ""
    Write-Host "1. SQL Server Configuration Manager'ı açın (Windows tuşu > 'SQL Server Configuration Manager')" -ForegroundColor White
    Write-Host ""
    Write-Host "2. Sol panel: SQL Server Network Configuration > Protocols for MSSQLSERVER" -ForegroundColor White
    Write-Host ""
    Write-Host "3. Sağ panel: 'TCP/IP' öğesine sağ tıklayın > 'Enable'" -ForegroundColor White
    Write-Host ""
    Write-Host "4. 'TCP/IP' öğesine çift tıklayın > 'IP Addresses' sekmesi" -ForegroundColor White
    Write-Host ""
    Write-Host "5. En alta kaydırın > 'IPAll' bölümü:" -ForegroundColor White
    Write-Host "   - 'TCP Dynamic Ports' alanını BOŞALTIN" -ForegroundColor Gray
    Write-Host "   - 'TCP Port' alanına '1433' yazın" -ForegroundColor Gray
    Write-Host "   - 'OK' butonuna tıklayın" -ForegroundColor Gray
    Write-Host ""
    Write-Host "6. SQL Server servisini yeniden başlatın:" -ForegroundColor White
    Write-Host "   Restart-Service -Name 'MSSQLSERVER'" -ForegroundColor Cyan
    Write-Host ""
}

Write-Host ""
Write-Host "Script tamamlandı." -ForegroundColor Cyan
