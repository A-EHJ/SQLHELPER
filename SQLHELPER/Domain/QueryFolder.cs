using System;

namespace SQLHELPER.Domain;

public class QueryFolder
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int? ParentFolderId { get; set; }
}
