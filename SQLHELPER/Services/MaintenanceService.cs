using Dapper;
using SQLHELPER.Domain;
using SQLHELPER.Infrastructure.Data;
using SQLHELPER.Infrastructure.Data.Repos;

namespace SQLHELPER.Services;

public class MaintenanceService
{
    private readonly DbConnectionFactory _connectionFactory;
    private readonly RunRepository _runs;
    private readonly RunStepRepository _steps;

    public MaintenanceService(DbConnectionFactory connectionFactory, RunRepository runs, RunStepRepository steps)
    {
        _connectionFactory = connectionFactory;
        _runs = runs;
        _steps = steps;
    }

    public Task<Run> RebuildIndexesAsync(Server server, DbTarget target)
    {
        const string script = "EXEC sp_MSforeachtable 'ALTER INDEX ALL ON ? REBUILD WITH (ONLINE = ON)';";
        return ExecuteMaintenanceAsync(server, target, "Indexes", script, "Reconstruir índices");
    }

    public Task<Run> UpdateStatisticsAsync(Server server, DbTarget target)
    {
        const string script = "EXEC sp_updatestats;";
        return ExecuteMaintenanceAsync(server, target, "Statistics", script, "Actualizar estadísticas");
    }

    public Task<Run> RunCheckDbAsync(Server server, DbTarget target)
    {
        const string script = "DBCC CHECKDB WITH NO_INFOMSGS";
        return ExecuteMaintenanceAsync(server, target, "CHECKDB", script, "CHECKDB");
    }

    private async Task<Run> ExecuteMaintenanceAsync(Server server, DbTarget target, string runType, string script, string stepName)
    {
        var run = new Run
        {
            ServerId = server.Id,
            TargetId = target.Id,
            RunType = runType,
            Status = "Running",
            StartedAt = DateTime.UtcNow
        };

        run.Id = await _runs.InsertAsync(run);
        var step = new RunStep
        {
            RunId = run.Id,
            StepName = stepName,
            Status = "Running",
            StartedAt = DateTime.UtcNow
        };

        step.Id = await _steps.InsertAsync(step);
        try
        {
            await using var connection = _connectionFactory.CreateTargetConnection(server, target.DatabaseName);
            await connection.OpenAsync();
            await connection.ExecuteAsync(script);

            step.Status = "Succeeded";
            step.CompletedAt = DateTime.UtcNow;
            run.Status = "Succeeded";
            run.CompletedAt = DateTime.UtcNow;
        }
        catch (Exception ex)
        {
            step.Status = "Failed";
            step.CompletedAt = DateTime.UtcNow;
            step.Details = ex.Message;
            run.Status = "Failed";
            run.CompletedAt = DateTime.UtcNow;
            run.Message = ex.Message;
        }

        await _steps.UpdateAsync(step);
        await _runs.UpdateAsync(run);
        return run;
    }
}
