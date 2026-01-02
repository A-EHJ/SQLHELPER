using Microsoft.Data.SqlClient;
using SQLHELPER.Infrastructure.Data;

namespace SQLHELPER.Services;

public class BootstrapService
{
    private readonly DbConnectionFactory _connectionFactory;

    public BootstrapService(DbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task InitializeAsync()
    {
        await EnsureDatabaseAsync();
        await EnsureTablesAsync();
    }

    private async Task EnsureDatabaseAsync()
    {
        await using var connection = _connectionFactory.CreateAdminConnection();
        await using var hubConnection = _connectionFactory.CreateHubConnection();
        var hubDatabase = hubConnection.Database;
        await connection.OpenAsync();
        var command = connection.CreateCommand();
        command.CommandText = "IF DB_ID(@db) IS NULL BEGIN EXEC('CREATE DATABASE [' + @db + ']'); END";
        command.Parameters.Add(new SqlParameter("@db", hubDatabase));
        await command.ExecuteNonQueryAsync();
    }

    private async Task EnsureTablesAsync()
    {
        await using var connection = _connectionFactory.CreateHubConnection();
        await connection.OpenAsync();

        var commands = new[]
        {
            @"IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Servers')
CREATE TABLE Servers (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(200) NOT NULL,
    Host NVARCHAR(200) NOT NULL,
    InstanceName NVARCHAR(200) NULL,
    Port INT NULL,
    UseIntegratedSecurity BIT NOT NULL DEFAULT 1,
    Username NVARCHAR(200) NULL,
    PasswordProtected NVARCHAR(MAX) NULL,
    CreatedAt DATETIME2 NOT NULL,
    UpdatedAt DATETIME2 NULL
);",
            @"IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'DbTargets')
CREATE TABLE DbTargets (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    ServerId INT NOT NULL CONSTRAINT FK_DbTargets_Servers REFERENCES Servers(Id),
    DatabaseName NVARCHAR(200) NOT NULL,
    IsActive BIT NOT NULL DEFAULT 1,
    Tags NVARCHAR(400) NULL,
    CreatedAt DATETIME2 NOT NULL
);",
            @"IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Runs')
CREATE TABLE Runs (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    ServerId INT NOT NULL CONSTRAINT FK_Runs_Servers REFERENCES Servers(Id),
    TargetId INT NULL CONSTRAINT FK_Runs_DbTargets REFERENCES DbTargets(Id),
    RunType NVARCHAR(100) NOT NULL,
    Status NVARCHAR(50) NOT NULL,
    StartedAt DATETIME2 NOT NULL,
    CompletedAt DATETIME2 NULL,
    Message NVARCHAR(MAX) NULL
);",
            @"IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'RunSteps')
CREATE TABLE RunSteps (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    RunId INT NOT NULL CONSTRAINT FK_RunSteps_Runs REFERENCES Runs(Id),
    StepName NVARCHAR(200) NOT NULL,
    Status NVARCHAR(50) NOT NULL,
    StartedAt DATETIME2 NOT NULL,
    CompletedAt DATETIME2 NULL,
    Details NVARCHAR(MAX) NULL
);",
            @"IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Notes')
CREATE TABLE Notes (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    ServerId INT NULL CONSTRAINT FK_Notes_Servers REFERENCES Servers(Id),
    TargetId INT NULL CONSTRAINT FK_Notes_DbTargets REFERENCES DbTargets(Id),
    Title NVARCHAR(200) NOT NULL,
    Body NVARCHAR(MAX) NOT NULL,
    CreatedBy NVARCHAR(200) NULL,
    CreatedAt DATETIME2 NOT NULL
);",
            @"IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'QueryFolders')
CREATE TABLE QueryFolders (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(200) NOT NULL,
    ParentFolderId INT NULL CONSTRAINT FK_QueryFolders_Parent REFERENCES QueryFolders(Id)
);",
            @"IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'SavedQueries')
CREATE TABLE SavedQueries (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    FolderId INT NULL CONSTRAINT FK_SavedQueries_QueryFolders REFERENCES QueryFolders(Id),
    Name NVARCHAR(200) NOT NULL,
    SqlText NVARCHAR(MAX) NOT NULL,
    Description NVARCHAR(MAX) NULL,
    IsFavorite BIT NOT NULL DEFAULT 0,
    CreatedAt DATETIME2 NOT NULL,
    UpdatedAt DATETIME2 NULL
);",
            @"IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'QueryRuns')
CREATE TABLE QueryRuns (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    SavedQueryId INT NULL CONSTRAINT FK_QueryRuns_SavedQueries REFERENCES SavedQueries(Id),
    TargetId INT NULL CONSTRAINT FK_QueryRuns_DbTargets REFERENCES DbTargets(Id),
    ExecutedAt DATETIME2 NOT NULL,
    DurationMs INT NULL,
    RowCount INT NULL,
    Error NVARCHAR(MAX) NULL
);"
        };

        foreach (var script in commands)
        {
            await using var cmd = connection.CreateCommand();
            cmd.CommandText = script;
            await cmd.ExecuteNonQueryAsync();
        }
    }
}
