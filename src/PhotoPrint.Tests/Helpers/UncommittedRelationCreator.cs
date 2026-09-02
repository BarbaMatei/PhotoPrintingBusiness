using Npgsql;

namespace PhotoPrint.Tests.Helpers;

public sealed class UncommittedRelationCreator : IAsyncDisposable
{
    private readonly NpgsqlConnection _connection;
    private readonly NpgsqlTransaction _transaction;
    private readonly string _connectionString;
    private readonly int _creatorPid;
    private bool _committed;
    private bool _disposed;

    private UncommittedRelationCreator(
        NpgsqlConnection connection, NpgsqlTransaction transaction, string connectionString)
    {
        _connection = connection;
        _transaction = transaction;
        _connectionString = connectionString;
        _creatorPid = connection.ProcessID;
    }

    public static Task<UncommittedRelationCreator> SequenceAsync(
        string connectionString, string sequenceName) =>
        StartAsync(connectionString, $"CREATE SEQUENCE \"{sequenceName}\" START 1");

    public static Task<UncommittedRelationCreator> TableAsync(
        string connectionString, string tableName) =>
        StartAsync(connectionString, $"CREATE TABLE \"{tableName}\" (id integer)");

    private static async Task<UncommittedRelationCreator> StartAsync(
        string connectionString, string ddl)
    {
        var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        var transaction = await connection.BeginTransactionAsync();

        try
        {
            await using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = ddl;
            await command.ExecuteNonQueryAsync();
        }
        catch
        {
            await transaction.DisposeAsync();
            await connection.DisposeAsync();
            throw;
        }

        return new UncommittedRelationCreator(connection, transaction, connectionString);
    }

    public async Task WaitUntilAnotherBackendBlocksAsync(TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow + timeout;

        // pg_stat_activity is snapshotted per transaction, so the poll needs its own autocommit connection.
        await using var observer = new NpgsqlConnection(_connectionString);
        await observer.OpenAsync();

        while (DateTime.UtcNow < deadline)
        {
            await using var command = observer.CreateCommand();
            command.CommandText = """
                SELECT count(*) FROM pg_stat_activity
                WHERE datname = current_database()
                  AND pid <> pg_backend_pid()
                  AND pid <> @creator
                  AND cardinality(pg_blocking_pids(pid)) > 0
                """;
            command.Parameters.AddWithValue("creator", _creatorPid);

            if (Convert.ToInt64(await command.ExecuteScalarAsync()) > 0)
                return;

            await Task.Delay(25);
        }

        throw new InvalidOperationException(
            $"No other backend blocked on the catalogue row within {timeout.TotalSeconds:0.#} s, " +
            "so the create race was never reached and this test proves nothing.");
    }

    public async Task CommitAsync()
    {
        await _transaction.CommitAsync();
        _committed = true;
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;

        if (!_committed)
            await _transaction.RollbackAsync();

        await _transaction.DisposeAsync();
        await _connection.DisposeAsync();
    }
}
