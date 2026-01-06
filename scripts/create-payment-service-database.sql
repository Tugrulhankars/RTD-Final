-- ============================================================================
-- PaymentServiceDb Veritabanı Oluşturma ve Yetkilendirme Scripti
-- ============================================================================
-- Bu script SQL Server Management Studio (SSMS) veya Azure Data Studio'da çalıştırılabilir
-- SQL Server'da "sysadmin" veya "securityadmin" yetkilerine sahip bir kullanıcı ile çalıştırın
-- ============================================================================

USE [master];
GO

PRINT '============================================================================';
PRINT '1. VERİTABANI OLUŞTURMA';
PRINT '============================================================================';

-- Veritabanının var olup olmadığını kontrol et ve oluştur
IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = 'PaymentServiceDb')
BEGIN
    CREATE DATABASE [PaymentServiceDb]
    ON PRIMARY
    (NAME = 'PaymentServiceDb', 
     FILENAME = 'C:\Program Files\Microsoft SQL Server\MSSQL16.MSSQLSERVER\MSSQL\DATA\PaymentServiceDb.mdf',
     SIZE = 100MB, 
     MAXSIZE = UNLIMITED, 
     FILEGROWTH = 10MB)
    LOG ON
    (NAME = 'PaymentServiceDb_Log', 
     FILENAME = 'C:\Program Files\Microsoft SQL Server\MSSQL16.MSSQLSERVER\MSSQL\DATA\PaymentServiceDb_Log.ldf',
     SIZE = 10MB, 
     MAXSIZE = 1GB, 
     FILEGROWTH = 10%);
    PRINT '✓ PaymentServiceDb veritabanı başarıyla oluşturuldu.';
END
ELSE
BEGIN
    PRINT '✓ PaymentServiceDb veritabanı zaten mevcut.';
END
GO

PRINT '';
PRINT '============================================================================';
PRINT '2. WINDOWS LOGIN OLUŞTURMA';
PRINT '============================================================================';

DECLARE @DbUser NVARCHAR(128) = 'MetropolTilkisi\karsl';
DECLARE @Sql NVARCHAR(MAX);

-- Windows Login kontrolü ve oluşturma
IF NOT EXISTS (SELECT * FROM sys.server_principals WHERE name = @DbUser AND type = 'U')
BEGIN
    SET @Sql = 'CREATE LOGIN [' + @DbUser + '] FROM WINDOWS;';
    EXEC sp_executesql @Sql;
    PRINT '✓ Windows login oluşturuldu: ' + @DbUser;
END
ELSE
BEGIN
    PRINT '✓ Windows login zaten mevcut: ' + @DbUser;
END
GO

PRINT '';
PRINT '============================================================================';
PRINT '3. VERİTABANI KULLANICISI VE YETKİLERİ';
PRINT '============================================================================';

USE [PaymentServiceDb];
GO

DECLARE @DbUser NVARCHAR(128) = 'MetropolTilkisi\karsl';
DECLARE @Sql NVARCHAR(MAX);

-- Database User oluştur
IF NOT EXISTS (SELECT * FROM sys.database_principals WHERE name = @DbUser AND type = 'S')
BEGIN
    SET @Sql = 'CREATE USER [' + @DbUser + '] FOR LOGIN [' + @DbUser + '];';
    EXEC sp_executesql @Sql;
    PRINT '✓ Database user oluşturuldu: ' + @DbUser;
END
ELSE
BEGIN
    PRINT '✓ Database user zaten mevcut: ' + @DbUser;
END
GO

-- db_owner rolü ver (eğer zaten üye değilse)
DECLARE @DbUser NVARCHAR(128) = 'MetropolTilkisi\karsl';
DECLARE @IsDbOwner BIT = 0;

SELECT @IsDbOwner = 1
FROM sys.database_role_members rm
INNER JOIN sys.database_principals r ON rm.role_principal_id = r.principal_id
INNER JOIN sys.database_principals m ON rm.member_principal_id = m.principal_id
WHERE r.name = 'db_owner' AND m.name = @DbUser;

IF @IsDbOwner = 0
BEGIN
    DECLARE @Sql NVARCHAR(MAX) = 'ALTER ROLE db_owner ADD MEMBER [' + @DbUser + '];';
    EXEC sp_executesql @Sql;
    PRINT '✓ db_owner rolü verildi: ' + @DbUser;
END
ELSE
BEGIN
    PRINT '✓ Kullanıcı zaten db_owner rolüne sahip: ' + @DbUser;
END
GO

PRINT '';
PRINT '============================================================================';
PRINT '4. YETKİ KONTROLÜ';
PRINT '============================================================================';

-- Yetkileri göster
SELECT 
    dp.name AS UserName,
    dp.type_desc AS UserType,
    ISNULL(r.name, 'No Role') AS RoleName
FROM sys.database_principals dp
LEFT JOIN sys.database_role_members rm ON dp.principal_id = rm.member_principal_id
LEFT JOIN sys.database_principals r ON rm.role_principal_id = r.principal_id
WHERE dp.name = 'MetropolTilkisi\karsl';
GO

PRINT '';
PRINT '============================================================================';
PRINT 'VERİTABANI YAPILANDIRMASI TAMAMLANDI!';
PRINT '============================================================================';
PRINT '';
PRINT 'SONRAKI ADIMLAR:';
PRINT '1. Entity Framework migration çalıştırın:';
PRINT '   cd PaymentService\PaymentService';
PRINT '   dotnet ef database update';
PRINT '';
PRINT '2. Veritabanı bağlantısını test edin:';
PRINT '   - PaymentService uygulamasını başlatın';
PRINT '   - Bir ödeme isteği gönderin';
PRINT '   - Logları kontrol edin';
PRINT '============================================================================';

