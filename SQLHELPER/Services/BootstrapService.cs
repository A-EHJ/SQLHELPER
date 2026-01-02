using System.Text.RegularExpressions;
using System.Linq;
using Microsoft.Data.SqlClient;

namespace SQLHELPER.Services;

public class BootstrapService
{
    private readonly ILogger<BootstrapService> _logger;
    private readonly IConfiguration _configuration;
    private readonly string _scriptsDirectory;

    public BootstrapService(ILogger<BootstrapService> logger, IConfiguration configuration, IWebHostEnvironment environment)
    {
        _logger = logger;
        _configuration = configuration;
        _scriptsDirectory = Path.Combine(environment.ContentRootPath, "sql");
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        if (!Directory.Exists(_scriptsDirectory))
        {
            _logger.LogWarning("SQL bootstrap directory not found at {Directory}. Skipping bootstrap.", _scriptsDirectory);
            return;
        }

        var baseConnectionString = _configuration.GetConnectionString("SqlServer");
        if (string.IsNullOrWhiteSpace(baseConnectionString))
        {
            _logger.LogWarning("Connection string 'SqlServer' not configured. SQL bootstrap skipped.");
            return;
        }

        var masterConnectionString = BuildConnectionStringForDatabase(baseConnectionString, "master");
        var hubConnectionString = BuildConnectionStringForDatabase(baseConnectionString, "SqlMaintenanceHub");

        await ExecuteScriptAsync(masterConnectionString, "000_create_hub_db.sql", cancellationToken);
        await ExecuteScriptAsync(hubConnectionString, "001_create_schema_tables.sql", cancellationToken);
        await ExecuteScriptAsync(hubConnectionString, "002_seed.sql", cancellationToken);
    }

    private static string BuildConnectionStringForDatabase(string baseConnectionString, string databaseName)
    {
        var builder = new SqlConnectionStringBuilder(baseConnectionString)
        {
            InitialCatalog = databaseName,
            TrustServerCertificate = true
        };

        return builder.ConnectionString;
    }

    private async Task ExecuteScriptAsync(string connectionString, string scriptFileName, CancellationToken cancellationToken)
    {
        var scriptPath = Path.Combine(_scriptsDirectory, scriptFileName);
        if (!File.Exists(scriptPath))
        {
            _logger.LogWarning("SQL bootstrap script {Script} not found at {Path}.", scriptFileName, scriptPath);
            return;
        }

        var scriptContent = await File.ReadAllTextAsync(scriptPath, cancellationToken);
        if (string.IsNullOrWhiteSpace(scriptContent))
        {
            _logger.LogInformation("SQL bootstrap script {Script} is empty. Nothing to execute.", scriptFileName);
            return;
        }

        var batches = Regex.Split(scriptContent, @"^\s*GO\s*$", RegexOptions.IgnoreCase | RegexOptions.Multiline)
            .Where(batch => !string.IsNullOrWhiteSpace(batch))
            .ToArray();

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        foreach (var batch in batches)
        {
            await using var command = new SqlCommand(batch, connection);
            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        var targetDatabase = new SqlConnectionStringBuilder(connectionString).InitialCatalog;
        _logger.LogInformation("Executed {BatchCount} batch(es) from {Script} against {Database}.", batches.Length, scriptFileName, targetDatabase);
    }
}
