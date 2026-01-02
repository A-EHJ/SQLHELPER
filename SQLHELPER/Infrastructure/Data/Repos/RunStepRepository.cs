using Dapper;
using SQLHELPER.Domain;

namespace SQLHELPER.Infrastructure.Data.Repos;

public class RunStepRepository
{
    private readonly DbConnectionFactory _connectionFactory;

    public RunStepRepository(DbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<int> InsertAsync(RunStep step)
    {
        const string sql = @"INSERT INTO RunSteps (RunId, StepName, Status, StartedAt, CompletedAt, Details)
VALUES (@RunId, @StepName, @Status, @StartedAt, @CompletedAt, @Details);
SELECT CAST(SCOPE_IDENTITY() AS int);";
        await using var connection = _connectionFactory.CreateHubConnection();
        step.StartedAt = step.StartedAt == default ? DateTime.UtcNow : step.StartedAt;
        return await connection.ExecuteScalarAsync<int>(sql, step);
    }

    public async Task UpdateAsync(RunStep step)
    {
        const string sql = @"UPDATE RunSteps
SET StepName = @StepName,
    Status = @Status,
    StartedAt = @StartedAt,
    CompletedAt = @CompletedAt,
    Details = @Details
WHERE Id = @Id";
        await using var connection = _connectionFactory.CreateHubConnection();
        await connection.ExecuteAsync(sql, step);
    }

    public async Task<IEnumerable<RunStep>> GetByRunAsync(int runId)
    {
        const string sql = "SELECT * FROM RunSteps WHERE RunId = @RunId ORDER BY StartedAt";
        await using var connection = _connectionFactory.CreateHubConnection();
        return await connection.QueryAsync<RunStep>(sql, new { RunId = runId });
    }
}
