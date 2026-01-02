namespace SQLHELPER.Models;

public record QueryResultSet(IReadOnlyList<string> Columns, IReadOnlyList<IReadOnlyDictionary<string, object?>> Rows);

public record QueryExecutionResult(bool Success, IReadOnlyList<QueryResultSet> ResultSets, string Message, string? Error);
