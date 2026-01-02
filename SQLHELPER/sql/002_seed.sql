IF OBJECT_ID('maint.Servers', 'U') IS NOT NULL
BEGIN
    IF NOT EXISTS (SELECT 1 FROM maint.Servers WHERE Name = 'localhost')
    BEGIN
        INSERT INTO maint.Servers (Name, DisplayName)
        VALUES ('localhost', 'Local SQL Server instance');
    END
END
GO

IF OBJECT_ID('maint.Jobs', 'U') IS NOT NULL
BEGIN
    IF NOT EXISTS (SELECT 1 FROM maint.Jobs WHERE Name = 'Sample inventory')
    BEGIN
        INSERT INTO maint.Jobs (Name, Description, ScriptPath)
        VALUES ('Sample inventory', 'Placeholder maintenance job used to validate the hub schema.', 'sql/sample_inventory.sql');
    END
END
GO

IF OBJECT_ID('maint.JobExecutions', 'U') IS NOT NULL AND OBJECT_ID('maint.Jobs', 'U') IS NOT NULL
BEGIN
    DECLARE @jobId INT = (SELECT TOP 1 JobId FROM maint.Jobs WHERE Name = 'Sample inventory');

    IF @jobId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM maint.JobExecutions WHERE JobId = @jobId)
    BEGIN
        INSERT INTO maint.JobExecutions (JobId, CompletedAtUtc, Status, Message)
        VALUES (@jobId, SYSUTCDATETIME(), 'Seeded', 'Initial seeded execution to validate bootstrap.');
    END
END
GO
