# SQL Server TCP/IP Protokol Kontrol Scripti
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "SQL Server TCP/IP Protokol Kontrol" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

Write-Host "Bu script SQL Server TCP/IP protokolunun acik olup olmadigini kontrol eder." -ForegroundColor Yellow
Write-Host ""
Write-Host "MANUEL KONTROL GEREKLIDIR:" -ForegroundColor Red
Write-Host ""
Write-Host "1. SQL Server Configuration Manager'i acin:" -ForegroundColor White
Write-Host "   - Windows tusuna basin" -ForegroundColor Gray
Write-Host "   - 'SQL Server Configuration Manager' yazin ve acin" -ForegroundColor Gray
Write-Host ""
Write-Host "2. Sol panelde:" -ForegroundColor White
Write-Host "   SQL Server Network Configuration > Protocols for MSSQLSERVER" -ForegroundColor Gray
Write-Host ""
Write-Host "3. Sag panelde 'TCP/IP' ogesini bulun:" -ForegroundColor White
Write-Host "   - Eger 'Disabled' ise, sag tiklayin > 'Enable'" -ForegroundColor Gray
Write-Host "   - 'TCP/IP' ogesine cift tiklayin" -ForegroundColor Gray
Write-Host "   - 'IP Addresses' sekmesine gidin" -ForegroundColor Gray
Write-Host "   - En alta kaydirin ve 'IPAll' bolumunu bulun" -ForegroundColor Gray
Write-Host "   - 'TCP Dynamic Ports' alanini BOSALTIN" -ForegroundColor Gray
Write-Host "   - 'TCP Port' alanina '1433' yazin" -ForegroundColor Gray
Write-Host "   - 'OK' butonuna tiklayin" -ForegroundColor Gray
Write-Host ""
Write-Host "4. SQL Server servisini yeniden baslatin:" -ForegroundColor White
Write-Host "   Restart-Service -Name 'MSSQLSERVER'" -ForegroundColor Cyan
Write-Host ""

# Port 1433 kontrolü
Write-Host "Port 1433 kontrol ediliyor..." -ForegroundColor Yellow
$testResult = Test-NetConnection -ComputerName localhost -Port 1433 -WarningAction SilentlyContinue
if ($testResult.TcpTestSucceeded) {
    Write-Host "[OK] Port 1433 erisilebilir - TCP/IP protokolu acik!" -ForegroundColor Green
} else {
    Write-Host "[HATA] Port 1433 erisilemiyor - TCP/IP protokolu kapali!" -ForegroundColor Red
    Write-Host "Yukaridaki manuel adimlari uygulayin." -ForegroundColor Yellow
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan

