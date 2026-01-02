using Dapper;
using SQLHELPER.Domain;

namespace SQLHELPER.Infrastructure.Data.Repos;

public class RunRepository
{
    private readonly DbConnectionFactory _connectionFactory;

    public RunRepository(DbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<int> InsertAsync(Run run)
    {
        const string sql = @"INSERT INTO Runs (ServerId, TargetId, RunType, Status, StartedAt, CompletedAt, Message)
VALUES (@ServerId, @TargetId, @RunType, @Status, @StartedAt, @CompletedAt, @Message);
SELECT CAST(SCOPE_IDENTITY() AS int);";
        await using var connection = _connectionFactory.CreateHubConnection();
        run.StartedAt = run.StartedAt == default ? DateTime.UtcNow : run.StartedAt;
        return await connection.ExecuteScalarAsync<int>(sql, run);
    }

    public async Task UpdateAsync(Run run)
    {
        const string sql = @"UPDATE Runs
SET ServerId = @ServerId,
    TargetId = @TargetId,
    RunType = @RunType,
    Status = @Status,
    StartedAt = @StartedAt,
    CompletedAt = @CompletedAt,
    Message = @Message
WHERE Id = @Id";
        await using var connection = _connectionFactory.CreateHubConnection();
        await connection.ExecuteAsync(sql, run);
    }

    public async Task<Run?> GetByIdAsync(int id)
    {
        const string sql = "SELECT * FROM Runs WHERE Id = @Id";
        await using var connection = _connectionFactory.CreateHubConnection();
        return await connection.QuerySingleOrDefaultAsync<Run>(sql, new { Id = id });
    }

    public async Task<IEnumerable<Run>> GetRecentAsync(int take = 100)
    {
        const string sql = "SELECT TOP(@Take) * FROM Runs ORDER BY StartedAt DESC";
        await using var connection = _connectionFactory.CreateHubConnection();
        return await connection.QueryAsync<Run>(sql, new { Take = take });
    }
}
