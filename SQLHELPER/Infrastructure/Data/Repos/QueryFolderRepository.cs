using Dapper;
using SQLHELPER.Domain;

namespace SQLHELPER.Infrastructure.Data.Repos;

public class QueryFolderRepository
{
    private readonly DbConnectionFactory _connectionFactory;

    public QueryFolderRepository(DbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<IEnumerable<QueryFolder>> GetAllAsync()
    {
        const string sql = "SELECT * FROM QueryFolders ORDER BY Name";
        await using var connection = _connectionFactory.CreateHubConnection();
        return await connection.QueryAsync<QueryFolder>(sql);
    }

    public async Task<int> InsertAsync(QueryFolder folder)
    {
        const string sql = @"INSERT INTO QueryFolders (Name, ParentFolderId)
VALUES (@Name, @ParentFolderId);
SELECT CAST(SCOPE_IDENTITY() AS int);";
        await using var connection = _connectionFactory.CreateHubConnection();
        return await connection.ExecuteScalarAsync<int>(sql, folder);
    }
}
