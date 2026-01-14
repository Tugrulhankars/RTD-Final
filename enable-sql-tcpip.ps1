# SQL Server TCP/IP Protokolünü Etkinleştirme Scripti
# Bu script SQL Server'da TCP/IP protokolünü açar ve port 1433'ü yapılandırır

Write-Host "=== SQL Server TCP/IP Protokolünü Etkinleştirme ===" -ForegroundColor Cyan
Write-Host ""

# Yönetici yetkisi kontrolü
$isAdmin = ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
if (-not $isAdmin) {
    Write-Host "UYARI: Bu script yönetici yetkisi gerektirir!" -ForegroundColor Red
    Write-Host "PowerShell'i 'Yönetici olarak çalıştır' ile açın ve tekrar deneyin." -ForegroundColor Yellow
    exit 1
}

# SQL Server versiyonunu bul
$sqlVersion = "16"  # SQL Server 2022 için
$foundVersion = $false

for ($i = 16; $i -ge 10; $i--) {
    $namespace = "root\Microsoft\SqlServer\ComputerManagement$i"
    try {
        $test = Get-WmiObject -Namespace $namespace -Class ServerNetworkProtocol -ErrorAction Stop
        $sqlVersion = $i.ToString()
        $foundVersion = $true
        Write-Host "SQL Server Management WMI namespace bulundu: ComputerManagement$i" -ForegroundColor Green
        break
    } catch {
        continue
    }
}

if (-not $foundVersion) {
    Write-Host "HATA: SQL Server WMI namespace bulunamadı!" -ForegroundColor Red
    Write-Host "Lütfen SQL Server Configuration Manager'ı manuel olarak kullanın." -ForegroundColor Yellow
    exit 1
}

$namespace = "root\Microsoft\SqlServer\ComputerManagement$sqlVersion"
$instanceName = "MSSQLSERVER"

Write-Host "Instance: $instanceName" -ForegroundColor Cyan
Write-Host "Namespace: $namespace" -ForegroundColor Cyan
Write-Host ""

# TCP/IP protokolünü etkinleştir
Write-Host "1. TCP/IP protokolünü etkinleştiriliyor..." -ForegroundColor Yellow
try {
    $tcpProtocol = Get-WmiObject -Namespace $namespace -Class ServerNetworkProtocol -Filter "InstanceName = '$instanceName' AND ProtocolName = 'Tcp'"
    if ($tcpProtocol) {
        if ($tcpProtocol.IsEnabled) {
            Write-Host "   TCP/IP zaten etkin." -ForegroundColor Green
        } else {
            $tcpProtocol.SetEnable()
            Write-Host "   TCP/IP protokolü etkinleştirildi." -ForegroundColor Green
        }
    } else {
        Write-Host "   HATA: TCP/IP protokolü bulunamadı!" -ForegroundColor Red
        exit 1
    }
} catch {
    Write-Host "   HATA: TCP/IP protokolü etkinleştirilemedi: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "   Lütfen SQL Server Configuration Manager'ı manuel olarak kullanın." -ForegroundColor Yellow
    exit 1
}

Write-Host ""

# TCP Port 1433'ü ayarla
Write-Host "2. TCP Port 1433 ayarlanıyor..." -ForegroundColor Yellow
try {
    $tcpPortProperty = Get-WmiObject -Namespace $namespace -Class ServerNetworkProtocolProperty -Filter "InstanceName = '$instanceName' AND ProtocolName = 'Tcp' AND IPAddressName = 'IPAll' AND PropertyName = 'TcpPort'"
    if ($tcpPortProperty) {
        $currentPort = $tcpPortProperty.PropertyStrValue
        if ($currentPort -eq "1433") {
            Write-Host "   TCP Port zaten 1433 olarak ayarlı." -ForegroundColor Green
        } else {
            $tcpPortProperty.SetStringValue("1433")
            Write-Host "   TCP Port 1433 olarak ayarlandı (önceki değer: $currentPort)." -ForegroundColor Green
        }
    } else {
        Write-Host "   UYARI: TCP Port özelliği bulunamadı, manuel olarak ayarlanması gerekebilir." -ForegroundColor Yellow
    }
} catch {
    Write-Host "   UYARI: TCP Port ayarlanamadı: $($_.Exception.Message)" -ForegroundColor Yellow
    Write-Host "   Lütfen SQL Server Configuration Manager'da manuel olarak ayarlayın:" -ForegroundColor Yellow
    Write-Host "   - TCP/IP Properties > IP Addresses > IPAll > TCP Port: 1433" -ForegroundColor Cyan
}

Write-Host ""

# Dynamic Port'u temizle
Write-Host "3. Dynamic Port temizleniyor..." -ForegroundColor Yellow
try {
    $dynamicPortProperty = Get-WmiObject -Namespace $namespace -Class ServerNetworkProtocolProperty -Filter "InstanceName = '$instanceName' AND ProtocolName = 'Tcp' AND IPAddressName = 'IPAll' AND PropertyName = 'TcpDynamicPorts'"
    if ($dynamicPortProperty) {
        $currentDynamicPort = $dynamicPortProperty.PropertyStrValue
        if ([string]::IsNullOrEmpty($currentDynamicPort)) {
            Write-Host "   Dynamic Port zaten boş." -ForegroundColor Green
        } else {
            $dynamicPortProperty.SetStringValue("")
            Write-Host "   Dynamic Port temizlendi (önceki değer: $currentDynamicPort)." -ForegroundColor Green
        }
    }
} catch {
    Write-Host "   UYARI: Dynamic Port temizlenemedi: $($_.Exception.Message)" -ForegroundColor Yellow
}

Write-Host ""

# SQL Server servisini yeniden başlat
Write-Host "4. SQL Server servisi yeniden başlatılıyor..." -ForegroundColor Yellow
try {
    $service = Get-Service -Name "MSSQLSERVER" -ErrorAction Stop
    if ($service.Status -eq 'Running') {
        Write-Host "   Servis durduruluyor..." -ForegroundColor Cyan
        Stop-Service -Name "MSSQLSERVER" -Force -ErrorAction Stop
        Start-Sleep -Seconds 3
    }
    Write-Host "   Servis başlatılıyor..." -ForegroundColor Cyan
    Start-Service -Name "MSSQLSERVER" -ErrorAction Stop
    Start-Sleep -Seconds 5
    Write-Host "   SQL Server servisi başarıyla yeniden başlatıldı." -ForegroundColor Green
} catch {
    Write-Host "   UYARI: SQL Server servisi otomatik olarak yeniden başlatılamadı: $($_.Exception.Message)" -ForegroundColor Yellow
    Write-Host "   Lütfen SQL Server servisini manuel olarak yeniden başlatın:" -ForegroundColor Yellow
    Write-Host "   - Services.msc > SQL Server (MSSQLSERVER) > Restart" -ForegroundColor Cyan
    Write-Host "   - Veya: Restart-Service -Name 'MSSQLSERVER'" -ForegroundColor Cyan
}

Write-Host ""

# Bağlantı testi
Write-Host "5. Bağlantı testi yapılıyor..." -ForegroundColor Yellow
Start-Sleep -Seconds 3
$testConnection = Test-NetConnection -ComputerName localhost -Port 1433 -WarningAction SilentlyContinue
if ($testConnection.TcpTestSucceeded) {
    Write-Host "   Başarılı! Port 1433 erişilebilir." -ForegroundColor Green
} else {
    Write-Host "   UYARI: Port 1433 hala erişilemiyor." -ForegroundColor Yellow
    Write-Host "   Lütfen şunları kontrol edin:" -ForegroundColor Yellow
    Write-Host "   1. SQL Server Configuration Manager'da TCP/IP'nin etkin olduğunu doğrulayın" -ForegroundColor Cyan
    Write-Host "   2. SQL Server servisinin çalıştığını kontrol edin" -ForegroundColor Cyan
    Write-Host "   3. Windows Firewall'da port 1433'ün açık olduğunu kontrol edin" -ForegroundColor Cyan
}

Write-Host ""
Write-Host "=== İşlem Tamamlandı ===" -ForegroundColor Cyan
Write-Host ""
Write-Host "Önemli: Değişikliklerin etkili olması için SQL Server servisinin yeniden başlatılması gerekir." -ForegroundColor Yellow
Write-Host "Eğer servis otomatik olarak yeniden başlatılamadıysa, lütfen manuel olarak yeniden başlatın." -ForegroundColor Yellow

