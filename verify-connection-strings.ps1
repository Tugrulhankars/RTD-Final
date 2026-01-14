# Connection String Kontrol Scripti
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Connection String Kontrol Scripti" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Kontrol edilecek dosyalar
$configFiles = @(
    @{ Path = "AccountService\AccountService\appsettings.json"; Service = "AccountService" },
    @{ Path = "PortfolioService\PortfolioService\appsettings.json"; Service = "PortfolioService" },
    @{ Path = "PaymentService\PaymentService\appsettings.json"; Service = "PaymentService" },
    @{ Path = "StrategyRuleService\Api\appsettings.json"; Service = "StrategyRuleService" }
)

$issues = @()

foreach ($config in $configFiles) {
    $filePath = Join-Path $PSScriptRoot $config.Path
    
    if (-not (Test-Path $filePath)) {
        Write-Host "[UYARI] Dosya bulunamadi: $($config.Path)" -ForegroundColor Yellow
        continue
    }
    
    Write-Host "Kontrol ediliyor: $($config.Service)" -ForegroundColor Yellow
    
    try {
        $json = Get-Content $filePath -Raw | ConvertFrom-Json
        
        if ($json.ConnectionStrings.DefaultConnection) {
            $connString = $json.ConnectionStrings.DefaultConnection
            Write-Host "  Connection String: $connString" -ForegroundColor Gray
            
            # Kontroller
            $warnings = @()
            
            # Server kontrolü
            if ($connString -match "Server=([^;]+)") {
                $server = $matches[1]
                if ($server -notmatch "localhost|127\.0\.0\.1|\.\\SQLEXPRESS|MetropolTilkisi") {
                    $warnings += "Server adi beklenmedik: $server (localhost, 127.0.0.1, .\SQLEXPRESS veya MetropolTilkisi olmali)"
                }
            }
            
            # Database kontrolü
            if ($connString -match "Database=([^;]+)") {
                $database = $matches[1]
                Write-Host "  Database: $database" -ForegroundColor Gray
            }
            
            # Authentication kontrolü
            if ($connString -match "Integrated Security|Trusted_Connection") {
                Write-Host "  [OK] Windows Authentication kullaniliyor" -ForegroundColor Green
            } elseif ($connString -match "User Id|User ID") {
                Write-Host "  [OK] SQL Authentication kullaniliyor" -ForegroundColor Green
            } else {
                $warnings += "Authentication tipi belirtilmemis"
            }
            
            # TrustServerCertificate kontrolü
            if ($connString -notmatch "TrustServerCertificate=True") {
                $warnings += "TrustServerCertificate=True eklenmeli (geliştirme ortamı için)"
            }
            
            if ($warnings.Count -gt 0) {
                Write-Host "  [UYARI] Sorunlar bulundu:" -ForegroundColor Yellow
                foreach ($warning in $warnings) {
                    Write-Host "    - $warning" -ForegroundColor Yellow
                }
                $issues += @{ Service = $config.Service; Warnings = $warnings }
            } else {
                Write-Host "  [OK] Connection string dogru gorunuyor" -ForegroundColor Green
            }
        } else {
            Write-Host "  [HATA] DefaultConnection bulunamadi!" -ForegroundColor Red
            $issues += @{ Service = $config.Service; Warnings = @("DefaultConnection bulunamadi") }
        }
    } catch {
        Write-Host "  [HATA] Dosya okunamadi: $_" -ForegroundColor Red
        $issues += @{ Service = $config.Service; Warnings = @("Dosya okunamadi: $_") }
    }
    
    Write-Host ""
}

Write-Host "========================================" -ForegroundColor Cyan
if ($issues.Count -eq 0) {
    Write-Host "[TAMAMLANDI] Tum connection string'ler dogru!" -ForegroundColor Green
} else {
    Write-Host "[UYARI] $($issues.Count) serviste sorun bulundu" -ForegroundColor Yellow
    Write-Host "Yukaridaki uyarilari kontrol edin." -ForegroundColor Yellow
}
Write-Host "========================================" -ForegroundColor Cyan

