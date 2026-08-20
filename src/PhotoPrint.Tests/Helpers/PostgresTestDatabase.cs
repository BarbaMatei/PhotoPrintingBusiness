using Microsoft.EntityFrameworkCore;
using Npgsql;
using PhotoPrint.API.Data;

namespace PhotoPrint.Tests.Helpers;

/// <summary>
/// A throwaway PostgreSQL database scoped to one test class: created on construction with the
/// migration chain applied, dropped on <see cref="Dispose"/>. Every context handed out points at
/// the same database, so tests that need two contexts observe each other's writes.
/// </summary>
public sealed class PostgresTestDatabase : IDisposable
{
    public const string ConnectionEnvVar = "POSTGRES_TEST_CONNECTION";

    private const string DefaultAdminConnectionString =
        "Host=localhost;Port=5432;Username=postgres;Password=postgres;Database=postgres";

    private readonly string _adminConnectionString;
    private readonly string _databaseName;
    private bool _dropped;

    public string ConnectionString { get; }

    public PostgresTestDatabase()
    {
        _adminConnectionString =
            Environment.GetEnvironmentVariable(ConnectionEnvVar) ?? DefaultAdminConnectionString;
        _databaseName = "pp_test_" + Guid.NewGuid().ToString("N");

        try
        {
            ExecuteOnAdmin($"CREATE DATABASE \"{_databaseName}\"");
        }
        catch (NpgsqlException ex)
        {
            throw new InvalidOperationException(
                $"These tests need a reachable PostgreSQL server. Tried '{Redact(_adminConnectionString)}'. " +
                $"Start PostgreSQL locally or set {ConnectionEnvVar} to a connection string whose role " +
                "may CREATE DATABASE.", ex);
        }

        ConnectionString = new NpgsqlConnectionStringBuilder(_adminConnectionString)
        {
            Database = _databaseName,
        }.ConnectionString;

        using var db = NewContext();
        db.Database.Migrate();
    }

    public DbContextOptions<PhotoPrintDbContext> Options =>
        new DbContextOptionsBuilder<PhotoPrintDbContext>()
            .UseNpgsql(ConnectionString)
            .Options;

    public PhotoPrintDbContext NewContext() => new(Options);

    /// <summary>
    /// Drops every foreign key in this database, for tests that insert a row without its
    /// parents. Postgres has no per-connection FK switch, so the constraints themselves go.
    /// </summary>
    public void DropAllForeignKeys()
    {
        const string sql = """
            DO $$
            DECLARE r record;
            BEGIN
              FOR r IN
                SELECT conrelid::regclass AS tbl, conname
                FROM pg_constraint WHERE contype = 'f'
              LOOP
                EXECUTE format('ALTER TABLE %s DROP CONSTRAINT %I', r.tbl, r.conname);
              END LOOP;
            END $$;
            """;

        Execute(sql);
    }

    /// <summary>Runs raw SQL against this database — schema surgery for failure-path tests.</summary>
    public void Execute(string sql)
    {
        using var db = NewContext();
        db.Database.ExecuteSqlRaw(sql);
    }

    /// <summary>
    /// Makes subsequent order reads and writes fail, standing in for an unreachable database.
    /// </summary>
    public void BreakOrdersTable() => Execute("DROP TABLE \"Orders\" CASCADE");

    public void Dispose()
    {
        if (_dropped) return;
        _dropped = true;

        // Pooled connections to the test database would block the DROP.
        NpgsqlConnection.ClearAllPools();
        try
        {
            ExecuteOnAdmin($"DROP DATABASE IF EXISTS \"{_databaseName}\" WITH (FORCE)");
        }
        catch (NpgsqlException)
        {
            // A leaked database is noise on a dev box, not a test failure.
        }
    }

    private void ExecuteOnAdmin(string sql)
    {
        using var connection = new NpgsqlConnection(_adminConnectionString);
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.ExecuteNonQuery();
    }

    private static string Redact(string connectionString) =>
        new NpgsqlConnectionStringBuilder(connectionString) { Password = "***" }.ConnectionString;
}
