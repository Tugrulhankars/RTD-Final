# SQL Server Servislerini Kontrol Etme Scripti
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "SQL Server Servis Kontrol Scripti" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# SQL Server servislerini bul
$sqlServices = Get-Service | Where-Object { $_.Name -like "*SQL*" -or $_.DisplayName -like "*SQL*" }

if ($sqlServices.Count -eq 0) {
    Write-Host "[HATA] SQL Server servisi bulunamadı!" -ForegroundColor Red
    Write-Host "Lutfen SQL Server'in yuklu oldugundan emin olun." -ForegroundColor Yellow
    exit 1
}

Write-Host "Bulunan SQL Server servisleri:" -ForegroundColor Yellow
Write-Host ""

$mssqlserver = $null
$sqlexpress = $null

foreach ($service in $sqlServices) {
    $statusColor = if ($service.Status -eq "Running") { "Green" } else { "Red" }
    Write-Host "  - $($service.DisplayName)" -ForegroundColor White
    Write-Host "    Name: $($service.Name)" -ForegroundColor Gray
    Write-Host "    Status: $($service.Status)" -ForegroundColor $statusColor
    Write-Host ""
    
    if ($service.Name -eq "MSSQLSERVER") {
        $mssqlserver = $service
    }
    if ($service.Name -like "*SQLEXPRESS*") {
        $sqlexpress = $service
    }
}

# Ana SQL Server servisini kontrol et
$mainService = $mssqlserver
if (-not $mainService) {
    $mainService = $sqlexpress
}

if ($mainService) {
    Write-Host "Ana SQL Server Servisi: $($mainService.DisplayName)" -ForegroundColor Cyan
    Write-Host ""
    
    if ($mainService.Status -ne "Running") {
        Write-Host "[UYARI] SQL Server servisi calismiyor!" -ForegroundColor Yellow
        Write-Host "Servisi baslatmak ister misiniz? (E/H)" -ForegroundColor Yellow
        $response = Read-Host
        
        if ($response -eq "E" -or $response -eq "e") {
            try {
                Start-Service -Name $mainService.Name
                Write-Host "[OK] SQL Server servisi baslatildi" -ForegroundColor Green
                Start-Sleep -Seconds 3
            } catch {
                Write-Host "[HATA] Servis baslatilamadi: $_" -ForegroundColor Red
                Write-Host "Lutfen yonetici olarak calistirdiginizdan emin olun." -ForegroundColor Yellow
            }
        }
    } else {
        Write-Host "[OK] SQL Server servisi calisiyor" -ForegroundColor Green
    }
} else {
    Write-Host "[UYARI] Ana SQL Server servisi bulunamadi" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Script tamamlandi." -ForegroundColor Cyan

