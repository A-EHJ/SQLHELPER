IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = 'maint')
BEGIN
    EXEC('CREATE SCHEMA maint');
END
GO

IF OBJECT_ID('maint.Servers', 'U') IS NULL
BEGIN
    CREATE TABLE maint.Servers
    (
        ServerId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Servers PRIMARY KEY,
        Name NVARCHAR(200) NOT NULL,
        DisplayName NVARCHAR(200) NULL,
        CreatedAtUtc DATETIME2 NOT NULL CONSTRAINT DF_Servers_CreatedAtUtc DEFAULT (SYSUTCDATETIME())
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_Servers_Name' AND object_id = OBJECT_ID('maint.Servers'))
BEGIN
    CREATE UNIQUE INDEX UX_Servers_Name ON maint.Servers (Name);
END
GO

IF OBJECT_ID('maint.Jobs', 'U') IS NULL
BEGIN
    CREATE TABLE maint.Jobs
    (
        JobId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Jobs PRIMARY KEY,
        Name NVARCHAR(200) NOT NULL,
        Description NVARCHAR(500) NULL,
        ScriptPath NVARCHAR(260) NULL,
        IsEnabled BIT NOT NULL CONSTRAINT DF_Jobs_IsEnabled DEFAULT(1),
        CreatedAtUtc DATETIME2 NOT NULL CONSTRAINT DF_Jobs_CreatedAtUtc DEFAULT (SYSUTCDATETIME())
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_Jobs_Name' AND object_id = OBJECT_ID('maint.Jobs'))
BEGIN
    CREATE UNIQUE INDEX UX_Jobs_Name ON maint.Jobs (Name);
END
GO

IF OBJECT_ID('maint.JobExecutions', 'U') IS NULL
BEGIN
    CREATE TABLE maint.JobExecutions
    (
        JobExecutionId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_JobExecutions PRIMARY KEY,
        JobId INT NOT NULL CONSTRAINT FK_JobExecutions_Jobs FOREIGN KEY REFERENCES maint.Jobs(JobId),
        StartedAtUtc DATETIME2 NOT NULL CONSTRAINT DF_JobExecutions_StartedAtUtc DEFAULT (SYSUTCDATETIME()),
        CompletedAtUtc DATETIME2 NULL,
        Status NVARCHAR(50) NOT NULL,
        Message NVARCHAR(2000) NULL
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_JobExecutions_JobId' AND object_id = OBJECT_ID('maint.JobExecutions'))
BEGIN
    CREATE INDEX IX_JobExecutions_JobId ON maint.JobExecutions (JobId);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_JobExecutions_StartedAtUtc' AND object_id = OBJECT_ID('maint.JobExecutions'))
BEGIN
    CREATE INDEX IX_JobExecutions_StartedAtUtc ON maint.JobExecutions (StartedAtUtc);
END
GO
