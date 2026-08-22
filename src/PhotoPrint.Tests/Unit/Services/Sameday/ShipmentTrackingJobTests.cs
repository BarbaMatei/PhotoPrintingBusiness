using System.Reflection;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using PhotoPrint.API.BackgroundJobs;
using PhotoPrint.API.Configuration;
using PhotoPrint.API.Data;
using PhotoPrint.API.Exceptions;
using PhotoPrint.API.Models;
using PhotoPrint.API.Services;
using PhotoPrint.Tests.Helpers;
using PhotoPrint.API.Services.Sameday;

namespace PhotoPrint.Tests.Unit.Services.Sameday;

/// <summary>
/// Tests for <see cref="ShipmentTrackingJob"/>. The CAS race-lost
/// invariant is explicitly pinned: pre-set the order to Cancelled, observe
/// Delivered from Sameday, assert the UPDATE has no effect AND the email
/// is NOT enqueued.
///
/// The InMemory provider does not support EF's <c>ExecuteUpdateAsync</c>, so the
/// suite runs against a real PostgreSQL database.
/// </summary>
public class ShipmentTrackingJobTests : IClassFixture<PostgresTestDatabase>, IDisposable
{
    private readonly PostgresScopeFactory _scopes;

    public ShipmentTrackingJobTests(PostgresTestDatabase database)
    {
        database.ResetForTest();
        _scopes = new PostgresScopeFactory(database);
    }

    public void Dispose() => _scopes.Dispose();

    private static readonly DateTimeOffset T0 = new(2026, 6, 2, 12, 0, 0, TimeSpan.Zero);

    private ShipmentTrackingJob Build(
        Mock<ISamedayClient> client,
        Mock<IOrderEmailService> emails,
        TrackingStopRegistry stop,
        TimeProvider clock,
        int intervalMinutes = 15,
        int maxAgeDays = 30)
    {
        _scopes.RegisterScopedSingleton(client.Object);
        _scopes.RegisterScopedSingleton(emails.Object);

        var settings = Options.Create(new SamedaySettings
        {
            Jobs = new SamedayJobsSettings
            {
                Enabled = true,
                TrackingIntervalMinutes = intervalMinutes,
                TrackingMaxAgeDays = maxAgeDays,
                MaxConcurrentSamedayCalls = 5,
            },
        });
        return new ShipmentTrackingJob(
            _scopes.Factory, stop, settings, clock,
            new LoggerFactory().CreateLogger<ShipmentTrackingJob>());
    }

    private static Task RunOneTickAsync(ShipmentTrackingJob job)
    {
        var method = typeof(ShipmentTrackingJob).GetMethod("RunOneTickAsync",
            BindingFlags.NonPublic | BindingFlags.Instance);
        return (Task)method!.Invoke(job, new object[] { CancellationToken.None })!;
    }

    private Order SeedShippedOrder(
        DateTimeOffset shippedAt,
        DateTimeOffset? lastTrackingSyncAt = null,
        string? awbNumber = "RO12345678",
        OrderStatus status = OrderStatus.Shipped)
    {
        var order = new Order
        {
            Id = Guid.NewGuid(),
            OrderNumber = $"FT-{Random.Shared.Next(100_000, 999_999)}",
            Status = status,
            AwbNumber = awbNumber,
            ShippedAt = shippedAt,
            LastTrackingSyncAt = lastTrackingSyncAt,
            DeliveryType = DeliveryType.Courier,
            ShippingAddress = new ShippingAddressSnapshot
            {
                RecipientName = "x", Phone = "x",
                Street = "x", Number = "1",
                City = "x", County = "x", PostalCode = "x",
            },
        };
        using var scope = _scopes.Factory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PhotoPrintDbContext>();
        db.Orders.Add(order);
        db.SaveChanges();
        return order;
    }

    private Order GetOrder(Guid id)
    {
        using var scope = _scopes.Factory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PhotoPrintDbContext>();
        return db.Orders.AsNoTracking().Single(o => o.Id == id);
    }

    /// <summary>Applies a change on its own context — stands in for a second replica writing
    /// between this poll's order load and its write.</summary>
    private void MutateOrder(Guid id, Action<Order> mutate)
    {
        using var scope = _scopes.Factory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PhotoPrintDbContext>();
        var order = db.Orders.Single(o => o.Id == id);
        mutate(order);
        db.SaveChanges();
    }

    private void AdvanceStatus(Guid id, OrderStatus status)
    {
        using var scope = _scopes.Factory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PhotoPrintDbContext>();
        db.Orders.Where(o => o.Id == id).ExecuteUpdate(s => s.SetProperty(o => o.Status, status));
    }

    [Fact]
    public async Task Transitions_to_Delivered_on_Sameday_delivered_state()
    {
        var observed = T0.AddDays(-1);
        var order = SeedShippedOrder(shippedAt: T0.AddDays(-3));

        var client = new Mock<ISamedayClient>();
        client.Setup(c => c.GetTrackingAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TrackingSnapshot(order.AwbNumber!, TrackingState.Delivered, observed, Array.Empty<TrackingEvent>()));

        var emails = new Mock<IOrderEmailService>();
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var clock = new FakeTimeProvider(T0);
        var sut = Build(client, emails, new TrackingStopRegistry(cache), clock);

        await RunOneTickAsync(sut);

        var refreshed = GetOrder(order.Id);
        refreshed.Status.Should().Be(OrderStatus.Delivered);
        refreshed.DeliveredAt.Should().Be(observed);   // vendor's observed time
        refreshed.LastTrackingSyncAt.Should().Be(T0);  // our poll clock

        emails.Verify(e => e.FireOrderDeliveredEmail(It.IsAny<Order>()), Times.Once);
    }

    [Fact]
    public async Task ADR_016_CAS_race_lost_when_order_advances_during_the_poll()
    {
        // The order is genuinely Shipped and IN the polling window. A concurrent
        // writer advances it out of Shipped WHILE the tracking call is in flight,
        // so the CAS UPDATE (WHERE Status == Shipped) must affect 0 rows, fire no
        // email, and throw nothing. Removing `&& Status == Shipped` reddens this.
        var order = SeedShippedOrder(shippedAt: T0.AddDays(-3));

        var client = new Mock<ISamedayClient>();
        client.Setup(c => c.GetTrackingAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns<string, CancellationToken>((_, _) =>
            {
                AdvanceStatus(order.Id, OrderStatus.Delivered);   // the racing writer
                return Task.FromResult(new TrackingSnapshot(
                    order.AwbNumber!, TrackingState.Delivered, T0, Array.Empty<TrackingEvent>()));
            });

        var emails = new Mock<IOrderEmailService>(MockBehavior.Strict); // any fire → test fails
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var clock = new FakeTimeProvider(T0);
        var sut = Build(client, emails, new TrackingStopRegistry(cache), clock);

        await RunOneTickAsync(sut);

        var refreshed = GetOrder(order.Id);
        refreshed.Status.Should().Be(OrderStatus.Delivered);
        refreshed.DeliveredAt.Should().BeNull(); // our CAS never ran — the racer only set Status
    }

    [Fact]
    public async Task A_poll_timeout_does_not_fault_the_tick()
    {
        // A per-poll HttpClient timeout (OperationCanceledException, not shutdown) must be
        // caught per order — if it escaped, WhenAll would rethrow and the loop would exit.
        var order = SeedShippedOrder(shippedAt: T0.AddDays(-3));

        var client = new Mock<ISamedayClient>();
        client.Setup(c => c.GetTrackingAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException());

        var emails = new Mock<IOrderEmailService>(MockBehavior.Strict);
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var sut = Build(client, emails, new TrackingStopRegistry(cache), new FakeTimeProvider(T0));

        var act = () => RunOneTickAsync(sut);
        await act.Should().NotThrowAsync();

        GetOrder(order.Id).Status.Should().Be(OrderStatus.Shipped); // untouched
    }

    [Fact]
    public async Task Updates_LastTrackingSyncAt_only_for_non_terminal_states()
    {
        var order = SeedShippedOrder(shippedAt: T0.AddDays(-3));

        var client = new Mock<ISamedayClient>();
        var observed = T0.AddHours(-1);
        client.Setup(c => c.GetTrackingAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TrackingSnapshot(order.AwbNumber!, TrackingState.InTransit, observed, Array.Empty<TrackingEvent>()));

        var emails = new Mock<IOrderEmailService>(MockBehavior.Strict);
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var clock = new FakeTimeProvider(T0);
        var sut = Build(client, emails, new TrackingStopRegistry(cache), clock);

        await RunOneTickAsync(sut);

        var refreshed = GetOrder(order.Id);
        refreshed.Status.Should().Be(OrderStatus.Shipped); // unchanged
        refreshed.LastTrackingSyncAt.Should().Be(T0);      // our poll clock, not the vendor time
        refreshed.DeliveredAt.Should().BeNull();
    }

    [Fact]
    public async Task Skips_polling_when_LastTrackingSyncAt_is_within_the_interval()
    {
        // Synced just 2 minutes ago; default interval is 15 min so this order
        // should NOT be polled this tick.
        var order = SeedShippedOrder(
            shippedAt: T0.AddDays(-3),
            lastTrackingSyncAt: T0.AddMinutes(-2));

        var client = new Mock<ISamedayClient>(MockBehavior.Strict); // any call → test fails
        var emails = new Mock<IOrderEmailService>(MockBehavior.Strict);
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var clock = new FakeTimeProvider(T0);
        var sut = Build(client, emails, new TrackingStopRegistry(cache), clock);

        await RunOneTickAsync(sut);

        client.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Emits_PollingStopped_once_for_orders_past_30_days()
    {
        // Shipped 31 days ago — outside the polling window.
        var staleOrder = SeedShippedOrder(shippedAt: T0.AddDays(-31));

        var client = new Mock<ISamedayClient>(MockBehavior.Strict); // not called for out-of-window
        var emails = new Mock<IOrderEmailService>(MockBehavior.Strict);
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var stop = new TrackingStopRegistry(cache);
        var clock = new FakeTimeProvider(T0);
        var sut = Build(client, emails, stop, clock);

        await RunOneTickAsync(sut);
        // After the tick, MarkOnce must return false (the job already marked it).
        stop.MarkOnce(staleOrder.Id).Should().BeFalse();
    }

    [Fact]
    public async Task Delivered_is_not_blocked_by_an_earlier_vendor_timestamp()
    {
        // The stored sync is 2h ago (a UtcNow-fallback poll); the real Delivered scan carries
        // an earlier vendor timestamp. The order must STILL transition — the old monotonic
        // guard wrongly dropped it (stranding the order Shipped until the 30-day stop).
        var order = SeedShippedOrder(
            shippedAt: T0.AddDays(-3),
            lastTrackingSyncAt: T0.AddHours(-2));

        var deliveredAt = T0.AddHours(-5); // earlier than the stored sync
        var client = new Mock<ISamedayClient>();
        client.Setup(c => c.GetTrackingAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TrackingSnapshot(order.AwbNumber!, TrackingState.Delivered, deliveredAt, Array.Empty<TrackingEvent>()));

        var emails = new Mock<IOrderEmailService>();
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var sut = Build(client, emails, new TrackingStopRegistry(cache), new FakeTimeProvider(T0));

        await RunOneTickAsync(sut);

        var refreshed = GetOrder(order.Id);
        refreshed.Status.Should().Be(OrderStatus.Delivered);
        refreshed.DeliveredAt.Should().Be(deliveredAt);
        emails.Verify(e => e.FireOrderDeliveredEmail(It.IsAny<Order>()), Times.Once);
    }

    [Fact]
    public async Task Polls_an_order_synced_a_full_interval_ago()
    {
        // an order polled exactly one interval ago must be eligible THIS tick. The old
        // full-interval window made `synced < now - interval` false and skipped it to the next tick.
        var order = SeedShippedOrder(
            shippedAt: T0.AddDays(-3),
            lastTrackingSyncAt: T0.AddMinutes(-15)); // exactly one interval ago

        var client = new Mock<ISamedayClient>();
        client.Setup(c => c.GetTrackingAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TrackingSnapshot(order.AwbNumber!, TrackingState.InTransit, T0.AddHours(-1), Array.Empty<TrackingEvent>()));

        var emails = new Mock<IOrderEmailService>(MockBehavior.Strict);
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var sut = Build(client, emails, new TrackingStopRegistry(cache), new FakeTimeProvider(T0));

        await RunOneTickAsync(sut);

        client.Verify(c => c.GetTrackingAsync(order.AwbNumber!, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Polls_multiple_in_window_orders_in_one_tick()
    {
        // the tick fans PollOneAsync out over every in-window id (each on its OWN scoped
        // DbContext). Exercise it with two orders so a per-order scope/isolation regression reddens.
        var delivered = SeedShippedOrder(shippedAt: T0.AddDays(-3), awbNumber: "RO-DELIV");
        var inTransit = SeedShippedOrder(shippedAt: T0.AddDays(-3), awbNumber: "RO-TRANSIT");

        var client = new Mock<ISamedayClient>();
        client.Setup(c => c.GetTrackingAsync("RO-DELIV", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TrackingSnapshot("RO-DELIV", TrackingState.Delivered, T0.AddHours(-1), Array.Empty<TrackingEvent>()));
        client.Setup(c => c.GetTrackingAsync("RO-TRANSIT", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TrackingSnapshot("RO-TRANSIT", TrackingState.InTransit, T0.AddHours(-1), Array.Empty<TrackingEvent>()));

        var emails = new Mock<IOrderEmailService>();
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var sut = Build(client, emails, new TrackingStopRegistry(cache), new FakeTimeProvider(T0));

        await RunOneTickAsync(sut);

        GetOrder(delivered.Id).Status.Should().Be(OrderStatus.Delivered);
        GetOrder(inTransit.Id).Status.Should().Be(OrderStatus.Shipped);
        GetOrder(inTransit.Id).LastTrackingSyncAt.Should().Be(T0);
        emails.Verify(e => e.FireOrderDeliveredEmail(It.IsAny<Order>()), Times.Once);
    }

    [Fact]
    public async Task Does_not_stamp_a_row_another_replica_already_moved_to_Delivered()
    {
        // The poll reads the order, then a second replica wins the Delivered transition before this
        // one writes its poll-throttle stamp. The non-delivered write must not touch that row:
        // without the Status guard it would stamp LastTrackingSyncAt onto a Delivered order.
        var order = SeedShippedOrder(shippedAt: T0.AddDays(-3));
        var otherReplicaStamp = T0.AddMinutes(-1);

        var client = new Mock<ISamedayClient>();
        client.Setup(c => c.GetTrackingAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
            {
                // Interleave: happens after the order load, before the write.
                MutateOrder(order.Id, o =>
                {
                    o.Status = OrderStatus.Delivered;
                    o.DeliveredAt = T0.AddMinutes(-1);
                    o.LastTrackingSyncAt = otherReplicaStamp;
                });
                return new TrackingSnapshot(order.AwbNumber!, TrackingState.InTransit, T0.AddHours(-2), Array.Empty<TrackingEvent>());
            });

        var emails = new Mock<IOrderEmailService>(MockBehavior.Strict);
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var sut = Build(client, emails, new TrackingStopRegistry(cache), new FakeTimeProvider(T0));

        await RunOneTickAsync(sut);

        var refreshed = GetOrder(order.Id);
        refreshed.Status.Should().Be(OrderStatus.Delivered);
        refreshed.LastTrackingSyncAt.Should().Be(otherReplicaStamp, "the Delivered row must be left alone");
    }

    [Fact]
    public async Task Does_not_move_LastTrackingSyncAt_backwards()
    {
        // A slow replica finishing its poll after a newer one already stamped a later time must not
        // rewind the poll-throttle timestamp — that would re-open an early re-poll window.
        var order = SeedShippedOrder(shippedAt: T0.AddDays(-3));
        var newerStamp = T0.AddMinutes(5);

        var client = new Mock<ISamedayClient>();
        client.Setup(c => c.GetTrackingAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
            {
                MutateOrder(order.Id, o => o.LastTrackingSyncAt = newerStamp);
                return new TrackingSnapshot(order.AwbNumber!, TrackingState.InTransit, T0.AddHours(-2), Array.Empty<TrackingEvent>());
            });

        var emails = new Mock<IOrderEmailService>(MockBehavior.Strict);
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var sut = Build(client, emails, new TrackingStopRegistry(cache), new FakeTimeProvider(T0));

        await RunOneTickAsync(sut);

        GetOrder(order.Id).LastTrackingSyncAt.Should().Be(newerStamp, "the later stamp wins");
    }

    [Fact]
    public async Task A_systemic_auth_failure_does_not_fault_the_tick_and_is_marked_once()
    {
        // Rotated credentials: every order throws SamedayAuthException. The tick must not fault, and
        // the outage escalation is deduped (one Error per window, not one per order).
        SeedShippedOrder(shippedAt: T0.AddDays(-3), awbNumber: "RO-A");
        SeedShippedOrder(shippedAt: T0.AddDays(-3), awbNumber: "RO-B");

        var client = new Mock<ISamedayClient>();
        client.Setup(c => c.GetTrackingAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new SamedayAuthException("/api/awb/tracking"));

        var emails = new Mock<IOrderEmailService>(MockBehavior.Strict);
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var stop = new TrackingStopRegistry(cache);
        var sut = Build(client, emails, stop, new FakeTimeProvider(T0));

        var act = () => RunOneTickAsync(sut);
        await act.Should().NotThrowAsync();

        // The job already claimed the outage key this window, so a fresh mark returns false.
        stop.MarkOutageOnce("auth", TimeSpan.FromMinutes(30)).Should().BeFalse();
    }
}

/// <summary>
/// Wraps a throwaway PostgreSQL database + a scope factory. A real relational database is
/// required (not the EF InMemory provider) because <c>ShipmentTrackingJob</c> calls
/// <c>ExecuteUpdateAsync</c> for the CAS transition, which InMemory does not support.
/// </summary>
internal sealed class PostgresScopeFactory : IDisposable
{
    private readonly PostgresTestDatabase _database;
    private readonly ServiceProvider _provider;
    private readonly Dictionary<Type, object> _overrides = new();

    public PostgresScopeFactory(PostgresTestDatabase database)
    {
        _database = database;
        var services = new ServiceCollection();
        services.AddDbContext<PhotoPrintDbContext>(
            options => options.UseNpgsql(_database.ConnectionString));
        services.AddLogging();
        _provider = services.BuildServiceProvider();
    }

    public IServiceScopeFactory Factory => new ScopeFactoryAdapter(_provider, _overrides);

    public void RegisterScopedSingleton<T>(T instance) where T : class
    {
        _overrides[typeof(T)] = instance;
    }

    public void Dispose()
    {
        _provider.Dispose();
    }

    private sealed class ScopeFactoryAdapter : IServiceScopeFactory
    {
        private readonly IServiceProvider _root;
        private readonly Dictionary<Type, object> _overrides;

        public ScopeFactoryAdapter(IServiceProvider root, Dictionary<Type, object> overrides)
        {
            _root = root;
            _overrides = overrides;
        }

        public IServiceScope CreateScope()
            => new OverridingScope(_root.GetRequiredService<IServiceScopeFactory>().CreateScope(), _overrides);
    }

    private sealed class OverridingScope : IServiceScope
    {
        private readonly IServiceScope _inner;

        public OverridingScope(IServiceScope inner, Dictionary<Type, object> overrides)
        {
            _inner = inner;
            ServiceProvider = new OverridingProvider(inner.ServiceProvider, overrides);
        }

        public IServiceProvider ServiceProvider { get; }

        public void Dispose() => _inner.Dispose();
    }

    private sealed class OverridingProvider : IServiceProvider
    {
        private readonly IServiceProvider _inner;
        private readonly Dictionary<Type, object> _overrides;

        public OverridingProvider(IServiceProvider inner, Dictionary<Type, object> overrides)
        {
            _inner = inner;
            _overrides = overrides;
        }

        public object? GetService(Type serviceType)
            => _overrides.TryGetValue(serviceType, out var v) ? v : _inner.GetService(serviceType);
    }
}
