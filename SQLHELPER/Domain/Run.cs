using System;

namespace SQLHELPER.Domain;

public class Run
{
    public int Id { get; set; }
    public int ServerId { get; set; }
    public int? TargetId { get; set; }
    public string RunType { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? Message { get; set; }
}
