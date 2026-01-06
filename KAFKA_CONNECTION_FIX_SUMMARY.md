# Kafka Connection Fix Summary

## ✅ All Configuration Files Updated

### 1. Docker Compose Files

#### `docker-compose-kafka.yml` ✅ (Already Correct)
- Port mapping: `19092:9092`
- `KAFKA_ADVERTISED_LISTENERS: PLAINTEXT://localhost:19092,PLAINTEXT_INTERNAL://kafka:29092`

#### `docker-compose.yml` ✅ (Fixed)
- **Updated**: Port mapping changed from `9092:9092` to `19092:9092`
- **Updated**: `KAFKA_ADVERTISED_LISTENERS` changed from `PLAINTEXT://localhost:9092` to `PLAINTEXT://localhost:19092`

### 2. Spring Boot Services

#### RTD-AuthUser-Service ✅
- **File**: `src/main/resources/application.properties`
- **Config**: `spring.kafka.bootstrap-servers=localhost:19092`
- **Code**: `KafkaProducerConfig.java` default value: `localhost:19092`
- **Resilience**: Added timeout configurations (5s max.block.ms)

#### RTD-Notification-Service ✅
- **Note**: This service does NOT use Kafka (only RabbitMQ)
- No Kafka configuration needed

### 3. .NET Services

All .NET services have been updated to use `localhost:19092`:

#### AccountService ✅
- `appsettings.json`: `"BootstrapServers": "localhost:19092"`
- `appsettings.Development.json`: `"BootstrapServers": "localhost:19092"`
- `bin/Debug/net9.0/appsettings.json`: Updated
- `bin/Debug/net9.0/appsettings.Development.json`: Updated
- **Code**: `KafkaConsumerService.cs` default: `localhost:19092`
- **Resilience**: Non-blocking, timeout-based consumer

#### PortfolioService ✅
- `appsettings.json`: `"BootstrapServers": "localhost:19092"`
- `bin/Debug/net9.0/appsettings.json`: Updated
- **Code**: `Program.cs` ProducerConfig with resilience settings
- **Code**: `KafkaConsumerService.cs` default: `localhost:19092`
- **Resilience**: Error handling in `KafkaProducerService.cs`

#### PaymentService ✅
- `appsettings.json`: `"BootstrapServers": "localhost:19092"`
- `bin/Debug/net9.0/appsettings.json`: Updated

#### MarketDataService ✅
- `appsettings.json`: `"BootstrapServers": "localhost:19092"`
- `bin/Debug/net9.0/appsettings.json`: Updated

#### StrategyRuleService ✅
- `Api/appsettings.json`: `"BootstrapServers": "localhost:19092"`
- `Api/appsettings.Development.json`: `"BootstrapServers": "localhost:19092"`
- `Api/bin/Debug/net9.0/appsettings.json`: Updated
- `Api/bin/Debug/net9.0/appsettings.Development.json`: Updated
- `Worker/appsettings.json`: `"BootstrapServers": "localhost:19092"`

## 🔍 Environment Variables

### Docker Compose Environment Variables
In `docker-compose.yml`, services running **inside Docker** use:
- `SPRING_KAFKA_BOOTSTRAP_SERVERS=kafka:29092` (for Docker-to-Docker communication)
- `Kafka__BootstrapServers=kafka:29092` (for .NET services in Docker)

**Note**: These are correct for services running inside Docker containers. Services running on **localhost** should use `localhost:19092` in their `application.properties` or `appsettings.json` files.

## 🧪 Testing Kafka Connection

### Option 1: PowerShell Script (Windows)
```powershell
.\test-kafka-connection.ps1
```

### Option 2: Bash Script (Linux/Mac/Git Bash)
```bash
chmod +x test-kafka-connection.sh
./test-kafka-connection.sh
```

### Option 3: Manual Testing

#### Test 1: Check if Kafka is running
```bash
docker ps | grep kafka
```

#### Test 2: Check if port 19092 is accessible
```bash
# Windows PowerShell
Test-NetConnection -ComputerName localhost -Port 19092

# Linux/Mac
nc -z localhost 19092
```

#### Test 3: Check Kafka logs
```bash
docker logs kafka --tail 50
```

#### Test 4: Test from Spring Boot Service
Start your Spring Boot service and check logs for:
```
Creating Kafka ProducerFactory with bootstrap servers: localhost:19092
Kafka ProducerFactory created successfully
```

If you see connection errors, verify:
1. Kafka is running: `docker-compose -f docker-compose-kafka.yml up -d`
2. Port 19092 is accessible
3. `application.properties` has `spring.kafka.bootstrap-servers=localhost:19092`

## 📋 Quick Verification Checklist

- [ ] `docker-compose-kafka.yml` uses port `19092:9092`
- [ ] `docker-compose.yml` uses port `19092:9092` (if using this file)
- [ ] `KAFKA_ADVERTISED_LISTENERS` includes `PLAINTEXT://localhost:19092`
- [ ] All `application.properties` files have `spring.kafka.bootstrap-servers=localhost:19092`
- [ ] All `appsettings.json` files have `"BootstrapServers": "localhost:19092"`
- [ ] All `bin/Debug` appsettings files are updated (will be regenerated on build)
- [ ] Kafka container is running: `docker ps | grep kafka`
- [ ] Port 19092 is accessible from host

## 🚀 Next Steps

1. **Clean up and restart Kafka** (if you see Zookeeper NodeExistsException):
   ```bash
   # Stop and remove containers
   docker-compose -f docker-compose-kafka.yml down
   
   # Remove volumes to clear Zookeeper state (if needed)
   docker-compose -f docker-compose-kafka.yml down -v
   
   # Start fresh
   docker-compose -f docker-compose-kafka.yml up -d
   
   # Check logs
   docker logs kafka --tail 50
   ```

2. **If Kafka container exited with error**:
   ```bash
   # Check what went wrong
   docker logs kafka --tail 50
   
   # Common issue: Zookeeper node exists - clean up and restart
   docker-compose -f docker-compose-kafka.yml down -v
   docker-compose -f docker-compose-kafka.yml up -d
   ```

2. **Restart all services** to pick up the new configuration

3. **Run the test script** to verify connection:
   ```powershell
   .\test-kafka-connection.ps1
   ```

4. **Check service logs** for successful Kafka connection:
   - Spring Boot: Look for "Kafka ProducerFactory created successfully"
   - .NET: Look for "Kafka consumer başlatılıyor" or similar

## ⚠️ Important Notes

1. **Docker vs Localhost**:
   - Services running **inside Docker**: Use `kafka:29092`
   - Services running on **localhost**: Use `localhost:19092`

2. **Port Mapping**:
   - Host port: `19092` (what your services connect to)
   - Container port: `9092` (internal Kafka port)
   - Mapping: `19092:9092` means host:19092 → container:9092

3. **KAFKA_ADVERTISED_LISTENERS**:
   - `PLAINTEXT://localhost:19092` - For host applications
   - `PLAINTEXT_INTERNAL://kafka:29092` - For Docker-to-Docker communication

4. **Build Artifacts**:
   - `bin/Debug` files are generated during build
   - Source files (`appsettings.json`) take precedence
   - If issues persist, clean and rebuild the project

