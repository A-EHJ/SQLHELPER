namespace SQLHELPER.Infrastructure.Settings;

public class AppSettingsLocal
{
    public bool SafeMode { get; set; } = true;
    public string? PasswordProtected { get; set; }
    public string? PreferredServer { get; set; }
    public string? LastExportPath { get; set; }
}
