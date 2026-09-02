using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using PhotoPrint.API.Data;
using PhotoPrint.Tests.Helpers;
using Xunit;

namespace PhotoPrint.Tests.Integration;

public sealed class PostgresSequencesTests : IDisposable
{
    private readonly PostgresTestDatabase _database = new();

    public void Dispose() => _database.Dispose();

    [Fact]
    public async Task EnsureAsync_LosesTheRaceToATableOfTheSameName_RaisesInsteadOfSwallowing()
    {
        const string name = "held_by_a_table_2026";
        var rival = await UncommittedRelationCreator.TableAsync(_database.ConnectionString, name);

        var db = _database.NewContext();
        Task? ensure = null;
        Exception? failure;

        try
        {
            ensure = Task.Run(() => PostgresSequences.EnsureAsync(db.Database, name));

            await rival.WaitUntilAnotherBackendBlocksAsync(TimeSpan.FromSeconds(10));
            await rival.CommitAsync();

            failure = await Record.ExceptionAsync(() => ensure);
        }
        finally
        {
            await rival.DisposeAsync();
            if (ensure is not null)
                await Record.ExceptionAsync(() => ensure);
            await Record.ExceptionAsync(() => db.DisposeAsync().AsTask());
        }

        failure.Should().BeOfType<PostgresException>(
            "a name held by a table is not the duplicate this helper may swallow");
    }
}

public sealed class PostgresSequencesNameGuardTests
{
    [Theory]
    [InlineData("Order_Number_Seq_2026")]
    [InlineData("order number seq")]
    [InlineData("order_seq\"; DROP TABLE \"Orders\"; --")]
    [InlineData("")]
    public async Task EnsureAsync_UnusableName_ThrowsBeforeAnySql(string name)
    {
        using var db = new PhotoPrintDbContext(
            new DbContextOptionsBuilder<PhotoPrintDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options);

        var act = () => PostgresSequences.EnsureAsync(db.Database, name);

        await act.Should().ThrowAsync<ArgumentException>();
    }
}
