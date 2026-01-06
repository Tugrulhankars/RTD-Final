# System Fix Instructions - Critical Infrastructure Errors

This document provides step-by-step instructions to fix the three critical infrastructure errors.

---

## 1. SQL Server Authentication Failure (Error 4060)

### Problem
`Cannot open database "PaymentServiceDb" requested by the login. Login failed for user 'MetropolTilkisi\karsl'.`

### Solution Steps

#### Step 1: Run the SQL Script

1. Open **SQL Server Management Studio (SSMS)** or **Azure Data Studio**
2. Connect to your SQL Server instance as a user with `sysadmin` or `securityadmin` privileges
3. Open the file: `scripts/create-payment-service-database.sql`
4. Execute the entire script
5. Verify the output messages show successful creation and permission assignment

#### Step 2: Verify Connection String

Check your `PaymentService/PaymentService/appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=PaymentServiceDb;Trusted_Connection=True;TrustServerCertificate=True;Max Pool Size=100;Connection Timeout=60;"
  }
}
```

**Important checks:**
- Ensure there are **no hidden spaces** or special characters in the database name
- Verify `Server=localhost` matches your SQL Server instance (or use `Server=localhost\\SQLEXPRESS` if using SQL Express)
- For named instances, use: `Server=localhost\\INSTANCENAME`
- Ensure `Trusted_Connection=True` for Windows Authentication

#### Step 3: Run Entity Framework Migrations

After creating the database, run:

```bash
cd PaymentService/PaymentService
dotnet ef database update
```

This will create the necessary tables.

#### Step 4: Verify Permissions

Run this query in SSMS to verify the user has `db_owner` role:

```sql
USE PaymentServiceDb;
GO

SELECT 
    dp.name AS UserName,
    r.name AS RoleName
FROM sys.database_principals dp
INNER JOIN sys.database_role_members rm ON dp.principal_id = rm.member_principal_id
INNER JOIN sys.database_principals r ON rm.role_principal_id = r.principal_id
WHERE dp.name = 'MetropolTilkisi\karsl';
```

You should see `db_owner` in the RoleName column.

---

## 2. Kafka Topic Availability Issue

### Problem
`Subscribed topic not available: payment-success: Broker: Unknown topic or partition`

### Solution

#### Option A: Automatic Topic Creation (Already Implemented in Code)

The code has been updated to automatically create topics. However, ensure:

1. **Kafka Docker Container Configuration** is correct (already configured):

   In `docker-compose-kafka.yml`, verify:
   ```yaml
   environment:
     KAFKA_AUTO_CREATE_TOPICS_ENABLE: "true"  # This is already set
   ```

2. **Restart Services** to apply changes:

   ```bash
   # Restart Kafka
   docker-compose -f docker-compose-kafka.yml restart kafka
   
   # Restart AccountService
   docker-compose restart account-service
   ```

3. **Verify Topic Creation**:

   After AccountService starts, check logs for:
   ```
   Topic başarıyla oluşturuldu: payment-success
   ```

#### Option B: Manual Topic Creation (Alternative)

If automatic creation still fails, manually create the topic:

```bash
docker exec -it kafka kafka-topics --create \
  --bootstrap-server localhost:9092 \
  --topic payment-success \
  --partitions 1 \
  --replication-factor 1 \
  --config retention.ms=604800000 \
  --config cleanup.policy=delete
```

Verify topic exists:
```bash
docker exec kafka kafka-topics --list --bootstrap-server localhost:9092
```

---

## 3. PaymentController Error Handling

### Problem
The `Callback` method crashes when database connection fails after retries.

### Solution

The code has been updated with comprehensive error handling. The `Callback` method now:

1. Catches `RetryLimitExceededException` from EF Core
2. Catches `InvalidOperationException` from PaymentRepository
3. Catches `DbUpdateException` for database update errors
4. Returns meaningful error messages to users
5. Logs detailed SQL exception information internally

**No additional action required** - the code changes are already in place.

---

## Testing Checklist

After applying all fixes:

### SQL Server
- [ ] Database `PaymentServiceDb` exists
- [ ] User `MetropolTilkisi\karsl` has `db_owner` role
- [ ] Entity Framework migrations ran successfully
- [ ] PaymentService can connect to database (check logs)

### Kafka
- [ ] Kafka container is running: `docker ps | grep kafka`
- [ ] Topic `payment-success` exists: `docker exec kafka kafka-topics --list --bootstrap-server localhost:9092`
- [ ] AccountService logs show: "Topic başarıyla oluşturuldu" or "Topic zaten mevcut"
- [ ] AccountService can consume from `payment-success` topic

### Error Handling
- [ ] PaymentController returns meaningful errors instead of crashing
- [ ] SQL exception details are logged (not exposed to users)
- [ ] Users receive user-friendly error messages

---

## Troubleshooting

### If SQL Server connection still fails:

1. Check SQL Server is running:
   ```powershell
   Get-Service MSSQLSERVER
   ```

2. Test connection manually:
   ```powershell
   sqlcmd -S localhost -E -Q "SELECT @@VERSION"
   ```

3. Verify Windows user exists:
   ```sql
   SELECT name, type_desc FROM sys.server_principals WHERE type = 'U';
   ```

### If Kafka topic still not found:

1. Check Kafka is accessible:
   ```bash
   docker exec kafka kafka-broker-api-versions --bootstrap-server localhost:9092
   ```

2. Check AccountService logs for topic creation errors

3. Manually create the topic (see Option B above)

---

## Summary of Code Changes

1. ✅ **SQL Script**: Enhanced with robust error handling and permission checks
2. ✅ **KafkaConsumerService**: Added `EnsureTopicExistsAsync` method that creates topics automatically
3. ✅ **PaymentController**: Added comprehensive error handling for database connection failures

All changes are backward compatible and include detailed logging for troubleshooting.

