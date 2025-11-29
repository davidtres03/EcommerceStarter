-- Migration: Add Auto-Blacklist and Notification Settings Columns
-- Date: 2025-11-27
-- Purpose: Add security threshold and notification configuration fields to SecuritySettings table

-- NOTE: Do not hardcode DB name; use current connection.
-- USE [YourDatabaseName];
GO

-- Add auto-blacklist threshold columns
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('SecuritySettings') AND name = 'AutoPermanentBlacklistEnabled')
BEGIN
    ALTER TABLE SecuritySettings ADD AutoPermanentBlacklistEnabled BIT NOT NULL DEFAULT 0;
    PRINT 'Added AutoPermanentBlacklistEnabled column';
END

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('SecuritySettings') AND name = 'ErrorSpikeThresholdPerMinute')
BEGIN
    ALTER TABLE SecuritySettings ADD ErrorSpikeThresholdPerMinute INT NOT NULL DEFAULT 20;
    PRINT 'Added ErrorSpikeThresholdPerMinute column';
END

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('SecuritySettings') AND name = 'ErrorSpikeConsecutiveMinutes')
BEGIN
    ALTER TABLE SecuritySettings ADD ErrorSpikeConsecutiveMinutes INT NOT NULL DEFAULT 1;
    PRINT 'Added ErrorSpikeConsecutiveMinutes column';
END

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('SecuritySettings') AND name = 'ReblockCountThreshold')
BEGIN
    ALTER TABLE SecuritySettings ADD ReblockCountThreshold INT NOT NULL DEFAULT 3;
    PRINT 'Added ReblockCountThreshold column';
END

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('SecuritySettings') AND name = 'ReblockWindowHours')
BEGIN
    ALTER TABLE SecuritySettings ADD ReblockWindowHours INT NOT NULL DEFAULT 24;
    PRINT 'Added ReblockWindowHours column';
END

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('SecuritySettings') AND name = 'FailedLoginBurstThreshold')
BEGIN
    ALTER TABLE SecuritySettings ADD FailedLoginBurstThreshold INT NOT NULL DEFAULT 10;
    PRINT 'Added FailedLoginBurstThreshold column';
END

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('SecuritySettings') AND name = 'FailedLoginBurstWindowMinutes')
BEGIN
    ALTER TABLE SecuritySettings ADD FailedLoginBurstWindowMinutes INT NOT NULL DEFAULT 5;
    PRINT 'Added FailedLoginBurstWindowMinutes column';
END

-- Add notification columns
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('SecuritySettings') AND name = 'NotifyOnCriticalEvents')
BEGIN
    ALTER TABLE SecuritySettings ADD NotifyOnCriticalEvents BIT NOT NULL DEFAULT 0;
    PRINT 'Added NotifyOnCriticalEvents column';
END

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('SecuritySettings') AND name = 'NotifyOnIpBlocking')
BEGIN
    ALTER TABLE SecuritySettings ADD NotifyOnIpBlocking BIT NOT NULL DEFAULT 0;
    PRINT 'Added NotifyOnIpBlocking column';
END

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('SecuritySettings') AND name = 'NotificationEmail')
BEGIN
    ALTER TABLE SecuritySettings ADD NotificationEmail NVARCHAR(500) NULL;
    PRINT 'Added NotificationEmail column';
END

-- Verify all columns exist
PRINT '';
PRINT 'Verification - All Security Settings columns:';
SELECT COLUMN_NAME, DATA_TYPE, IS_NULLABLE, COLUMN_DEFAULT
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'SecuritySettings'
ORDER BY ORDINAL_POSITION;

PRINT '';
PRINT 'Migration completed successfully!';
GO
