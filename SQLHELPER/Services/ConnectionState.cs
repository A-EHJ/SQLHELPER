using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using Microsoft.Data.SqlClient;
using SQLHELPER.Models;

namespace SQLHELPER.Services;

public class ConnectionState
{
    private readonly ProtectedLocalStorage _protectedLocalStorage;
    private const string StorageKey = "sqlhelper.profile";
    private bool _isLoaded;

    public ConnectionState(ProtectedLocalStorage protectedLocalStorage)
    {
        _protectedLocalStorage = protectedLocalStorage;
    }

    public ConnectionProfile Profile { get; private set; } = new();

    public event Action? OnChange;

    public async Task EnsureLoadedAsync()
    {
        if (_isLoaded)
        {
            return;
        }

        try
        {
            var result = await _protectedLocalStorage.GetAsync<ConnectionProfile>(StorageKey);
            if (result.Success && result.Value is not null)
            {
                Profile = NormalizeProfile(result.Value);
            }
        }
        catch
        {
            Profile = new ConnectionProfile();
        }

        _isLoaded = true;
        NotifyChanged();
    }

    public async Task SaveAsync(ConnectionProfile profile)
    {
        Profile = NormalizeProfile(profile);
        await _protectedLocalStorage.SetAsync(StorageKey, Profile);
        NotifyChanged();
    }

    public async Task<(bool Success, string Message)> TestConnectionAsync(ConnectionProfile? profileOverride = null)
    {
        try
        {
            await EnsureLoadedAsync();
            var profile = NormalizeProfile(profileOverride ?? Profile);
            ValidateProfile(profile);

            await using var connection = new SqlConnection(BuildConnectionStringInternal(profile, "master"));
            await connection.OpenAsync();
            return (true, "Connection successful.");
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    public string BuildBaseConnectionString()
    {
        ValidateProfile(Profile);
        return BuildConnectionStringInternal(Profile, null);
    }

    public string BuildConnectionString(string? databaseName)
    {
        return BuildConnectionStringInternal(Profile, databaseName);
    }

    public async Task UpdateTargetDatabaseAsync(string databaseName)
    {
        await EnsureLoadedAsync();
        Profile.DefaultTargetDb = string.IsNullOrWhiteSpace(databaseName) ? "SIN" : databaseName;
        await _protectedLocalStorage.SetAsync(StorageKey, Profile);
        NotifyChanged();
    }

    private static void ValidateProfile(ConnectionProfile profile)
    {
        if (!profile.HasRequiredValues)
        {
            throw new InvalidOperationException("Configure the connection profile before continuing.");
        }
    }

    private static ConnectionProfile NormalizeProfile(ConnectionProfile profile)
    {
        profile.DefaultTargetDb = string.IsNullOrWhiteSpace(profile.DefaultTargetDb) ? "SIN" : profile.DefaultTargetDb.Trim();
        profile.Server = profile.Server?.Trim() ?? string.Empty;
        profile.User = profile.User?.Trim() ?? string.Empty;
        profile.Password = profile.Password ?? string.Empty;
        return profile;
    }

    private void NotifyChanged() => OnChange?.Invoke();

    private static string BuildConnectionStringInternal(ConnectionProfile profile, string? databaseName)
    {
        var builder = new SqlConnectionStringBuilder
        {
            DataSource = profile.Server,
            UserID = profile.User,
            Password = profile.Password,
            Encrypt = profile.Encrypt,
            TrustServerCertificate = profile.TrustServerCertificate,
            InitialCatalog = databaseName
        };

        return builder.ConnectionString;
    }
}
