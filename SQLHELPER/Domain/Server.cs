using System;

namespace SQLHELPER.Domain;

public class Server
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Host { get; set; } = string.Empty;
    public string? InstanceName { get; set; }
    public int? Port { get; set; }
    public bool UseIntegratedSecurity { get; set; } = true;
    public string? Username { get; set; }
    public string? PasswordProtected { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
