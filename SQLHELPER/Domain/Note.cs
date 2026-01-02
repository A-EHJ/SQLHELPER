using System;

namespace SQLHELPER.Domain;

public class Note
{
    public int Id { get; set; }
    public int? ServerId { get; set; }
    public int? TargetId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
}
