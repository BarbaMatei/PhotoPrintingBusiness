using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using PhotoPrint.API.Data;
using Xunit;

namespace PhotoPrint.Tests.Unit.Data;

/// <summary>
/// M9 (review 042-v4): every upload/preview test uses the InMemory provider (ignores migrations)
/// and the SQLite tests use EnsureCreated (the model, not migrations), so the
/// AddUploadThumbnailPath migration DDL — including a typo in Up() — was exercised by NO test and
/// could ship green. This applies the REAL migration chain to a SQLite database and asserts the
/// column lands. The Npgsql "character varying(512)" arm stays deferred to the 3-env/Testcontainers
/// phase (DB-1) — this covers the SQLite arm and the Up()/Down() DDL running at all.
/// </summary>
public class UploadThumbnailPathMigrationTests : IDisposable
{
    private readonly SqliteConnection _connection;

    public UploadThumbnailPathMigrationTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
    }

    public void Dispose() => _connection.Dispose();

    private (string name, string type, bool notNull)? Column(string columnName)
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "SELECT name, type, \"notnull\" FROM pragma_table_info('Uploads');";
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            if (reader.GetString(0) == columnName)
                return (reader.GetString(0), reader.GetString(1), reader.GetInt32(2) == 1);
        }
        return null;
    }

    private void ApplyMigrations()
    {
        var opts = new DbContextOptionsBuilder<PhotoPrintDbContext>()
            .UseSqlite(_connection)
            .Options;
        using var db = new PhotoPrintDbContext(opts);
        db.Database.Migrate();
    }

    [Fact]
    public void Migrate_OnSqlite_CreatesNullableUploadsThumbnailPathColumn()
    {
        ApplyMigrations();

        var column = Column("ThumbnailPath");
        column.Should().NotBeNull("the AddUploadThumbnailPath migration must add the column");
        column!.Value.notNull.Should().BeFalse("ThumbnailPath is nullable until a preview is generated");
    }

    [Fact]
    public void Migrate_OnSqlite_MakesUploadsFilePathNullable()
    {
        // F7 (review 043-v1): the original-purge (bolt 052) sets FilePath=null then SaveChanges,
        // which requires the MakeUploadFilePathNullable migration's NOT-NULL drop to have run.
        // Purger tests use the InMemory provider (null always allowed regardless of DDL), so a
        // regression in this migration would surface only as silently-Failed purges in prod.
        // Assert the real migration chain leaves Uploads.FilePath nullable.
        ApplyMigrations();

        var column = Column("FilePath");
        column.Should().NotBeNull("Uploads.FilePath must exist");
        column!.Value.notNull.Should().BeFalse(
            "MakeUploadFilePathNullable must drop the NOT NULL constraint so the purge can null it");
    }
}
