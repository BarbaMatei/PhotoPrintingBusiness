using FluentAssertions;
using Npgsql;
using PhotoPrint.Tests.Helpers;
using Xunit;

namespace PhotoPrint.Tests.Unit.Data;

/// <summary>
/// Every upload/preview test runs on the InMemory provider, which ignores DDL entirely — so a
/// broken column definition would ship green. These apply the real migration chain to a
/// PostgreSQL database and assert the resulting Uploads columns.
/// </summary>
public class UploadMigrationSchemaTests : IDisposable
{
    private readonly PostgresTestDatabase _database = new();

    public void Dispose() => _database.Dispose();

    private (string type, bool notNull)? Column(string columnName)
    {
        using var connection = new NpgsqlConnection(_database.ConnectionString);
        connection.Open();
        using var cmd = connection.CreateCommand();
        cmd.CommandText =
            "SELECT data_type, is_nullable FROM information_schema.columns " +
            "WHERE table_schema = 'public' AND table_name = 'Uploads' AND column_name = @name;";
        cmd.Parameters.AddWithValue("name", columnName);

        using var reader = cmd.ExecuteReader();
        if (!reader.Read())
            return null;

        return (reader.GetString(0), reader.GetString(1) == "NO");
    }

    [Fact]
    public void Migrate_CreatesNullableUploadsThumbnailPathColumn()
    {
        var column = Column("ThumbnailPath");

        column.Should().NotBeNull("the migration chain must add Uploads.ThumbnailPath");
        column!.Value.notNull.Should().BeFalse(
            "ThumbnailPath is nullable until a preview is generated");
    }

    [Fact]
    public void Migrate_LeavesUploadsFilePathNullable()
    {
        // The original-purge sets FilePath=null then SaveChanges. Purger tests use the InMemory
        // provider, where null is always allowed regardless of DDL, so a regression here would
        // surface only as silently-Failed purges in production.
        var column = Column("FilePath");

        column.Should().NotBeNull("Uploads.FilePath must exist");
        column!.Value.notNull.Should().BeFalse(
            "the purge nulls FilePath, so the column must not be NOT NULL");
    }
}
