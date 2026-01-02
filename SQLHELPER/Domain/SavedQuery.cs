using System;

namespace SQLHELPER.Domain;

public class SavedQuery
{
    public int Id { get; set; }
    public int? FolderId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string SqlText { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsFavorite { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
