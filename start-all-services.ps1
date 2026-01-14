# Tüm Servisleri Sırayla Başlatma Scripti
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "RTD Servisleri Baslatma Scripti" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# SQL Server servisini kontrol et ve başlat
Write-Host "[1/9] SQL Server servisi kontrol ediliyor..." -ForegroundColor Yellow
$sqlService = Get-Service -Name "MSSQLSERVER" -ErrorAction SilentlyContinue
if (-not $sqlService) {
    $sqlService = Get-Service | Where-Object { $_.Name -like "*SQL*" -and $_.DisplayName -like "*SQL Server*" } | Select-Object -First 1
}

if ($sqlService) {
    if ($sqlService.Status -ne "Running") {
        Write-Host "SQL Server servisi baslatiliyor..." -ForegroundColor Yellow
        Start-Service -Name $sqlService.Name
        Start-Sleep -Seconds 5
    }
    Write-Host "[OK] SQL Server servisi hazir" -ForegroundColor Green
} else {
    Write-Host "[UYARI] SQL Server servisi bulunamadi!" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "[2/9] 3 saniye bekleniyor (SQL Server'in tamamen baslamasi icin)..." -ForegroundColor Yellow
Start-Sleep -Seconds 3

# Servis dizinleri
$services = @(
    @{ Name = "AuthUser-Service"; Path = "RTD-AuthUser-Service"; Port = 8081 },
    @{ Name = "Account-Service"; Path = "AccountService\AccountService"; Port = 5239 },
    @{ Name = "Portfolio-Service"; Path = "PortfolioService\PortfolioService"; Port = 5242 },
    @{ Name = "Payment-Service"; Path = "PaymentService\PaymentService"; Port = 5231 },
    @{ Name = "MarketData-Service"; Path = "MarketDataService\MarketDataService"; Port = 5275 },
    @{ Name = "StrategyRule-Service"; Path = "StrategyRuleService\Api"; Port = 5184 },
    @{ Name = "Trading-Service"; Path = "tradingService"; Port = 9084 },
    @{ Name = "Gateway"; Path = "RTD-Gateway\Gateway"; Port = 9082 }
)

$serviceIndex = 3
foreach ($service in $services) {
    Write-Host ""
    Write-Host "[$serviceIndex/9] $($service.Name) baslatiliyor..." -ForegroundColor Yellow
    
    $servicePath = Join-Path $PSScriptRoot $service.Path
    
    if (-not (Test-Path $servicePath)) {
        Write-Host "[HATA] Servis dizini bulunamadi: $servicePath" -ForegroundColor Red
        $serviceIndex++
        continue
    }
    
    Set-Location $servicePath
    
    # .NET servisleri için
    if (Test-Path "*.csproj") {
        Write-Host "  dotnet run komutu calistiriliyor..." -ForegroundColor Gray
        Start-Process powershell -ArgumentList "-NoExit", "-Command", "dotnet run" -WindowStyle Minimized
    }
    # Java servisleri için
    elseif (Test-Path "pom.xml") {
        Write-Host "  mvn spring-boot:run komutu calistiriliyor..." -ForegroundColor Gray
        Start-Process powershell -ArgumentList "-NoExit", "-Command", "mvn spring-boot:run" -WindowStyle Minimized
    }
    # Go servisleri için
    elseif (Test-Path "main.go") {
        Write-Host "  go run komutu calistiriliyor..." -ForegroundColor Gray
        Start-Process powershell -ArgumentList "-NoExit", "-Command", "go run ." -WindowStyle Minimized
    }
    else {
        Write-Host "[UYARI] Servis tipi belirlenemedi: $servicePath" -ForegroundColor Yellow
    }
    
    Write-Host "[OK] $($service.Name) baslatildi (Port: $($service.Port))" -ForegroundColor Green
    Write-Host "  2 saniye bekleniyor..." -ForegroundColor Gray
    Start-Sleep -Seconds 2
    
    $serviceIndex++
}

Set-Location $PSScriptRoot

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "[TAMAMLANDI] Tum servisler baslatildi!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Servislerin durumunu kontrol etmek icin:" -ForegroundColor Yellow
Write-Host "  - AuthUser-Service: http://localhost:8081" -ForegroundColor Gray
Write-Host "  - Account-Service: http://localhost:5239" -ForegroundColor Gray
Write-Host "  - Portfolio-Service: http://localhost:5242" -ForegroundColor Gray
Write-Host "  - Payment-Service: http://localhost:5231" -ForegroundColor Gray
Write-Host "  - MarketData-Service: http://localhost:5275" -ForegroundColor Gray
Write-Host "  - StrategyRule-Service: http://localhost:5184" -ForegroundColor Gray
Write-Host "  - Trading-Service: http://localhost:9084" -ForegroundColor Gray
Write-Host "  - Gateway: http://localhost:9082" -ForegroundColor Gray
Write-Host ""

