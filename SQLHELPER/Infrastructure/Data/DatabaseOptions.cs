namespace SQLHELPER.Infrastructure.Data;

public class DatabaseOptions
{
    public string Server { get; set; } = "localhost";
    public string? Instance { get; set; }
    public int? Port { get; set; }
    public string AdminDatabase { get; set; } = "master";
    public string HubDatabase { get; set; } = "SqlMaintenanceHub";
    public string Username { get; set; } = string.Empty;
    public string? Password { get; set; }
    public bool UseIntegratedSecurity { get; set; }
}
