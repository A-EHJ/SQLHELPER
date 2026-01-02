using Dapper;
using SQLHELPER.Domain;

namespace SQLHELPER.Infrastructure.Data.Repos;

public class SavedQueryRepository
{
    private readonly DbConnectionFactory _connectionFactory;

    public SavedQueryRepository(DbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<IEnumerable<SavedQuery>> GetAllAsync()
    {
        const string sql = "SELECT * FROM SavedQueries ORDER BY Name";
        await using var connection = _connectionFactory.CreateHubConnection();
        return await connection.QueryAsync<SavedQuery>(sql);
    }

    public async Task<SavedQuery?> GetByIdAsync(int id)
    {
        const string sql = "SELECT * FROM SavedQueries WHERE Id = @Id";
        await using var connection = _connectionFactory.CreateHubConnection();
        return await connection.QuerySingleOrDefaultAsync<SavedQuery>(sql, new { Id = id });
    }

    public async Task<int> InsertAsync(SavedQuery query)
    {
        const string sql = @"INSERT INTO SavedQueries (FolderId, Name, SqlText, Description, IsFavorite, CreatedAt, UpdatedAt)
VALUES (@FolderId, @Name, @SqlText, @Description, @IsFavorite, @CreatedAt, @UpdatedAt);
SELECT CAST(SCOPE_IDENTITY() AS int);";
        await using var connection = _connectionFactory.CreateHubConnection();
        query.CreatedAt = query.CreatedAt == default ? DateTime.UtcNow : query.CreatedAt;
        query.UpdatedAt = query.CreatedAt;
        return await connection.ExecuteScalarAsync<int>(sql, query);
    }

    public async Task UpdateAsync(SavedQuery query)
    {
        const string sql = @"UPDATE SavedQueries
SET FolderId = @FolderId,
    Name = @Name,
    SqlText = @SqlText,
    Description = @Description,
    IsFavorite = @IsFavorite,
    UpdatedAt = @UpdatedAt
WHERE Id = @Id";
        await using var connection = _connectionFactory.CreateHubConnection();
        query.UpdatedAt = DateTime.UtcNow;
        await connection.ExecuteAsync(sql, query);
    }

    public async Task DeleteAsync(int id)
    {
        const string sql = "DELETE FROM SavedQueries WHERE Id = @Id";
        await using var connection = _connectionFactory.CreateHubConnection();
        await connection.ExecuteAsync(sql, new { Id = id });
    }
}
