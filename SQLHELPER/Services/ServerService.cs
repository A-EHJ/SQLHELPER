using SQLHELPER.Domain;
using SQLHELPER.Infrastructure.Data.Repos;
using SQLHELPER.Infrastructure.Security;
using SQLHELPER.Infrastructure.Settings;

namespace SQLHELPER.Services;

public class ServerService
{
    private readonly ServerRepository _serverRepository;
    private readonly DbTargetRepository _targetRepository;
    private readonly SettingsStore _settingsStore;
    private readonly AppSettingsLocal _settings;

    public ServerService(
        ServerRepository serverRepository,
        DbTargetRepository targetRepository,
        SettingsStore settingsStore,
        AppSettingsLocal settings)
    {
        _serverRepository = serverRepository;
        _targetRepository = targetRepository;
        _settingsStore = settingsStore;
        _settings = settings;
    }

    public async Task<IEnumerable<Server>> GetServersAsync()
    {
        var servers = await _serverRepository.GetAllAsync();
        return servers.Select(UnprotectPassword);
    }

    public async Task<Server?> GetServerAsync(int id)
    {
        var server = await _serverRepository.GetByIdAsync(id);
        return server is null ? null : UnprotectPassword(server);
    }

    public async Task<int> SaveServerAsync(Server server)
    {
        server.PasswordProtected = DpapiProtector.Protect(server.PasswordProtected);
        if (server.Id == 0)
        {
            var id = await _serverRepository.InsertAsync(server);
            server.Id = id;
        }
        else
        {
            await _serverRepository.UpdateAsync(server);
        }

        if (string.IsNullOrEmpty(_settings.PreferredServer))
        {
            _settings.PreferredServer = server.Name;
            await _settingsStore.SaveAsync(_settings);
        }

        return server.Id;
    }

    public async Task DeleteServerAsync(int id)
    {
        await _serverRepository.DeleteAsync(id);
    }

    public async Task<IEnumerable<DbTarget>> GetTargetsAsync(int serverId)
    {
        return await _targetRepository.GetByServerAsync(serverId);
    }

    public async Task<int> SaveTargetAsync(DbTarget target)
    {
        if (target.Id == 0)
        {
            return await _targetRepository.InsertAsync(target);
        }

        await _targetRepository.UpdateAsync(target);
        return target.Id;
    }

    public async Task DeleteTargetAsync(int id)
    {
        await _targetRepository.DeleteAsync(id);
    }

    private static Server UnprotectPassword(Server server)
    {
        server.PasswordProtected = DpapiProtector.Unprotect(server.PasswordProtected);
        return server;
    }
}
