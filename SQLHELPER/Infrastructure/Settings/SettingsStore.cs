using System.Text.Json;
using SQLHELPER.Infrastructure.Security;

namespace SQLHELPER.Infrastructure.Settings;

public class SettingsStore
{
    private readonly string _settingsPath;
    private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    public SettingsStore()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        if (string.IsNullOrWhiteSpace(appData))
        {
            appData = Environment.GetEnvironmentVariable("APPDATA")
                ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".config");
        }

        _settingsPath = Path.Combine(appData, "SqlMaintenanceHub", "settings.json");
    }

    public async Task<AppSettingsLocal> LoadAsync()
    {
        if (!File.Exists(_settingsPath))
        {
            return new AppSettingsLocal();
        }

        await using var stream = File.OpenRead(_settingsPath);
        var settings = await JsonSerializer.DeserializeAsync<AppSettingsLocal>(stream, _jsonOptions) ?? new AppSettingsLocal();
        settings.PasswordProtected = DpapiProtector.Unprotect(settings.PasswordProtected);
        return settings;
    }

    public async Task SaveAsync(AppSettingsLocal settings)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_settingsPath)!);
        var toPersist = new AppSettingsLocal
        {
            SafeMode = settings.SafeMode,
            PreferredServer = settings.PreferredServer,
            LastExportPath = settings.LastExportPath,
            PasswordProtected = DpapiProtector.Protect(settings.PasswordProtected)
        };

        await using var stream = File.Create(_settingsPath);
        await JsonSerializer.SerializeAsync(stream, toPersist, _jsonOptions);
    }
}
