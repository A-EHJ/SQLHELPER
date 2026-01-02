using Dapper;
using SQLHELPER.Domain;

namespace SQLHELPER.Infrastructure.Data.Repos;

public class ServerRepository
{
    private readonly DbConnectionFactory _connectionFactory;

    public ServerRepository(DbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<IEnumerable<Server>> GetAllAsync()
    {
        const string sql = "SELECT * FROM Servers ORDER BY Name";
        await using var connection = _connectionFactory.CreateHubConnection();
        return await connection.QueryAsync<Server>(sql);
    }

    public async Task<Server?> GetByIdAsync(int id)
    {
        const string sql = "SELECT * FROM Servers WHERE Id = @Id";
        await using var connection = _connectionFactory.CreateHubConnection();
        return await connection.QuerySingleOrDefaultAsync<Server>(sql, new { Id = id });
    }

    public async Task<int> InsertAsync(Server server)
    {
        const string sql = @"INSERT INTO Servers (Name, Host, InstanceName, Port, UseIntegratedSecurity, Username, PasswordProtected, CreatedAt, UpdatedAt)
VALUES (@Name, @Host, @InstanceName, @Port, @UseIntegratedSecurity, @Username, @PasswordProtected, @CreatedAt, @UpdatedAt);
SELECT CAST(SCOPE_IDENTITY() AS int);";
        await using var connection = _connectionFactory.CreateHubConnection();
        server.CreatedAt = DateTime.UtcNow;
        server.UpdatedAt = server.CreatedAt;
        return await connection.ExecuteScalarAsync<int>(sql, server);
    }

    public async Task UpdateAsync(Server server)
    {
        const string sql = @"UPDATE Servers
SET Name = @Name,
    Host = @Host,
    InstanceName = @InstanceName,
    Port = @Port,
    UseIntegratedSecurity = @UseIntegratedSecurity,
    Username = @Username,
    PasswordProtected = @PasswordProtected,
    UpdatedAt = @UpdatedAt
WHERE Id = @Id";
        await using var connection = _connectionFactory.CreateHubConnection();
        server.UpdatedAt = DateTime.UtcNow;
        await connection.ExecuteAsync(sql, server);
    }

    public async Task DeleteAsync(int id)
    {
        const string sql = "DELETE FROM Servers WHERE Id = @Id";
        await using var connection = _connectionFactory.CreateHubConnection();
        await connection.ExecuteAsync(sql, new { Id = id });
    }
}
