using System.Reflection;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PhotoPrint.Tests.Helpers;
using PhotoPrint.API.BackgroundJobs;
using PhotoPrint.API.Configuration;
using PhotoPrint.API.Data;
using PhotoPrint.API.Models;
using PhotoPrint.API.Services.Sameday;

namespace PhotoPrint.Tests.Unit.Services.Sameday;

/// <summary>
/// Unit tests for <see cref="AwbRetryJob"/>. Uses a real PostgreSQL database (not EF InMemory
/// provider) so the sweep's date arithmetic and the fresh-claim exclusion run
/// as real SQL, not LINQ-to-objects. The <c>RunOneTickAsync</c> private method
/// is invoked via reflection.
/// </summary>
public class AwbRetryJobTests : IClassFixture<PostgresTestDatabase>
{
    private readonly PostgresTestDatabase _database;

    public AwbRetryJobTests(PostgresTestDatabase database)
    {
        _database = database;
        database.TruncateAllTables();

        // FK enforcement off: these exercise the retry sweep, not referential integrity.
        _database.DropAllForeignKeys();
    }


    private PhotoPrintDbContext CreateDb() => _database.NewContext();

    private IServiceScopeFactory BuildScopes()
    {
        var services = new ServiceCollection();
        services.AddScoped(_ => CreateDb());
        services.AddLogging();
        return services.BuildServiceProvider().GetRequiredService<IServiceScopeFactory>();
    }

    private AwbRetryJob Build(
        IAwbJobQueue queue,
        AwbGiveUpRegistry giveUp,
        TimeProvider clock,
        int giveUpHours = 24,
        int retryIntervalMinutes = 60)
    {
        var settings = Options.Create(new SamedaySettings
        {
            Jobs = new SamedayJobsSettings
            {
                Enabled = true,
                AwbRetryIntervalMinutes = retryIntervalMinutes,
                AwbGiveUpHours = giveUpHours,
            },
        });
        return new AwbRetryJob(
            BuildScopes(), queue, giveUp, settings, clock,
            new LoggerFactory().CreateLogger<AwbRetryJob>());
    }

    private static Task RunOneTickAsync(AwbRetryJob job)
    {
        var method = typeof(AwbRetryJob).GetMethod("RunOneTickAsync",
            BindingFlags.NonPublic | BindingFlags.Instance);
        return (Task)method!.Invoke(job, new object[] { CancellationToken.None })!;
    }

    private Order SeedPaidOrder(
        DateTimeOffset paidAt,
        string? awbNumber = null,
        DateTimeOffset? claimedAt = null,
        OrderStatus status = OrderStatus.Paid)
    {
        var order = new Order
        {
            Id = Guid.NewGuid(),
            OrderNumber = $"FT-{Random.Shared.Next(100_000, 999_999)}",
            Status = status,
            AwbNumber = awbNumber,
            AwbClaimedAt = claimedAt,
            PaidAt = paidAt,
            DeliveryType = DeliveryType.Courier,
            ShippingAddress = new ShippingAddressSnapshot
            {
                RecipientName = "x", Phone = "x",
                Street = "x", Number = "1",
                City = "x", County = "x", PostalCode = "x",
            },
        };
        using var db = CreateDb();
        db.Orders.Add(order);
        db.SaveChanges();
        return order;
    }

    private sealed class TestQueue : IAwbJobQueue
    {
        public List<AwbJob> Enqueued { get; } = new();

        public ValueTask EnqueueAsync(AwbJob job, CancellationToken ct = default)
        {
            Enqueued.Add(job);
            return ValueTask.CompletedTask;
        }

        public async IAsyncEnumerable<AwbJob> DequeueAllAsync(
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct)
        {
            await Task.Yield();
            foreach (var job in Enqueued)
                yield return job;
        }
    }

    private static readonly DateTimeOffset T0 = new(2026, 6, 2, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Enqueues_orders_inside_the_24h_window()
    {
        // Paid 1h/2h ago — well inside the 24h window.
        SeedPaidOrder(paidAt: T0.AddHours(-1));
        SeedPaidOrder(paidAt: T0.AddHours(-2));

        var clock = new FakeTimeProvider(T0);
        var queue = new TestQueue();
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var sut = Build(queue, new AwbGiveUpRegistry(cache), clock);

        await RunOneTickAsync(sut);

        queue.Enqueued.Should().HaveCount(2);
        queue.Enqueued.Should().AllSatisfy(j => j.Attempt.Should().Be(1));
    }

    [Fact]
    public async Task Skips_orders_already_having_an_AwbNumber()
    {
        SeedPaidOrder(paidAt: T0.AddHours(-1), awbNumber: "RO12345678");

        var clock = new FakeTimeProvider(T0);
        var queue = new TestQueue();
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var sut = Build(queue, new AwbGiveUpRegistry(cache), clock);

        await RunOneTickAsync(sut);

        queue.Enqueued.Should().BeEmpty();
    }

    [Fact]
    public async Task Skips_orders_with_a_fresh_claim()
    {
        // A worker is actively creating the AWB (claim taken 1 min ago) — the sweep must not
        // churn a duplicate concurrent attempt against it.
        SeedPaidOrder(paidAt: T0.AddHours(-1), claimedAt: T0.AddMinutes(-1));

        var clock = new FakeTimeProvider(T0);
        var queue = new TestQueue();
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var sut = Build(queue, new AwbGiveUpRegistry(cache), clock);

        await RunOneTickAsync(sut);

        queue.Enqueued.Should().BeEmpty();
    }

    [Fact]
    public async Task Enqueues_orders_whose_claim_is_stale()
    {
        // A crashed worker left a claim 10 min ago (> the 5-min TTL) — the sweep must re-drive it.
        SeedPaidOrder(paidAt: T0.AddHours(-1), claimedAt: T0.AddMinutes(-10));

        var clock = new FakeTimeProvider(T0);
        var queue = new TestQueue();
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var sut = Build(queue, new AwbGiveUpRegistry(cache), clock);

        await RunOneTickAsync(sut);

        queue.Enqueued.Should().HaveCount(1);
    }

    [Fact]
    public async Task Logs_give_up_once_for_orders_outside_the_24h_window()
    {
        // Paid 25h ago — past the 24h give-up.
        var staleOrder = SeedPaidOrder(paidAt: T0.AddHours(-25));

        var clock = new FakeTimeProvider(T0);
        var queue = new TestQueue();
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var giveUp = new AwbGiveUpRegistry(cache);
        var sut = Build(queue, giveUp, clock);

        await RunOneTickAsync(sut);

        queue.Enqueued.Should().BeEmpty();
        giveUp.MarkOnce(staleOrder.Id).Should().BeFalse();
    }

    [Fact]
    public async Task Give_up_dedup_means_a_second_tick_does_not_re_log()
    {
        var staleOrder = SeedPaidOrder(paidAt: T0.AddHours(-25));

        var clock = new FakeTimeProvider(T0);
        var queue = new TestQueue();
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var giveUp = new AwbGiveUpRegistry(cache);
        var sut = Build(queue, giveUp, clock);

        await RunOneTickAsync(sut);
        await RunOneTickAsync(sut);   // second tick

        giveUp.MarkOnce(staleOrder.Id).Should().BeFalse();
    }

    [Fact]
    public async Task Gives_up_on_an_order_advanced_past_paid_before_its_label_existed()
    {
        var advanced = SeedPaidOrder(paidAt: T0.AddHours(-25), status: OrderStatus.Printing);

        var clock = new FakeTimeProvider(T0);
        var queue = new TestQueue();
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var giveUp = new AwbGiveUpRegistry(cache);
        var sut = Build(queue, giveUp, clock);

        await RunOneTickAsync(sut);

        giveUp.MarkOnce(advanced.Id).Should().BeFalse(
            "an admin moving the order on is not a reason for the only never-got-a-label alarm "
                + "to go silent");
        queue.Enqueued.Should().BeEmpty(
            "AWB creation still refuses any status but Paid, so re-enqueuing would only churn");
    }

    [Fact]
    public async Task Does_not_enqueue_orders_in_non_Paid_status()
    {
        SeedPaidOrder(paidAt: T0.AddHours(-1), status: OrderStatus.Cancelled);

        var clock = new FakeTimeProvider(T0);
        var queue = new TestQueue();
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var sut = Build(queue, new AwbGiveUpRegistry(cache), clock);

        await RunOneTickAsync(sut);

        queue.Enqueued.Should().BeEmpty();
    }
}
