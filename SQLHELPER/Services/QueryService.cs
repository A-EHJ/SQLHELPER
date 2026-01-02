using System.Diagnostics;
using System.Text.Json;
using Dapper;
using SQLHELPER.Domain;
using SQLHELPER.Infrastructure.Data;
using SQLHELPER.Infrastructure.Data.Repos;
using SQLHELPER.Infrastructure.Settings;

namespace SQLHELPER.Services;

public class QueryService
{
    private readonly DbConnectionFactory _connectionFactory;
    private readonly SavedQueryRepository _savedQueries;
    private readonly QueryRunRepository _queryRuns;
    private readonly AppSettingsLocal _settings;
    private readonly SettingsStore _settingsStore;

    public QueryService(
        DbConnectionFactory connectionFactory,
        SavedQueryRepository savedQueries,
        QueryRunRepository queryRuns,
        AppSettingsLocal settings,
        SettingsStore settingsStore)
    {
        _connectionFactory = connectionFactory;
        _savedQueries = savedQueries;
        _queryRuns = queryRuns;
        _settings = settings;
        _settingsStore = settingsStore;
    }

    public bool SafeMode => _settings.SafeMode;

    public async Task ToggleSafeModeAsync(bool safeMode)
    {
        _settings.SafeMode = safeMode;
        await _settingsStore.SaveAsync(_settings);
    }

    public async Task<IEnumerable<SavedQuery>> GetSavedQueriesAsync()
    {
        return await _savedQueries.GetAllAsync();
    }

    public async Task<SavedQuery?> GetSavedQueryAsync(int id)
    {
        return await _savedQueries.GetByIdAsync(id);
    }

    public async Task<int> SaveQueryAsync(SavedQuery query)
    {
        if (query.Id == 0)
        {
            return await _savedQueries.InsertAsync(query);
        }

        await _savedQueries.UpdateAsync(query);
        return query.Id;
    }

    public async Task DeleteQueryAsync(int id)
    {
        await _savedQueries.DeleteAsync(id);
    }

    public async Task<IEnumerable<QueryRun>> GetHistoryAsync(int take = 50)
    {
        return await _queryRuns.GetRecentAsync(take);
    }

    public async Task<QueryRun> LogRunAsync(QueryRun run)
    {
        var id = await _queryRuns.InsertAsync(run);
        run.Id = id;
        return run;
    }

    public async Task<(IEnumerable<dynamic> Rows, QueryRun Log)> ExecuteAgainstTargetAsync(Server server, DbTarget target, string sql, int? savedQueryId = null, CancellationToken cancellationToken = default)
    {
        if (_settings.SafeMode && IsDangerous(sql))
        {
            throw new InvalidOperationException("Safe mode bloquea comandos destructivos. Revise la consulta.");
        }

        var stopwatch = Stopwatch.StartNew();
        IEnumerable<dynamic> rows = Array.Empty<dynamic>();
        string? error = null;
        try
        {
            await using var connection = _connectionFactory.CreateTargetConnection(server, target.DatabaseName);
            await connection.OpenAsync(cancellationToken);
            var result = await connection.QueryAsync(sql);
            rows = result.ToList();
        }
        catch (Exception ex)
        {
            error = ex.Message;
            throw;
        }
        finally
        {
            stopwatch.Stop();
            var logEntry = new QueryRun
            {
                SavedQueryId = savedQueryId,
                TargetId = target.Id,
                ExecutedAt = DateTime.UtcNow,
                DurationMs = (int)stopwatch.ElapsedMilliseconds,
                RowCount = rows.Count(),
                Error = error
            };

            await LogRunAsync(logEntry);
        }

        var outputLog = new QueryRun
        {
            SavedQueryId = savedQueryId,
            TargetId = target.Id,
            ExecutedAt = DateTime.UtcNow,
            DurationMs = (int)stopwatch.ElapsedMilliseconds,
            RowCount = rows.Count(),
            Error = error
        };

        return (rows, outputLog);
    }

    public async Task ExportSavedQueriesAsync(string path)
    {
        var queries = await _savedQueries.GetAllAsync();
        var json = JsonSerializer.Serialize(queries, new JsonSerializerOptions(JsonSerializerDefaults.Web) { WriteIndented = true });
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        await File.WriteAllTextAsync(path, json);
        _settings.LastExportPath = path;
        await _settingsStore.SaveAsync(_settings);
    }

    public async Task ImportSavedQueriesAsync(string path, bool overwrite = false)
    {
        if (!File.Exists(path))
        {
            throw new FileNotFoundException("No se encontró el archivo de exportación", path);
        }

        var json = await File.ReadAllTextAsync(path);
        var queries = JsonSerializer.Deserialize<IEnumerable<SavedQuery>>(json, new JsonSerializerOptions(JsonSerializerDefaults.Web))
                     ?? Enumerable.Empty<SavedQuery>();

        if (overwrite)
        {
            var existing = await _savedQueries.GetAllAsync();
            foreach (var query in existing)
            {
                await _savedQueries.DeleteAsync(query.Id);
            }
        }

        foreach (var query in queries)
        {
            query.Id = 0;
            await _savedQueries.InsertAsync(query);
        }

        _settings.LastExportPath = path;
        await _settingsStore.SaveAsync(_settings);
    }

    private static bool IsDangerous(string sql)
    {
        var normalized = sql.ToUpperInvariant();
        var dangerousTokens = new[] { "DROP ", "ALTER ", "DELETE ", "TRUNCATE ", "UPDATE ", "INSERT ", "EXEC " };
        return dangerousTokens.Any(t => normalized.Contains(t));
    }
}
