using System;

namespace SQLHELPER.Domain;

public class QueryRun
{
    public int Id { get; set; }
    public int? SavedQueryId { get; set; }
    public int? TargetId { get; set; }
    public DateTime ExecutedAt { get; set; }
    public int? DurationMs { get; set; }
    public int? RowCount { get; set; }
    public string? Error { get; set; }
}
