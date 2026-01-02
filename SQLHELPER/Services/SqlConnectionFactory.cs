using Microsoft.Data.SqlClient;
using SQLHELPER.Models;

namespace SQLHELPER.Services;

public class SqlConnectionFactory
{
    private readonly ConnectionState _connectionState;

    public SqlConnectionFactory(ConnectionState connectionState)
    {
        _connectionState = connectionState;
    }

    public async Task<SqlConnection> CreateMasterConnectionAsync()
    {
        await _connectionState.EnsureLoadedAsync();
        var connectionString = BuildForDatabase("master");
        return new SqlConnection(connectionString);
    }

    public async Task<SqlConnection> CreateSqlHelperConnectionAsync()
    {
        await _connectionState.EnsureLoadedAsync();
        var connectionString = BuildForDatabase("SQLHELPER");
        return new SqlConnection(connectionString);
    }

    public async Task<SqlConnection> CreateTargetConnectionAsync(string? databaseName = null)
    {
        await _connectionState.EnsureLoadedAsync();
        var targetDatabase = string.IsNullOrWhiteSpace(databaseName) ? _connectionState.Profile.DefaultTargetDb : databaseName;
        var connectionString = BuildForDatabase(targetDatabase);
        return new SqlConnection(connectionString);
    }

    public string BaseConnectionString => _connectionState.BuildBaseConnectionString();

    public string BuildForDatabase(string? databaseName) => _connectionState.BuildConnectionString(databaseName);
}
