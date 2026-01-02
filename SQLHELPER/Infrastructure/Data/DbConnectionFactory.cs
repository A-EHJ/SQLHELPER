using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using SQLHELPER.Domain;

namespace SQLHELPER.Infrastructure.Data;

public class DbConnectionFactory
{
    private readonly DatabaseOptions _options;

    public DbConnectionFactory(IOptions<DatabaseOptions> options)
    {
        _options = options.Value;
    }

    public SqlConnection CreateAdminConnection()
    {
        return CreateConnection(
            _options.AdminDatabase,
            _options.Server,
            _options.Instance,
            _options.Port,
            _options.UseIntegratedSecurity,
            _options.Username,
            _options.Password);
    }

    public SqlConnection CreateHubConnection()
    {
        return CreateConnection(
            _options.HubDatabase,
            _options.Server,
            _options.Instance,
            _options.Port,
            _options.UseIntegratedSecurity,
            _options.Username,
            _options.Password);
    }

    public SqlConnection CreateTargetConnection(Server server, string databaseName)
    {
        return CreateConnection(
            databaseName,
            server.Host,
            server.InstanceName,
            server.Port,
            server.UseIntegratedSecurity,
            server.Username,
            server.PasswordProtected);
    }

    private static string BuildDataSource(string server, string? instance, int? port)
    {
        if (!string.IsNullOrWhiteSpace(instance))
        {
            return $"{server}\\{instance}";
        }

        if (port.HasValue)
        {
            return $"{server},{port}";
        }

        return server;
    }

    private static SqlConnection CreateConnection(string database, string server, string? instance, int? port, bool integratedSecurity, string? username, string? password)
    {
        var builder = new SqlConnectionStringBuilder
        {
            InitialCatalog = database,
            DataSource = BuildDataSource(server, instance, port),
            TrustServerCertificate = true,
            MultipleActiveResultSets = true,
            Encrypt = true
        };

        if (integratedSecurity)
        {
            builder.IntegratedSecurity = true;
        }
        else
        {
            builder.IntegratedSecurity = false;
            builder.UserID = username;
            builder.Password = password;
        }

        return new SqlConnection(builder.ConnectionString);
    }
}
