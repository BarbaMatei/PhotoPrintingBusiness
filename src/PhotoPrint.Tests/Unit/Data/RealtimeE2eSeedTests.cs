using System.Text.RegularExpressions;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using PhotoPrint.API.Data;
using PhotoPrint.API.Data.Seed;
using PhotoPrint.API.Models;
using PhotoPrint.Tests.Helpers;

namespace PhotoPrint.Tests.Unit.Data;

public class RealtimeE2eSeedTests
{
    [Fact]
    public async Task DevSeed_ShipsAPaidOrderForEveryAttemptTheRealtimeSpecCanMake()
    {
        var pinned   = PinnedOrderNumbers();
        var attempts = CiAttempts();

        pinned.Count.Should().BeGreaterThanOrEqualTo(
            attempts,
            "the realtime spec turns one Paid order into Printing and nothing turns it back, so a "
            + "CI job running {0} attempt(s) needs that many pinned orders or the retry is doomed",
            attempts);

        await using var db = NewContext();
        await DevDataSeed.ApplyAsync(db);

        var paid = await db.Orders
            .Where(o => o.Status == OrderStatus.Paid)
            .Select(o => o.OrderNumber)
            .ToListAsync();

        paid.Should().Contain(
            pinned,
            "the spec drives only the order numbers it pins, so every one of them must be seeded "
            + "in Paid — otherwise the run fails on a target it can never find");
    }

    private static PhotoPrintDbContext NewContext() =>
        new(new DbContextOptionsBuilder<PhotoPrintDbContext>()
            .UseInMemoryDatabase($"dev-seed-{Guid.NewGuid()}")
            .Options);

    private static List<string> PinnedOrderNumbers()
    {
        var spec = RepoFiles.ReadAllText("src", "PhotoPrint.UI", "e2e", "realtime-order.spec.ts");

        return Regex.Matches(spec, @"FT-\d{4}-\d{4}")
            .Select(m => m.Value)
            .Distinct()
            .ToList();
    }

    private static int CiAttempts()
    {
        var config = RepoFiles.ReadAllText("src", "PhotoPrint.UI", "playwright.config.ts");
        var match  = Regex.Match(config, @"retries:\s*isCi\s*\?\s*(\d+)\s*:\s*(\d+)");

        match.Success.Should().BeTrue("the CI retry count decides how many Paid orders a run consumes");
        return int.Parse(match.Groups[1].Value) + 1;
    }
}
