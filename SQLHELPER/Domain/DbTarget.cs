using System;

namespace SQLHELPER.Domain;

public class DbTarget
{
    public int Id { get; set; }
    public int ServerId { get; set; }
    public string DatabaseName { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public string? Tags { get; set; }
    public DateTime CreatedAt { get; set; }
}
