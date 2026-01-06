# Kafka Connection Test Script (PowerShell)
# This script tests if Kafka is accessible on localhost:19092

Write-Host "==========================================" -ForegroundColor Cyan
Write-Host "Kafka Connection Test" -ForegroundColor Cyan
Write-Host "==========================================" -ForegroundColor Cyan
Write-Host ""

# Test 1: Check if port 19092 is listening
Write-Host "Test 1: Checking if port 19092 is listening..." -ForegroundColor Yellow
$portTest = Test-NetConnection -ComputerName localhost -Port 19092 -WarningAction SilentlyContinue
if ($portTest.TcpTestSucceeded) {
    Write-Host "✅ Port 19092 is accessible" -ForegroundColor Green
} else {
    Write-Host "❌ Port 19092 is NOT accessible" -ForegroundColor Red
    Write-Host "   Make sure Kafka is running: docker-compose -f docker-compose-kafka.yml up -d" -ForegroundColor Yellow
    exit 1
}
Write-Host ""

# Test 2: Check Docker container status
Write-Host "Test 2: Checking Kafka Docker container status..." -ForegroundColor Yellow
$kafkaContainer = docker ps --filter "name=kafka" --format "{{.Names}}"
if ($kafkaContainer -eq "kafka") {
    Write-Host "✅ Kafka container is running" -ForegroundColor Green
    docker ps --filter "name=kafka"
} else {
    Write-Host "❌ Kafka container is NOT running" -ForegroundColor Red
    Write-Host "   Start Kafka: docker-compose -f docker-compose-kafka.yml up -d" -ForegroundColor Yellow
}
Write-Host ""

# Test 3: Check Kafka logs for connection errors
Write-Host "Test 3: Checking Kafka logs for errors..." -ForegroundColor Yellow
$kafkaLogs = docker logs kafka --tail 20 2>&1
if ($kafkaLogs -match "error|exception|failed") {
    Write-Host "⚠️  Found potential issues in Kafka logs:" -ForegroundColor Yellow
    $kafkaLogs | Select-String -Pattern "error|exception|failed" -CaseSensitive:$false
} else {
    Write-Host "✅ No errors found in Kafka logs" -ForegroundColor Green
}
Write-Host ""

# Test 4: Test connection using telnet (if available)
Write-Host "Test 4: Testing TCP connection to localhost:19092..." -ForegroundColor Yellow
try {
    $tcpClient = New-Object System.Net.Sockets.TcpClient
    $connect = $tcpClient.BeginConnect("localhost", 19092, $null, $null)
    $wait = $connect.AsyncWaitHandle.WaitOne(3000, $false)
    if ($wait) {
        $tcpClient.EndConnect($connect)
        Write-Host "✅ TCP connection to localhost:19092 successful" -ForegroundColor Green
        $tcpClient.Close()
    } else {
        Write-Host "❌ TCP connection timeout" -ForegroundColor Red
    }
} catch {
    Write-Host "⚠️  TCP connection test failed: $_" -ForegroundColor Yellow
}
Write-Host ""

Write-Host "==========================================" -ForegroundColor Cyan
Write-Host "Test Complete" -ForegroundColor Cyan
Write-Host "==========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "If all tests pass, your services should be able to connect to Kafka on localhost:19092" -ForegroundColor Green
Write-Host ""

