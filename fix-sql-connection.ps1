# SQL Server TCP/IP Kontrol Scripti
Write-Host "SQL Server TCP/IP Kontrol Scripti" -ForegroundColor Cyan
Write-Host ""

# SQL Server servisi kontrol
$sqlService = Get-Service -Name "MSSQLSERVER" -ErrorAction SilentlyContinue
if ($sqlService -and $sqlService.Status -eq "Running") {
    Write-Host "[OK] SQL Server servisi calisiyor" -ForegroundColor Green
} else {
    Write-Host "[HATA] SQL Server servisi calismiyor!" -ForegroundColor Red
    exit 1
}

# Port 1433 kontrol
Write-Host ""
Write-Host "Port 1433 kontrol ediliyor..." -ForegroundColor Yellow
$testResult = Test-NetConnection -ComputerName localhost -Port 1433 -WarningAction SilentlyContinue
if ($testResult.TcpTestSucceeded) {
    Write-Host "[OK] Port 1433 erisilebilir" -ForegroundColor Green
} else {
    Write-Host "[HATA] Port 1433 erisilemiyor!" -ForegroundColor Red
    Write-Host ""
    Write-Host "MANUEL ADIMLAR:" -ForegroundColor Yellow
    Write-Host "1. SQL Server Configuration Manager acin" -ForegroundColor White
    Write-Host "2. SQL Server Network Configuration > Protocols for MSSQLSERVER" -ForegroundColor White
    Write-Host "3. TCP/IP'yi Enable yapin" -ForegroundColor White
    Write-Host "4. TCP/IP Properties > IP Addresses > IPAll" -ForegroundColor White
    Write-Host "5. TCP Dynamic Ports'i bosaltin, TCP Port'a 1433 yazin" -ForegroundColor White
    Write-Host "6. SQL Server servisini yeniden baslatin:" -ForegroundColor White
    Write-Host "   Restart-Service -Name MSSQLSERVER" -ForegroundColor Cyan
}

# Firewall kontrol
Write-Host ""
Write-Host "Firewall kontrol ediliyor..." -ForegroundColor Yellow
$firewallRule = Get-NetFirewallRule -DisplayName "SQL Server" -ErrorAction SilentlyContinue
if ($firewallRule) {
    Write-Host "[OK] Firewall kurali mevcut" -ForegroundColor Green
} else {
    Write-Host "Firewall kurali olusturuluyor..." -ForegroundColor Yellow
    try {
        New-NetFirewallRule -DisplayName "SQL Server" -Direction Inbound -LocalPort 1433 -Protocol TCP -Action Allow -ErrorAction Stop | Out-Null
        Write-Host "[OK] Firewall kurali olusturuldu" -ForegroundColor Green
    } catch {
        Write-Host "[UYARI] Firewall kurali olusturulamadi - Yonetici olarak calistirin" -ForegroundColor Yellow
    }
}

Write-Host ""
Write-Host "Script tamamlandi." -ForegroundColor Cyan

