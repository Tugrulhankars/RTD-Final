# Kafka Listener Configuration

## Updated Configuration

### docker-compose-kafka.yml

The Kafka configuration has been updated with the following listener setup:

```yaml
KAFKA_LISTENERS: PLAINTEXT://0.0.0.0:9092,CONNECTIONS_FROM_HOST://0.0.0.0:9092
KAFKA_ADVERTISED_LISTENERS: PLAINTEXT://kafka:9092,CONNECTIONS_FROM_HOST://localhost:19092
KAFKA_LISTENER_SECURITY_PROTOCOL_MAP: PLAINTEXT:PLAINTEXT,CONNECTIONS_FROM_HOST:PLAINTEXT
KAFKA_INTER_BROKER_LISTENER_NAME: PLAINTEXT
```

### Port Mappings

- **19092:9092** - Host port 19092 maps to container port 9092 (for host connections)
- **9092:9092** - Host port 9092 also maps to container port 9092 (for Docker network connections)

### Connection Addresses

#### For Services Running on Host Machine (localhost)
- **Bootstrap Server**: `localhost:19092`
- Uses: `CONNECTIONS_FROM_HOST` listener
- Advertised as: `localhost:19092`

#### For Services Running Inside Docker Network
- **Bootstrap Server**: `kafka:9092`
- Uses: `PLAINTEXT` listener
- Advertised as: `kafka:9092`

## Application Configuration

### Spring Boot Services (Host-side)
- **File**: `application.properties`
- **Config**: `spring.kafka.bootstrap-servers=localhost:19092`

### .NET Services (Host-side)
- **File**: `appsettings.json`
- **Config**: `"BootstrapServers": "localhost:19092"`

### Services Inside Docker (docker-compose.yml)
- **Environment Variable**: `Kafka__BootstrapServers=kafka:9092` (for .NET)
- **Environment Variable**: `SPRING_KAFKA_BOOTSTRAP_SERVERS=kafka:9092` (for Spring Boot)

## Restart Instructions

After updating the configuration:

1. **Stop Kafka**:
   ```bash
   docker-compose -f docker-compose-kafka.yml down
   ```

2. **Start Kafka with new configuration**:
   ```bash
   docker-compose -f docker-compose-kafka.yml up -d
   ```

3. **Verify Kafka is running**:
   ```bash
   docker logs kafka --tail 50
   ```

4. **Check for listener initialization**:
   Look for messages like:
   ```
   [KafkaServer id=1] started (kafka.server.KafkaServer)
   ```

## Troubleshooting

### POLLHUP Errors
- Ensure `KAFKA_ADVERTISED_LISTENERS` includes `CONNECTIONS_FROM_HOST://localhost:19092`
- Verify port 19092 is accessible: `Test-NetConnection -ComputerName localhost -Port 19092`

### APIVERSION_QUERY Failures
- Check that `KAFKA_LISTENER_SECURITY_PROTOCOL_MAP` includes both listeners
- Verify `KAFKA_INTER_BROKER_LISTENER_NAME` is set to `PLAINTEXT`
- Ensure services use the correct bootstrap server address based on their location

### Connection Issues
- **Host services**: Must use `localhost:19092`
- **Docker services**: Must use `kafka:9092`
- Verify network connectivity: `docker network inspect rtd-finalproject_rtd-network`

