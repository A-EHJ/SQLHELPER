using Dapper;
using SQLHELPER.Domain;

namespace SQLHELPER.Infrastructure.Data.Repos;

public class DbTargetRepository
{
    private readonly DbConnectionFactory _connectionFactory;

    public DbTargetRepository(DbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<IEnumerable<DbTarget>> GetAllAsync()
    {
        const string sql = "SELECT * FROM DbTargets ORDER BY DatabaseName";
        await using var connection = _connectionFactory.CreateHubConnection();
        return await connection.QueryAsync<DbTarget>(sql);
    }

    public async Task<IEnumerable<DbTarget>> GetByServerAsync(int serverId)
    {
        const string sql = "SELECT * FROM DbTargets WHERE ServerId = @ServerId";
        await using var connection = _connectionFactory.CreateHubConnection();
        return await connection.QueryAsync<DbTarget>(sql, new { ServerId = serverId });
    }

    public async Task<DbTarget?> GetByIdAsync(int id)
    {
        const string sql = "SELECT * FROM DbTargets WHERE Id = @Id";
        await using var connection = _connectionFactory.CreateHubConnection();
        return await connection.QuerySingleOrDefaultAsync<DbTarget>(sql, new { Id = id });
    }

    public async Task<int> InsertAsync(DbTarget target)
    {
        const string sql = @"INSERT INTO DbTargets (ServerId, DatabaseName, IsActive, Tags, CreatedAt)
VALUES (@ServerId, @DatabaseName, @IsActive, @Tags, @CreatedAt);
SELECT CAST(SCOPE_IDENTITY() AS int);";
        await using var connection = _connectionFactory.CreateHubConnection();
        target.CreatedAt = DateTime.UtcNow;
        return await connection.ExecuteScalarAsync<int>(sql, target);
    }

    public async Task UpdateAsync(DbTarget target)
    {
        const string sql = @"UPDATE DbTargets
SET ServerId = @ServerId,
    DatabaseName = @DatabaseName,
    IsActive = @IsActive,
    Tags = @Tags
WHERE Id = @Id";
        await using var connection = _connectionFactory.CreateHubConnection();
        await connection.ExecuteAsync(sql, target);
    }

    public async Task DeleteAsync(int id)
    {
        const string sql = "DELETE FROM DbTargets WHERE Id = @Id";
        await using var connection = _connectionFactory.CreateHubConnection();
        await connection.ExecuteAsync(sql, new { Id = id });
    }
}
