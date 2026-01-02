using Dapper;
using SQLHELPER.Domain;

namespace SQLHELPER.Infrastructure.Data.Repos;

public class NoteRepository
{
    private readonly DbConnectionFactory _connectionFactory;

    public NoteRepository(DbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<IEnumerable<Note>> GetForServerAsync(int serverId)
    {
        const string sql = "SELECT * FROM Notes WHERE ServerId = @ServerId ORDER BY CreatedAt DESC";
        await using var connection = _connectionFactory.CreateHubConnection();
        return await connection.QueryAsync<Note>(sql, new { ServerId = serverId });
    }

    public async Task<int> InsertAsync(Note note)
    {
        const string sql = @"INSERT INTO Notes (ServerId, TargetId, Title, Body, CreatedBy, CreatedAt)
VALUES (@ServerId, @TargetId, @Title, @Body, @CreatedBy, @CreatedAt);
SELECT CAST(SCOPE_IDENTITY() AS int);";
        await using var connection = _connectionFactory.CreateHubConnection();
        note.CreatedAt = note.CreatedAt == default ? DateTime.UtcNow : note.CreatedAt;
        return await connection.ExecuteScalarAsync<int>(sql, note);
    }
}
