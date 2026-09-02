using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;
using PhotoPrint.API.Data;

namespace PhotoPrint.Tests.Helpers;

public class PostgresTestDatabase : IDisposable
{
    public const string ConnectionEnvVar = "POSTGRES_TEST_CONNECTION";

    private const string DefaultAdminConnectionString =
        "Host=localhost;Port=5432;Username=postgres;Password=postgres;Database=postgres";

    private const int MaxPoolSlots = 64;
    private const string InsufficientPrivilege = "42501";
    private const string ForeignKeyViolation = "23503";

    private static readonly object SweepGate = new();
    private static string? _fingerprint;
    private static string? _salt;
    private static bool _swept;
    private static bool _fastResetUnavailable;
    private static string? _migratedSequences;
    private static string? _migrationScript;

    private readonly string _adminConnectionString;
    private readonly string _databaseName;
    private readonly NpgsqlConnection? _lease;
    private bool _disposed;
    private bool _schemaTouched;

    public string ConnectionString { get; }

    public PostgresTestDatabase() : this(dropForeignKeys: false)
    {
    }

    protected PostgresTestDatabase(bool dropForeignKeys)
    {
        _adminConnectionString =
            Environment.GetEnvironmentVariable(ConnectionEnvVar) ?? DefaultAdminConnectionString;

        try
        {
            SweepOnce(_adminConnectionString);
            (_databaseName, _lease) = LeaseSlot(_adminConnectionString, dropForeignKeys);
            ConnectionString = ConnectionStringFor(_adminConnectionString, _databaseName);
        }
        catch (NpgsqlException ex)
        {
            throw new InvalidOperationException(UnreachableMessage(_adminConnectionString), ex);
        }
    }

    private PostgresTestDatabase(string adminConnectionString)
    {
        _adminConnectionString = adminConnectionString;
        _databaseName = "pp_tmp_" + Guid.NewGuid().ToString("N");

        try
        {
            ExecuteOnAdmin(_adminConnectionString, $"CREATE DATABASE \"{_databaseName}\"");
            ConnectionString = ConnectionStringFor(_adminConnectionString, _databaseName);

            using var db = NewContext();
            db.Database.Migrate();
        }
        catch (NpgsqlException ex)
        {
            throw new InvalidOperationException(UnreachableMessage(_adminConnectionString), ex);
        }
    }

    public static PostgresTestDatabase Throwaway() =>
        new(Environment.GetEnvironmentVariable(ConnectionEnvVar) ?? DefaultAdminConnectionString);

    public DbContextOptions<PhotoPrintDbContext> Options =>
        new DbContextOptionsBuilder<PhotoPrintDbContext>()
            .UseNpgsql(ConnectionString)
            .Options;

    public PhotoPrintDbContext NewContext() => new(Options);

    public void ResetForTest()
    {
        if (_fastResetUnavailable)
        {
            TruncateEverything();
            ResetSequences();
            return;
        }

        const string sql = """
            DO $$
            DECLARE t text;
            BEGIN
              SET LOCAL session_replication_role = replica;

              FOR t IN SELECT format('%I.%I', schemaname, tablename)
                         FROM pg_tables
                        WHERE schemaname = 'public' AND tablename <> '__EFMigrationsHistory'
              LOOP
                EXECUTE 'DELETE FROM ' || t;
              END LOOP;
            END $$;
            """;

        try
        {
            ExecuteInternal(sql);
        }
        catch (PostgresException ex)
            when (ex.SqlState is InsufficientPrivilege or ForeignKeyViolation)
        {
            // Switching foreign keys off for the wipe needs a superuser.
            _fastResetUnavailable = true;
            TruncateEverything();
        }

        ResetSequences();
    }

    private void ResetSequences()
    {
        // Per-year sequences are created on first use, so one left behind makes the next test's
        // create a duplicate; only the ones the migration chain ships may survive a reset.
        var sql = $"""
            DO $$
            DECLARE s record;
            BEGIN
              FOR s IN SELECT schemaname, sequencename FROM pg_sequences WHERE schemaname = 'public'
              LOOP
                IF s.sequencename = ANY ({MigratedSequences()}) THEN
                  EXECUTE format('ALTER SEQUENCE %I.%I RESTART', s.schemaname, s.sequencename);
                ELSE
                  EXECUTE format('DROP SEQUENCE %I.%I CASCADE', s.schemaname, s.sequencename);
                END IF;
              END LOOP;
            END $$;
            """;

        ExecuteInternal(sql);
    }

    // Postgres has no per-connection FK switch, so the constraints themselves go.
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

        ExecuteInternal(sql);
    }

    public void Execute(string sql)
    {
        _schemaTouched = true;
        ExecuteInternal(sql);
    }

    public void BreakOrdersTable() => Execute("DROP TABLE \"Orders\" CASCADE");

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        if (_lease is null)
        {
            Drop();
            return;
        }

        // A test that changed the schema cannot hand the database on to the next class.
        if (_schemaTouched) Drop();
        _lease.Dispose();
    }

    private void Drop()
    {
        // Pooled connections to the test database would block the DROP.
        NpgsqlConnection.ClearAllPools();
        try
        {
            ExecuteOnAdmin(
                _adminConnectionString, $"DROP DATABASE IF EXISTS \"{_databaseName}\" WITH (FORCE)");
        }
        catch (NpgsqlException)
        {
            // A leaked database is noise on a dev box, not a test failure.
        }
    }

    private (string Name, NpgsqlConnection Lease) LeaseSlot(
        string adminConnectionString, bool dropForeignKeys)
    {
        var prefix = $"pp_test_{Salt()}_{Fingerprint(adminConnectionString)}_{(dropForeignKeys ? "nofk" : "std")}";

        for (var slot = 1; slot <= MaxPoolSlots; slot++)
        {
            var name = $"{prefix}_{slot:D2}";
            var lease = new NpgsqlConnection(adminConnectionString);
            lease.Open();

            if (!TryLock(lease, name))
            {
                lease.Dispose();
                continue;
            }

            try
            {
                var slotConnectionString = ConnectionStringFor(adminConnectionString, name);

                if (DatabaseExists(adminConnectionString, name))
                {
                    // An interrupted first use leaves the database created but unmigrated, and
                    // truncating that hands on a slot with no tables at all.
                    EnsureSchemaApplied(slotConnectionString, dropForeignKeys);
                    TruncateEverything(slotConnectionString);
                }
                else
                {
                    ExecuteOnAdmin(adminConnectionString, $"CREATE DATABASE \"{name}\"");
                    try
                    {
                        EnsureSchemaApplied(slotConnectionString, dropForeignKeys);
                    }
                    catch
                    {
                        // Leaving it behind would poison the slot for every later run.
                        TryDropDatabase(adminConnectionString, name);
                        throw;
                    }
                }

                return (name, lease);
            }
            catch
            {
                lease.Dispose();
                throw;
            }
        }

        throw new InvalidOperationException(
            $"All {MaxPoolSlots} pooled test databases are leased. Either test parallelism grew " +
            "past the pool, or leases are being held open by a stuck process.");
    }

    internal static void EnsureSchemaApplied(string slotConnectionString, bool dropForeignKeys)
    {
        var options = new DbContextOptionsBuilder<PhotoPrintDbContext>()
            .UseNpgsql(slotConnectionString)
            .Options;
        using var db = new PhotoPrintDbContext(options);
        if (!db.Database.GetPendingMigrations().Any()) return;

        db.Database.Migrate();
        if (dropForeignKeys)
            ExecuteOn(slotConnectionString, DropForeignKeysSql);
    }

    private static void TryDropDatabase(string adminConnectionString, string name)
    {
        try
        {
            ExecuteOnAdmin(adminConnectionString, $"DROP DATABASE IF EXISTS \"{name}\" WITH (FORCE)");
        }
        catch
        {
            /* the sweep collects it later */
        }
    }

    private static bool TryLock(NpgsqlConnection lease, string name)
    {
        using var command = lease.CreateCommand();
        command.CommandText = "SELECT pg_try_advisory_lock(@key)";
        command.Parameters.AddWithValue("key", LockKey(name));

        return command.ExecuteScalar() is true;
    }

    private static long LockKey(string name) =>
        BitConverter.ToInt64(SHA256.HashData(Encoding.UTF8.GetBytes("pp_test_lease:" + name)), 0);

    private static void SweepOnce(string adminConnectionString)
    {
        lock (SweepGate)
        {
            if (_swept) return;
            _swept = true;

            var current = $"pp_test_{Salt()}_{Fingerprint(adminConnectionString)}_";
            var stale = new List<string>();

            using (var connection = new NpgsqlConnection(adminConnectionString))
            {
                connection.Open();
                using var command = connection.CreateCommand();
                command.CommandText =
                    """
                    SELECT d.datname
                      FROM pg_database d
                     WHERE d.datname LIKE @prefix || '%'
                       AND d.datname NOT LIKE @current || '%'
                       AND NOT EXISTS (SELECT 1 FROM pg_stat_activity a WHERE a.datname = d.datname)
                    """;
                command.Parameters.AddWithValue("prefix", $"pp_test_{Salt()}_");
                command.Parameters.AddWithValue("current", current);

                using var reader = command.ExecuteReader();
                while (reader.Read()) stale.Add(reader.GetString(0));
            }

            foreach (var name in stale)
            {
                // A concurrent run on an older schema still holds its lease.
                using var lease = new NpgsqlConnection(adminConnectionString);
                lease.Open();
                if (!TryLock(lease, name)) continue;

                try
                {
                    ExecuteOnAdmin(adminConnectionString, $"DROP DATABASE IF EXISTS \"{name}\" WITH (FORCE)");
                }
                catch (NpgsqlException)
                {
                }
            }
        }
    }

    private static string Salt()
    {
        if (_salt is not null) return _salt;

        var root = Path.GetDirectoryName(typeof(PostgresTestDatabase).Assembly.Location)
                   ?? Environment.CurrentDirectory;

        // Normalised: the same directory reached as d:\… and D:/… would otherwise salt two pools,
        // doubling the databases and leaving each one's sweep blind to the other.
        var normalised = Path.GetFullPath(root).Replace('\\', '/').TrimEnd('/').ToLowerInvariant();

        return _salt = Hash(normalised, 8);
    }

    private static string Fingerprint(string adminConnectionString)
    {
        if (_fingerprint is not null) return _fingerprint;

        var options = new DbContextOptionsBuilder<PhotoPrintDbContext>()
            .UseNpgsql(adminConnectionString)
            .Options;
        using var db = new PhotoPrintDbContext(options);

        // A migration edited in place keeps its id while producing a different schema.
        var schema = string.Join('|', db.Database.GetMigrations()) + MigrationScript(adminConnectionString);

        return _fingerprint = Hash(schema, 12);
    }

    private static string MigratedSequences()
    {
        if (_migratedSequences is not null) return _migratedSequences;

        var script = MigrationScript(
            Environment.GetEnvironmentVariable(ConnectionEnvVar) ?? DefaultAdminConnectionString);

        var names = Regex
            .Matches(script,
                "CREATE SEQUENCE (?:IF NOT EXISTS )?\"?([A-Za-z0-9_]+)\"?", RegexOptions.IgnoreCase)
            .Select(m => m.Groups[1].Value)
            .Distinct(StringComparer.Ordinal)
            .Select(n => $"'{n.Replace("'", "''")}'");

        return _migratedSequences = $"ARRAY[{string.Join(", ", names)}]::text[]";
    }

    private static string MigrationScript(string adminConnectionString)
    {
        if (_migrationScript is not null) return _migrationScript;

        var options = new DbContextOptionsBuilder<PhotoPrintDbContext>()
            .UseNpgsql(adminConnectionString)
            .Options;
        using var db = new PhotoPrintDbContext(options);

        // The migrations' own SQL, not the model's: sequences and the invoice index are raw
        // statements the model knows nothing about.
        return _migrationScript = db.GetService<IMigrator>().GenerateScript();
    }

    private static string Hash(string value, int length) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)))[..length].ToLowerInvariant();

    private const string DropForeignKeysSql = """
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

    private void TruncateEverything() => TruncateEverything(ConnectionString);

    private static void TruncateEverything(string connectionString)
    {
        const string sql = """
            DO $$
            DECLARE tables text; s record;
            BEGIN
              SELECT string_agg(format('%I.%I', schemaname, tablename), ', ')
                INTO tables
                FROM pg_tables
               WHERE schemaname = 'public' AND tablename <> '__EFMigrationsHistory';

              IF tables IS NOT NULL THEN
                EXECUTE 'TRUNCATE TABLE ' || tables || ' RESTART IDENTITY CASCADE';
              END IF;

              -- No table column owns the numbering sequences, so RESTART IDENTITY skips them.
              FOR s IN SELECT schemaname, sequencename FROM pg_sequences WHERE schemaname = 'public'
              LOOP
                EXECUTE format('ALTER SEQUENCE %I.%I RESTART', s.schemaname, s.sequencename);
              END LOOP;
            END $$;
            """;

        ExecuteOn(connectionString, sql);
    }

    private void ExecuteInternal(string sql) => ExecuteOn(ConnectionString, sql);

    private static void ExecuteOn(string connectionString, string sql)
    {
        using var connection = new NpgsqlConnection(connectionString);
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.ExecuteNonQuery();
    }

    private static bool DatabaseExists(string adminConnectionString, string name)
    {
        using var connection = new NpgsqlConnection(adminConnectionString);
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT 1 FROM pg_database WHERE datname = @name";
        command.Parameters.AddWithValue("name", name);

        return command.ExecuteScalar() is not null;
    }

    private static string ConnectionStringFor(string adminConnectionString, string databaseName) =>
        new NpgsqlConnectionStringBuilder(adminConnectionString) { Database = databaseName }.ConnectionString;

    private static void ExecuteOnAdmin(string adminConnectionString, string sql) =>
        ExecuteOn(adminConnectionString, sql);

    private static string UnreachableMessage(string adminConnectionString) =>
        $"These tests need a reachable PostgreSQL server. Tried '{Redact(adminConnectionString)}'. " +
        $"Start PostgreSQL locally or set {ConnectionEnvVar} to a connection string whose role " +
        "may CREATE DATABASE.";

    private static string Redact(string connectionString) =>
        new NpgsqlConnectionStringBuilder(connectionString) { Password = "***" }.ConnectionString;
}

public sealed class ForeignKeyFreeTestDatabase : PostgresTestDatabase
{
    public ForeignKeyFreeTestDatabase() : base(dropForeignKeys: true)
    {
    }
}
