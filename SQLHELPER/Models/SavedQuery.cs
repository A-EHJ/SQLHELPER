namespace SQLHELPER.Models;

public class SavedQuery
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Title { get; set; } = string.Empty;

    public string Statement { get; set; } = string.Empty;

    public string? Tags { get; set; }

    public string? DatabaseName { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}
