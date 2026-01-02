namespace SQLHELPER.Services;

public class SqlHelperService
{
    private readonly List<DatabaseInfo> _databases =
    [
        new("Inventory", "Online", 42, "SQL Server 2022"),
        new("Finance", "Online", 18, "SQL Server 2019"),
        new("Analytics", "Restoring", 8, "PostgreSQL 14"),
    ];

    private readonly List<MaintenanceJob> _maintenanceJobs =
    [
        new("Index rebuild", "Weekly index maintenance across primary OLTP databases.", DateTimeOffset.UtcNow.AddDays(1), "Scheduled"),
        new("Statistics refresh", "Refresh statistics on heavy reporting tables.", DateTimeOffset.UtcNow.AddHours(6), "Running"),
        new("Backups", "Verify last full and differential backups across servers.", DateTimeOffset.UtcNow.AddMinutes(45), "Pending"),
    ];

    private readonly List<QueryHistoryEntry> _history =
    [
        new(Guid.NewGuid(), "Monthly revenue snapshot", "SELECT * FROM revenue_monthly", DateTimeOffset.UtcNow.AddMinutes(-15), "Completed"),
        new(Guid.NewGuid(), "Purge staging", "DELETE FROM stage_temp", DateTimeOffset.UtcNow.AddHours(-1), "Warning"),
        new(Guid.NewGuid(), "Load dimensions", "EXEC sync_dimensions", DateTimeOffset.UtcNow.AddDays(-1), "Completed"),
    ];

    private readonly List<NoteEntry> _notes =
    [
        new(Guid.NewGuid(), "Connection string", "Remember to rotate passwords every quarter.", DateTimeOffset.UtcNow.AddDays(-5)),
        new(Guid.NewGuid(), "Analytics restore", "Analytics database is running in read-only mode until Friday.", DateTimeOffset.UtcNow.AddDays(-1)),
    ];

    private readonly List<QueryDefinition> _savedQueries =
    [
        new(Guid.NewGuid(), "Recent orders", "SELECT TOP 100 * FROM orders ORDER BY created_at DESC", "Inventory"),
        new(Guid.NewGuid(), "Top customers", "SELECT TOP 50 name, total_spend FROM customers ORDER BY total_spend DESC", "Finance"),
    ];

    public Task<DashboardSnapshot> GetDashboardAsync() => Task.FromResult(new DashboardSnapshot(
        _databases.Count,
        _history.Count(h => h.ExecutedOn > DateTimeOffset.UtcNow.AddHours(-1)),
        _maintenanceJobs.Count(j => j.Status != "Completed"),
        _savedQueries.Count));

    public Task<IReadOnlyList<DatabaseInfo>> GetDatabasesAsync() => Task.FromResult<IReadOnlyList<DatabaseInfo>>(_databases);

    public Task<IReadOnlyList<MaintenanceJob>> GetMaintenanceJobsAsync() => Task.FromResult<IReadOnlyList<MaintenanceJob>>(_maintenanceJobs);

    public Task<IReadOnlyList<QueryHistoryEntry>> GetHistoryAsync() => Task.FromResult<IReadOnlyList<QueryHistoryEntry>>(_history.OrderByDescending(h => h.ExecutedOn).ToList());

    public Task<IReadOnlyList<QueryDefinition>> GetSavedQueriesAsync() => Task.FromResult<IReadOnlyList<QueryDefinition>>(_savedQueries);

    public Task<IReadOnlyList<NoteEntry>> GetNotesAsync() => Task.FromResult<IReadOnlyList<NoteEntry>>(_notes.OrderByDescending(n => n.CreatedOn).ToList());

    public Task AddNoteAsync(string title, string content)
    {
        _notes.Add(new NoteEntry(Guid.NewGuid(), title, content, DateTimeOffset.UtcNow));
        return Task.CompletedTask;
    }

    public Task<SqlQueryResult> ExecuteQueryAsync(string sql)
    {
        var normalized = sql.Trim();
        var rows = new List<IReadOnlyDictionary<string, object?>>();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return Task.FromResult(new SqlQueryResult([], [], "Provide a query to see results."));
        }

        rows.Add(new Dictionary<string, object?>
        {
            ["Input"] = normalized,
            ["Preview"] = normalized.Length > 40 ? normalized[..40] + "..." : normalized,
            ["ExecutedAt"] = DateTimeOffset.UtcNow
        });

        return Task.FromResult(new SqlQueryResult(
            new List<string> { "Input", "Preview", "ExecutedAt" },
            rows,
            "Simulated execution. Replace with real data access."));
    }
}

public record DashboardSnapshot(int Databases, int RecentActivity, int PendingMaintenance, int SavedQueries);

public record DatabaseInfo(string Name, string Status, int Tables, string EngineVersion);

public record MaintenanceJob(string Name, string Description, DateTimeOffset ScheduledFor, string Status);

public record QueryHistoryEntry(Guid Id, string Title, string Statement, DateTimeOffset ExecutedOn, string Status);

public record QueryDefinition(Guid Id, string Title, string Statement, string Database);

public record NoteEntry(Guid Id, string Title, string Content, DateTimeOffset CreatedOn);

public record SqlQueryResult(IReadOnlyList<string> Columns, IReadOnlyList<IReadOnlyDictionary<string, object?>> Rows, string Message);
