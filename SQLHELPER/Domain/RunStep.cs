using System;

namespace SQLHELPER.Domain;

public class RunStep
{
    public int Id { get; set; }
    public int RunId { get; set; }
    public string StepName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? Details { get; set; }
}
