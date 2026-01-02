using Microsoft.Extensions.Configuration;

namespace SQLHELPER.Services;

public class SetupState
{
    private readonly bool _isConfiguredFromConfig;
    private bool _isConfigured;

    public SetupState(IConfiguration configuration)
    {
        _isConfiguredFromConfig = configuration.GetValue("Setup:IsConfigured", false);
        _isConfigured = _isConfiguredFromConfig;
    }

    public bool IsConfigured => _isConfigured || _isConfiguredFromConfig;
    public bool IsSetupRequired => !IsConfigured;

    public event Action? OnChange;

    public void MarkConfigured()
    {
        if (_isConfigured)
        {
            return;
        }

        _isConfigured = true;
        NotifyStateChanged();
    }

    public void MarkRequiresSetup()
    {
        if (!_isConfigured)
        {
            return;
        }

        _isConfigured = false;
        NotifyStateChanged();
    }

    public void SetSetupStatus(bool isConfigured)
    {
        _isConfigured = isConfigured || _isConfiguredFromConfig;
        NotifyStateChanged();
    }

    private void NotifyStateChanged() => OnChange?.Invoke();
}
