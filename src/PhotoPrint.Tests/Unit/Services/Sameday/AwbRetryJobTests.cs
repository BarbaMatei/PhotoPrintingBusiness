using System.Reflection;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PhotoPrint.API.BackgroundJobs;
using PhotoPrint.API.Configuration;
using PhotoPrint.API.Data;
using PhotoPrint.API.Models;
using PhotoPrint.API.Services.Sameday;

namespace PhotoPrint.Tests.Unit.Services.Sameday;

/// <summary>
/// Unit tests for <see cref="AwbRetryJob"/>. The <c>RunOneTickAsync</c> private
/// method is invoked via reflection — same pattern as
/// <c>ArchiveRetentionJobTests</c>.
/// </summary>
public class AwbRetryJobTests
{
    private static PhotoPrintDbContext CreateDb() =>
        new(new DbContextOptionsBuilder<PhotoPrintDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static IServiceScopeFactory BuildScopes(PhotoPrintDbContext db)
    {
        var services = new ServiceCollection();
        services.AddSingleton(db);
        services.AddLogging();
        return services.BuildServiceProvider().GetRequiredService<IServiceScopeFactory>();
    }

    private static AwbRetryJob Build(
        PhotoPrintDbContext db,
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
            BuildScopes(db), queue, giveUp, settings, clock,
            new LoggerFactory().CreateLogger<AwbRetryJob>());
    }

    private static Task RunOneTickAsync(AwbRetryJob job)
    {
        var method = typeof(AwbRetryJob).GetMethod("RunOneTickAsync",
            BindingFlags.NonPublic | BindingFlags.Instance);
        return (Task)method!.Invoke(job, new object[] { CancellationToken.None })!;
    }

    private static Order SeedPaidOrder(
        PhotoPrintDbContext db,
        DateTimeOffset paidAt,
        string? awbNumber = null)
    {
        var order = new Order
        {
            Id = Guid.NewGuid(),
            OrderNumber = $"FT-{Random.Shared.Next(100_000, 999_999)}",
            Status = OrderStatus.Paid,
            AwbNumber = awbNumber,
            PaidAt = paidAt,
            DeliveryType = DeliveryType.Courier,
            ShippingAddress = new ShippingAddressSnapshot
            {
                RecipientName = "x", Phone = "x",
                Street = "x", Number = "1",
                City = "x", County = "x", PostalCode = "x",
            },
        };
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
        using var db = CreateDb();
        // Paid 1h ago — well inside the 24h window.
        SeedPaidOrder(db, paidAt: T0.AddHours(-1));
        SeedPaidOrder(db, paidAt: T0.AddHours(-2));

        var clock = new FakeTimeProvider(T0);
        var queue = new TestQueue();
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var giveUp = new AwbGiveUpRegistry(cache);
        var sut = Build(db, queue, giveUp, clock);

        await RunOneTickAsync(sut);

        queue.Enqueued.Should().HaveCount(2);
        queue.Enqueued.Should().AllSatisfy(j => j.Attempt.Should().Be(1));
    }

    [Fact]
    public async Task Skips_orders_already_having_an_AwbNumber()
    {
        using var db = CreateDb();
        SeedPaidOrder(db, paidAt: T0.AddHours(-1), awbNumber: "RO12345678");

        var clock = new FakeTimeProvider(T0);
        var queue = new TestQueue();
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var sut = Build(db, queue, new AwbGiveUpRegistry(cache), clock);

        await RunOneTickAsync(sut);

        queue.Enqueued.Should().BeEmpty();
    }

    [Fact]
    public async Task Logs_give_up_once_for_orders_outside_the_24h_window()
    {
        using var db = CreateDb();
        // Paid 25h ago — past the 24h give-up.
        var staleOrder = SeedPaidOrder(db, paidAt: T0.AddHours(-25));

        var clock = new FakeTimeProvider(T0);
        var queue = new TestQueue();
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var giveUp = new AwbGiveUpRegistry(cache);
        var sut = Build(db, queue, giveUp, clock);

        await RunOneTickAsync(sut);

        // Stale order is NOT re-enqueued; instead the registry marks it.
        queue.Enqueued.Should().BeEmpty();

        // MarkOnce now returns false for the same id because the job already marked it.
        giveUp.MarkOnce(staleOrder.Id).Should().BeFalse();
    }

    [Fact]
    public async Task Give_up_dedup_means_a_second_tick_does_not_re_log()
    {
        using var db = CreateDb();
        var staleOrder = SeedPaidOrder(db, paidAt: T0.AddHours(-25));

        var clock = new FakeTimeProvider(T0);
        var queue = new TestQueue();
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var giveUp = new AwbGiveUpRegistry(cache);
        var sut = Build(db, queue, giveUp, clock);

        await RunOneTickAsync(sut);
        await RunOneTickAsync(sut);   // second tick

        // Marked exactly once across two ticks.
        giveUp.MarkOnce(staleOrder.Id).Should().BeFalse();
    }

    [Fact]
    public async Task Does_not_enqueue_orders_in_non_Paid_status()
    {
        using var db = CreateDb();
        var order = SeedPaidOrder(db, paidAt: T0.AddHours(-1));
        order.Status = OrderStatus.Cancelled;
        await db.SaveChangesAsync();

        var clock = new FakeTimeProvider(T0);
        var queue = new TestQueue();
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var sut = Build(db, queue, new AwbGiveUpRegistry(cache), clock);

        await RunOneTickAsync(sut);

        queue.Enqueued.Should().BeEmpty();
    }
}
