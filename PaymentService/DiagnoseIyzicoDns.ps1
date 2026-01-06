# Iyzico DNS ve Bağlantı Tanılama Scripti
# Bu script Iyzico API'sine bağlantı sorunlarını tanılamak için kullanılır

param(
    [string]$Hostname = "sandbox-api.iyzipay.com",
    [string]$ExpectedIp = "213.226.118.91",
    [int]$Port = 443
)

Write-Host "================================================" -ForegroundColor Cyan
Write-Host "Iyzico DNS ve Bağlantı Tanılama Scripti" -ForegroundColor Cyan
Write-Host "================================================" -ForegroundColor Cyan
Write-Host ""

# 1. DNS Çözümleme Testi
Write-Host "1. DNS Çözümleme Testi (Resolve-DnsName)" -ForegroundColor Yellow
Write-Host "   Hostname: $Hostname" -ForegroundColor Gray
Write-Host ""

try {
    $dnsResult = Resolve-DnsName -Name $Hostname -ErrorAction Stop
    Write-Host "   [BAŞARILI] DNS çözümleme başarılı!" -ForegroundColor Green
    
    if ($dnsResult) {
        Write-Host "   Bulunan IP Adresleri:" -ForegroundColor White
        $ipAddresses = @()
        foreach ($record in $dnsResult) {
            if ($record.IPAddress) {
                $ipAddresses += $record.IPAddress
                Write-Host "   - $($record.IPAddress) (Type: $($record.Type), TTL: $($record.TTL))" -ForegroundColor Gray
            }
        }
        
        if ($ExpectedIp -and $ipAddresses -contains $ExpectedIp) {
            Write-Host "   [BULUNDU] Beklenen IP adresi ($ExpectedIp) DNS sonuçlarında bulundu!" -ForegroundColor Green
        } elseif ($ExpectedIp) {
            Write-Host "   [UYARI] Beklenen IP adresi ($ExpectedIp) DNS sonuçlarında bulunamadı!" -ForegroundColor Yellow
        }
    }
}
catch {
    Write-Host "   [HATA] DNS çözümleme başarısız!" -ForegroundColor Red
    Write-Host "   Hata: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "   Hata Türü: $($_.Exception.GetType().Name)" -ForegroundColor Red
}

Write-Host ""

# 2. DNS Cache Temizleme
Write-Host "2. DNS Cache Temizleme" -ForegroundColor Yellow
Write-Host ""

try {
    $dnsCacheClear = ipconfig /flushdns 2>&1
    Write-Host "   [BAŞARILI] DNS cache temizlendi!" -ForegroundColor Green
    Write-Host "   Çıktı: $dnsCacheClear" -ForegroundColor Gray
}
catch {
    Write-Host "   [UYARI] DNS cache temizlenirken hata oluştu: $($_.Exception.Message)" -ForegroundColor Yellow
}

Write-Host ""

# 3. Tekrar DNS Çözümleme (Cache temizlendikten sonra)
Write-Host "3. DNS Çözümleme Testi (Cache Temizlendikten Sonra)" -ForegroundColor Yellow
Write-Host ""

try {
    Start-Sleep -Seconds 2  # DNS cache temizlendikten sonra kısa bir bekleme
    $dnsResultAfterFlush = Resolve-DnsName -Name $Hostname -ErrorAction Stop
    Write-Host "   [BAŞARILI] DNS çözümleme başarılı (cache temizlendikten sonra)!" -ForegroundColor Green
    
    if ($dnsResultAfterFlush) {
        foreach ($record in $dnsResultAfterFlush) {
            if ($record.IPAddress) {
                Write-Host "   - $($record.IPAddress)" -ForegroundColor Gray
            }
        }
    }
}
catch {
    Write-Host "   [HATA] DNS çözümleme başarısız (cache temizlendikten sonra)!" -ForegroundColor Red
    Write-Host "   Hata: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host ""

# 4. Port 443 Bağlantı Testi (IP Adresi ile)
Write-Host "4. Port $Port Bağlantı Testi (IP Adresi: $ExpectedIp)" -ForegroundColor Yellow
Write-Host ""

try {
    $tcpClient = New-Object System.Net.Sockets.TcpClient
    $connection = $tcpClient.BeginConnect($ExpectedIp, $Port, $null, $null)
    $wait = $connection.AsyncWaitHandle.WaitOne(5000, $false)
    
    if ($wait) {
        try {
            $tcpClient.EndConnect($connection)
            Write-Host "   [BAŞARILI] Port $Port'ye bağlantı başarılı! ($ExpectedIp:$Port)" -ForegroundColor Green
            $tcpClient.Close()
        }
        catch {
            Write-Host "   [HATA] Bağlantı kurulamadı: $($_.Exception.Message)" -ForegroundColor Red
        }
    }
    else {
        Write-Host "   [HATA] Bağlantı zaman aşımına uğradı (5 saniye)" -ForegroundColor Red
        $tcpClient.Close()
    }
}
catch {
    Write-Host "   [HATA] Port $Port'ye bağlantı testi başarısız: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "   Hata Türü: $($_.Exception.GetType().Name)" -ForegroundColor Red
}

Write-Host ""

# 5. Port 443 Bağlantı Testi (Hostname ile)
Write-Host "5. Port $Port Bağlantı Testi (Hostname: $Hostname)" -ForegroundColor Yellow
Write-Host ""

try {
    $tcpClient2 = New-Object System.Net.Sockets.TcpClient
    $connection2 = $tcpClient2.BeginConnect($Hostname, $Port, $null, $null)
    $wait2 = $connection2.AsyncWaitHandle.WaitOne(5000, $false)
    
    if ($wait2) {
        try {
            $tcpClient2.EndConnect($connection2)
            Write-Host "   [BAŞARILI] Port $Port'ye bağlantı başarılı! ($Hostname:$Port)" -ForegroundColor Green
            $tcpClient2.Close()
        }
        catch {
            Write-Host "   [HATA] Bağlantı kurulamadı: $($_.Exception.Message)" -ForegroundColor Red
            Write-Host "   Bu, DNS çözümleme veya ağ bağlantısı sorununu gösterir." -ForegroundColor Yellow
        }
    }
    else {
        Write-Host "   [HATA] Bağlantı zaman aşımına uğradı (5 saniye)" -ForegroundColor Red
        $tcpClient2.Close()
    }
}
catch {
    Write-Host "   [HATA] Port $Port'ye bağlantı testi başarısız: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "   Hata Türü: $($_.Exception.GetType().Name)" -ForegroundColor Red
    if ($_.Exception.Message -like "*HostNotFound*" -or $_.Exception.Message -like "*11001*") {
        Write-Host "   [TESPİT] Bu, DNS çözümleme hatasıdır (HostNotFound / ErrorCode: 11001)" -ForegroundColor Yellow
    }
}

Write-Host ""

# 6. DNS Sunucu Bilgileri
Write-Host "6. DNS Sunucu Bilgileri" -ForegroundColor Yellow
Write-Host ""

try {
    $dnsServers = Get-DnsClientServerAddress | Where-Object { $_.AddressFamily -eq 2 } | Select-Object -ExpandProperty ServerAddresses
    if ($dnsServers) {
        Write-Host "   Kullanılan DNS Sunucuları:" -ForegroundColor White
        foreach ($server in $dnsServers) {
            Write-Host "   - $server" -ForegroundColor Gray
        }
        
        # DNS sunucuları test et
        Write-Host ""
        Write-Host "   DNS Sunucu Testi:" -ForegroundColor White
        foreach ($server in $dnsServers) {
            try {
                $testResult = Resolve-DnsName -Name $Hostname -Server $server -ErrorAction Stop
                Write-Host "   - $server: [BAŞARILI]" -ForegroundColor Green
            }
            catch {
                Write-Host "   - $server: [BAŞARISIZ] - $($_.Exception.Message)" -ForegroundColor Red
            }
        }
    }
}
catch {
    Write-Host "   [UYARI] DNS sunucu bilgileri alınamadı: $($_.Exception.Message)" -ForegroundColor Yellow
}

Write-Host ""

# 7. Özet ve Öneriler
Write-Host "================================================" -ForegroundColor Cyan
Write-Host "Özet ve Öneriler" -ForegroundColor Cyan
Write-Host "================================================" -ForegroundColor Cyan
Write-Host ""

if ($dnsResult -and $dnsResult.Count -gt 0) {
    Write-Host "✓ DNS çözümleme çalışıyor" -ForegroundColor Green
}
else {
    Write-Host "✗ DNS çözümleme çalışmıyor" -ForegroundColor Red
    Write-Host "  Öneri: DNS sunucunuzu değiştirmeyi deneyin (Google DNS: 8.8.8.8, 8.8.4.4)" -ForegroundColor Yellow
    Write-Host "  Öneri: VPN kullanıyorsanız kapatın ve tekrar deneyin" -ForegroundColor Yellow
    Write-Host "  Öneri: Firewall/antivirus yazılımını kontrol edin" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "Uygulama ayarları için:" -ForegroundColor White
Write-Host "  appsettings.json'da 'Iyzico:UseHardcodedIp: true' ayarını aktif edebilirsiniz" -ForegroundColor Gray
Write-Host "  Bu, DNS sorunlarını bypass etmek için geçici bir çözümdür (sadece tanılama amaçlı)" -ForegroundColor Gray
Write-Host ""

