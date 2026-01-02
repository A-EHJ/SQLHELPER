using Dapper;
using SQLHELPER.Domain;
using SQLHELPER.Infrastructure.Data;

namespace SQLHELPER.Services;

public class HealthService
{
    private readonly DbConnectionFactory _connectionFactory;

    public HealthService(DbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<IEnumerable<dynamic>> GetBlockingSessionsAsync(Server server)
    {
        const string sql = @"SELECT blocking_session_id AS BlockingSessionId, session_id AS SessionId, wait_type, wait_time, last_wait_type, percent_complete
FROM sys.dm_exec_requests WHERE blocking_session_id <> 0";
        await using var connection = _connectionFactory.CreateTargetConnection(server, "master");
        return await connection.QueryAsync(sql);
    }

    public async Task<IEnumerable<dynamic>> GetTopCpuQueriesAsync(Server server, int top = 10)
    {
        const string sql = @"SELECT TOP(@Top) qs.total_worker_time / qs.execution_count AS AverageCpu, qs.execution_count, DB_NAME(qs.database_id) AS DatabaseName,
    SUBSTRING(st.text, (qs.statement_start_offset/2)+1, ((CASE qs.statement_end_offset WHEN -1 THEN DATALENGTH(st.text) ELSE qs.statement_end_offset END - qs.statement_start_offset)/2)+1) AS QueryText
FROM sys.dm_exec_query_stats qs
CROSS APPLY sys.dm_exec_sql_text(qs.sql_handle) st
ORDER BY AverageCpu DESC";
        await using var connection = _connectionFactory.CreateTargetConnection(server, "master");
        return await connection.QueryAsync(sql, new { Top = top });
    }

    public async Task<IEnumerable<dynamic>> GetDatabaseSizesAsync(Server server)
    {
        const string sql = @"SELECT DB_NAME(database_id) AS DatabaseName, CAST(SUM(size) * 8 / 1024.0 AS DECIMAL(12,2)) AS SizeMb
FROM sys.master_files
GROUP BY database_id
ORDER BY DatabaseName";
        await using var connection = _connectionFactory.CreateTargetConnection(server, "master");
        return await connection.QueryAsync(sql);
    }

    public async Task<IEnumerable<dynamic>> GetFailedJobsAsync(Server server, int days = 7)
    {
        const string sql = @"SELECT j.name AS JobName, h.run_date, h.run_time, h.message
FROM msdb.dbo.sysjobhistory h
INNER JOIN msdb.dbo.sysjobs j ON h.job_id = j.job_id
WHERE h.run_status = 0 AND h.run_date >= CONVERT(INT, CONVERT(VARCHAR(8), DATEADD(DAY, -@Days, GETDATE()), 112))
ORDER BY h.run_date DESC, h.run_time DESC";
        await using var connection = _connectionFactory.CreateTargetConnection(server, "msdb");
        return await connection.QueryAsync(sql, new { Days = days });
    }
}
