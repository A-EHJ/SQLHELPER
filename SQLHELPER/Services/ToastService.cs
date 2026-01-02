namespace SQLHELPER.Services;

public class ToastService
{
    private readonly List<ToastMessage> _messages = [];

    public event Action? OnChange;

    public IReadOnlyList<ToastMessage> Messages => _messages;

    public void ShowInfo(string title, string message) => AddToast(title, message, ToastLevel.Info);
    public void ShowSuccess(string title, string message) => AddToast(title, message, ToastLevel.Success);
    public void ShowWarning(string title, string message) => AddToast(title, message, ToastLevel.Warning);
    public void ShowDanger(string title, string message) => AddToast(title, message, ToastLevel.Danger);

    public void Dismiss(Guid id)
    {
        var toast = _messages.FirstOrDefault(t => t.Id == id);
        if (toast is not null)
        {
            _messages.Remove(toast);
            OnChange?.Invoke();
        }
    }

    private void AddToast(string title, string message, ToastLevel level)
    {
        _messages.Add(new ToastMessage(Guid.NewGuid(), title, message, level, DateTimeOffset.UtcNow));
        OnChange?.Invoke();
    }
}

public enum ToastLevel
{
    Info,
    Success,
    Warning,
    Danger
}

public record ToastMessage(Guid Id, string Title, string Message, ToastLevel Level, DateTimeOffset Timestamp);
