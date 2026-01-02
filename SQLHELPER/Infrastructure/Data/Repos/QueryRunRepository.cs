using Dapper;
using SQLHELPER.Domain;

namespace SQLHELPER.Infrastructure.Data.Repos;

public class QueryRunRepository
{
    private readonly DbConnectionFactory _connectionFactory;

    public QueryRunRepository(DbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<int> InsertAsync(QueryRun run)
    {
        const string sql = @"INSERT INTO QueryRuns (SavedQueryId, TargetId, ExecutedAt, DurationMs, RowCount, Error)
VALUES (@SavedQueryId, @TargetId, @ExecutedAt, @DurationMs, @RowCount, @Error);
SELECT CAST(SCOPE_IDENTITY() AS int);";
        await using var connection = _connectionFactory.CreateHubConnection();
        run.ExecutedAt = run.ExecutedAt == default ? DateTime.UtcNow : run.ExecutedAt;
        return await connection.ExecuteScalarAsync<int>(sql, run);
    }

    public async Task<IEnumerable<QueryRun>> GetRecentAsync(int take = 50)
    {
        const string sql = "SELECT TOP(@Take) * FROM QueryRuns ORDER BY ExecutedAt DESC";
        await using var connection = _connectionFactory.CreateHubConnection();
        return await connection.QueryAsync<QueryRun>(sql, new { Take = take });
    }
}
