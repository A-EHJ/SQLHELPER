using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;
using SQLHELPER.Models;

namespace SQLHELPER.Services;

public class SqlWorkspaceService
{
    private readonly SqlConnectionFactory _connectionFactory;
    private readonly ILogger<SqlWorkspaceService> _logger;

    public SqlWorkspaceService(SqlConnectionFactory connectionFactory, ILogger<SqlWorkspaceService> logger)
    {
        _connectionFactory = connectionFactory;
        _logger = logger;
    }

    public string SetupScript => BuildSetupScript();

    public async Task<ServerInformation?> GetServerInformationAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await using var connection = await _connectionFactory.CreateMasterConnectionAsync();
            await connection.OpenAsync(cancellationToken);

            const string sql = """
                SELECT
                    CAST(SERVERPROPERTY('ServerName') AS NVARCHAR(128)) AS ServerName,
                    CAST(SERVERPROPERTY('Edition') AS NVARCHAR(128)) AS Edition,
                    CAST(SERVERPROPERTY('ProductVersion') AS NVARCHAR(128)) AS ProductVersion,
                    CAST(SERVERPROPERTY('ProductLevel') AS NVARCHAR(128)) AS ProductLevel;
                """;

            await using var command = new SqlCommand(sql, connection);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            if (await reader.ReadAsync(cancellationToken))
            {
                return new ServerInformation(
                    reader.GetString(0),
                    reader.GetString(1),
                    reader.GetString(2),
                    reader.GetString(3));
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to retrieve server information.");
        }

        return null;
    }

    public async Task<IReadOnlyList<DatabaseOverview>> GetDatabasesAsync(CancellationToken cancellationToken = default)
    {
        var databases = new List<DatabaseOverview>();

        const string sql = """
            SELECT
                d.name,
                d.state_desc,
                d.recovery_model_desc,
                d.compatibility_level,
                CAST(SUM(mf.size) * 8.0 / 1024 AS DECIMAL(18,2)) AS SizeMb
            FROM sys.databases d
            LEFT JOIN sys.master_files mf ON d.database_id = mf.database_id
            GROUP BY d.name, d.state_desc, d.recovery_model_desc, d.compatibility_level
            ORDER BY d.name;
            """;

        await using var connection = await _connectionFactory.CreateMasterConnectionAsync();
        await connection.OpenAsync(cancellationToken);
        await using var command = new SqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            databases.Add(new DatabaseOverview(
                reader.GetString(0),
                reader.GetString(1),
                reader.GetString(2),
                reader.GetInt32(3),
                reader.IsDBNull(4) ? 0 : reader.GetDecimal(4)));
        }

        return databases;
    }

    public async Task<QueryExecutionResult> ExecuteQueryAsync(string sql, string databaseName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sql))
        {
            return new QueryExecutionResult(false, Array.Empty<QueryResultSet>(), "Provide a query to execute.", null);
        }

        var resultSets = new List<QueryResultSet>();
        try
        {
            await using var connection = await _connectionFactory.CreateTargetConnectionAsync(databaseName);
            await connection.OpenAsync(cancellationToken);
            await using var command = connection.CreateCommand();
            command.CommandText = sql;
            command.CommandTimeout = 90;

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            do
            {
                var columns = Enumerable.Range(0, reader.FieldCount)
                    .Select(reader.GetName)
                    .ToList();

                var rows = new List<IReadOnlyDictionary<string, object?>>();
                while (await reader.ReadAsync(cancellationToken))
                {
                    var row = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
                    for (var i = 0; i < reader.FieldCount; i++)
                    {
                        row[columns[i]] = reader.IsDBNull(i) ? null : reader.GetValue(i);
                    }

                    rows.Add(row);
                }

                resultSets.Add(new QueryResultSet(columns, rows));
            } while (await reader.NextResultAsync(cancellationToken));

            return new QueryExecutionResult(true, resultSets, "Query executed successfully.", null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing query.");
            return new QueryExecutionResult(false, resultSets, "Failed to execute query.", ex.Message);
        }
    }

    public async Task<IReadOnlyList<SavedQuery>> GetSavedQueriesAsync(string? search = null, CancellationToken cancellationToken = default)
    {
        var queries = new List<SavedQuery>();
        var sql = """
            SELECT Id, Title, Statement, Tags, DatabaseName, CreatedAt, UpdatedAt
            FROM dbo.SavedQueries
            WHERE (@search IS NULL OR Title LIKE @term OR Tags LIKE @term)
            ORDER BY UpdatedAt DESC;
            """;

        await using var connection = await _connectionFactory.CreateSqlHelperConnectionAsync();
        await connection.OpenAsync(cancellationToken);
        await using var command = new SqlCommand(sql, connection);
        var term = search is null ? DBNull.Value : $"%{search}%";
        command.Parameters.AddWithValue("@search", search is null ? DBNull.Value : search);
        command.Parameters.AddWithValue("@term", term);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            queries.Add(new SavedQuery
            {
                Id = reader.GetGuid(0),
                Title = reader.GetString(1),
                Statement = reader.GetString(2),
                Tags = reader.IsDBNull(3) ? null : reader.GetString(3),
                DatabaseName = reader.IsDBNull(4) ? null : reader.GetString(4),
                CreatedAt = reader.GetDateTimeOffset(5),
                UpdatedAt = reader.GetDateTimeOffset(6)
            });
        }

        return queries;
    }

    public async Task<SavedQuery?> GetSavedQueryAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var sql = """
            SELECT Id, Title, Statement, Tags, DatabaseName, CreatedAt, UpdatedAt
            FROM dbo.SavedQueries
            WHERE Id = @id;
            """;

        await using var connection = await _connectionFactory.CreateSqlHelperConnectionAsync();
        await connection.OpenAsync(cancellationToken);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@id", id);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (await reader.ReadAsync(cancellationToken))
        {
            return new SavedQuery
            {
                Id = reader.GetGuid(0),
                Title = reader.GetString(1),
                Statement = reader.GetString(2),
                Tags = reader.IsDBNull(3) ? null : reader.GetString(3),
                DatabaseName = reader.IsDBNull(4) ? null : reader.GetString(4),
                CreatedAt = reader.GetDateTimeOffset(5),
                UpdatedAt = reader.GetDateTimeOffset(6)
            };
        }

        return null;
    }

    public async Task UpsertSavedQueryAsync(SavedQuery savedQuery, CancellationToken cancellationToken = default)
    {
        var sql = """
            MERGE dbo.SavedQueries AS target
            USING (SELECT @Id AS Id) AS source
            ON target.Id = source.Id
            WHEN MATCHED THEN
                UPDATE SET Title = @Title, Statement = @Statement, Tags = @Tags, DatabaseName = @DatabaseName, UpdatedAt = SYSUTCDATETIME()
            WHEN NOT MATCHED THEN
                INSERT (Id, Title, Statement, Tags, DatabaseName, CreatedAt, UpdatedAt)
                VALUES (@Id, @Title, @Statement, @Tags, @DatabaseName, SYSUTCDATETIME(), SYSUTCDATETIME());
            """;

        await using var connection = await _connectionFactory.CreateSqlHelperConnectionAsync();
        await connection.OpenAsync(cancellationToken);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@Id", savedQuery.Id);
        command.Parameters.AddWithValue("@Title", savedQuery.Title);
        command.Parameters.AddWithValue("@Statement", savedQuery.Statement);
        command.Parameters.AddWithValue("@Tags", (object?)savedQuery.Tags ?? DBNull.Value);
        command.Parameters.AddWithValue("@DatabaseName", (object?)savedQuery.DatabaseName ?? DBNull.Value);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task DeleteSavedQueryAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var sql = "DELETE FROM dbo.SavedQueries WHERE Id = @Id;";
        await using var connection = await _connectionFactory.CreateSqlHelperConnectionAsync();
        await connection.OpenAsync(cancellationToken);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@Id", id);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<NoteItem>> SearchNotesAsync(string? search, string? tagFilter, CancellationToken cancellationToken = default)
    {
        var notes = new List<NoteItem>();
        var sql = """
            SELECT Id, Title, Body, Tags, DatabaseName, CreatedAt, UpdatedAt
            FROM dbo.Notes
            WHERE (@search IS NULL OR Title LIKE @pattern OR Body LIKE @pattern OR Tags LIKE @pattern)
              AND (@tag IS NULL OR Tags LIKE @tagPattern)
            ORDER BY UpdatedAt DESC;
            """;

        await using var connection = await _connectionFactory.CreateSqlHelperConnectionAsync();
        await connection.OpenAsync(cancellationToken);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@search", search is null ? DBNull.Value : search);
        command.Parameters.AddWithValue("@pattern", search is null ? DBNull.Value : $"%{search}%");
        command.Parameters.AddWithValue("@tag", tagFilter is null ? DBNull.Value : tagFilter);
        command.Parameters.AddWithValue("@tagPattern", tagFilter is null ? DBNull.Value : $"%{tagFilter}%");

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            notes.Add(new NoteItem
            {
                Id = reader.GetGuid(0),
                Title = reader.GetString(1),
                Body = reader.GetString(2),
                Tags = reader.IsDBNull(3) ? null : reader.GetString(3),
                DatabaseName = reader.IsDBNull(4) ? null : reader.GetString(4),
                CreatedAt = reader.GetDateTimeOffset(5),
                UpdatedAt = reader.GetDateTimeOffset(6)
            });
        }

        return notes;
    }

    public async Task UpsertNoteAsync(NoteItem note, CancellationToken cancellationToken = default)
    {
        var sql = """
            MERGE dbo.Notes AS target
            USING (SELECT @Id AS Id) AS source
            ON target.Id = source.Id
            WHEN MATCHED THEN
                UPDATE SET Title = @Title, Body = @Body, Tags = @Tags, DatabaseName = @DatabaseName, UpdatedAt = SYSUTCDATETIME()
            WHEN NOT MATCHED THEN
                INSERT (Id, Title, Body, Tags, DatabaseName, CreatedAt, UpdatedAt)
                VALUES (@Id, @Title, @Body, @Tags, @DatabaseName, SYSUTCDATETIME(), SYSUTCDATETIME());
            """;

        await using var connection = await _connectionFactory.CreateSqlHelperConnectionAsync();
        await connection.OpenAsync(cancellationToken);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@Id", note.Id);
        command.Parameters.AddWithValue("@Title", note.Title);
        command.Parameters.AddWithValue("@Body", note.Body);
        command.Parameters.AddWithValue("@Tags", (object?)note.Tags ?? DBNull.Value);
        command.Parameters.AddWithValue("@DatabaseName", (object?)note.DatabaseName ?? DBNull.Value);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task DeleteNoteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var sql = "DELETE FROM dbo.Notes WHERE Id = @Id;";
        await using var connection = await _connectionFactory.CreateSqlHelperConnectionAsync();
        await connection.OpenAsync(cancellationToken);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@Id", id);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<SetupResult> RunSetupAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await using var connection = await _connectionFactory.CreateMasterConnectionAsync();
            await connection.OpenAsync(cancellationToken);

            var batches = SplitBatches(SetupScript);
            foreach (var batch in batches)
            {
                await using var command = new SqlCommand(batch, connection);
                await command.ExecuteNonQueryAsync(cancellationToken);
            }

            return new SetupResult(true, "Setup executed successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to run setup script.");
            return new SetupResult(false, ex.Message);
        }
    }

    public async Task<bool> IsSetupCompleteAsync(CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT db_id('SQLHELPER');
            """;

        await using var connection = await _connectionFactory.CreateMasterConnectionAsync();
        await connection.OpenAsync(cancellationToken);
        await using var command = new SqlCommand(sql, connection);
        var result = await command.ExecuteScalarAsync(cancellationToken);
        if (result is null || result is DBNull)
        {
            return false;
        }

        return await AreTablesPresentAsync(cancellationToken);
    }

    private async Task<bool> AreTablesPresentAsync(CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT COUNT(*) FROM sys.tables WHERE name IN ('Notes', 'SavedQueries');
            """;

        await using var connection = await _connectionFactory.CreateSqlHelperConnectionAsync();
        await connection.OpenAsync(cancellationToken);
        await using var command = new SqlCommand(sql, connection);
        var count = (int)(await command.ExecuteScalarAsync(cancellationToken) ?? 0);
        return count >= 2;
    }

    private static string BuildSetupScript()
    {
        var builder = new StringBuilder();
        builder.AppendLine("IF NOT EXISTS (SELECT 1 FROM sys.databases WHERE name = 'SQLHELPER')");
        builder.AppendLine("BEGIN");
        builder.AppendLine("    CREATE DATABASE SQLHELPER;");
        builder.AppendLine("END");
        builder.AppendLine("GO");
        builder.AppendLine("USE [SQLHELPER];");
        builder.AppendLine("GO");
        builder.AppendLine("""
            IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Notes')
            BEGIN
                CREATE TABLE dbo.Notes
                (
                    Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
                    Title NVARCHAR(200) NOT NULL,
                    Body NVARCHAR(MAX) NOT NULL,
                    Tags NVARCHAR(400) NULL,
                    DatabaseName SYSNAME NULL,
                    CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
                    UpdatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
                );
            END
            """);
        builder.AppendLine("GO");
        builder.AppendLine("""
            IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'SavedQueries')
            BEGIN
                CREATE TABLE dbo.SavedQueries
                (
                    Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
                    Title NVARCHAR(200) NOT NULL,
                    Statement NVARCHAR(MAX) NOT NULL,
                    Tags NVARCHAR(400) NULL,
                    DatabaseName SYSNAME NULL,
                    CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
                    UpdatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
                );
            END
            """);
        builder.AppendLine("GO");
        builder.AppendLine("""
            CREATE OR ALTER VIEW dbo.NotesByTag
            AS
            SELECT Id, Title, Body, Tags, DatabaseName, CreatedAt, UpdatedAt
            FROM dbo.Notes;
            """);
        builder.AppendLine("GO");
        return builder.ToString();
    }

    private static IReadOnlyList<string> SplitBatches(string scriptContent)
    {
        var batches = Regex.Split(scriptContent, @"^\s*GO\s*$", RegexOptions.IgnoreCase | RegexOptions.Multiline)
            .Where(batch => !string.IsNullOrWhiteSpace(batch))
            .Select(batch => batch.Trim())
            .ToList();
        return batches;
    }
}
