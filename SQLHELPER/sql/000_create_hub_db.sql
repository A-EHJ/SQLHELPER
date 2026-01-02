IF DB_ID('SqlMaintenanceHub') IS NULL
BEGIN
    PRINT 'Creating database [SqlMaintenanceHub]';
    CREATE DATABASE [SqlMaintenanceHub];
END
ELSE
BEGIN
    PRINT 'Database [SqlMaintenanceHub] already exists. Skipping creation.';
END
