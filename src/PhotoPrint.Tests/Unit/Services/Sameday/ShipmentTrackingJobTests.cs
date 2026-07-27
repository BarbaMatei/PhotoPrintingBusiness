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
using PhotoPrint.API.Models;
using PhotoPrint.API.Services;
using PhotoPrint.API.Services.Sameday;

namespace PhotoPrint.Tests.Unit.Services.Sameday;

/// <summary>
/// Tests for <see cref="ShipmentTrackingJob"/>. ADR-016's CAS race-lost
/// invariant is explicitly pinned: pre-set the order to Cancelled, observe
/// Delivered from Sameday, assert the UPDATE has no effect AND the email
/// is NOT enqueued.
///
/// In-memory provider doesn't support EF's <c>ExecuteUpdateAsync</c>, so the
/// suite uses SQLite — same approach as parts of the existing bolt-051
/// PromotionRecoveryScanner integration paths.
/// </summary>
public class ShipmentTrackingJobTests : IDisposable
{
    private readonly SqliteScopeFactory _scopes;

    public ShipmentTrackingJobTests()
    {
        _scopes = new SqliteScopeFactory();
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
        refreshed.DeliveredAt.Should().Be(observed);
        refreshed.LastTrackingSyncAt.Should().Be(observed);

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
        refreshed.LastTrackingSyncAt.Should().Be(observed);
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
    public async Task Refuses_to_move_LastTrackingSyncAt_backwards()
    {
        // Stored sync is 2 hours ago; Sameday observes 5 hours ago. The
        // monotonic-non-decreasing invariant rejects this write.
        var existingSync = T0.AddHours(-2);
        var oldObserved = T0.AddHours(-5);
        var order = SeedShippedOrder(
            shippedAt: T0.AddDays(-3),
            lastTrackingSyncAt: existingSync);

        var client = new Mock<ISamedayClient>();
        client.Setup(c => c.GetTrackingAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TrackingSnapshot(order.AwbNumber!, TrackingState.InTransit, oldObserved, Array.Empty<TrackingEvent>()));

        var emails = new Mock<IOrderEmailService>(MockBehavior.Strict);
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var clock = new FakeTimeProvider(T0);
        var sut = Build(client, emails, new TrackingStopRegistry(cache), clock);

        await RunOneTickAsync(sut);

        var refreshed = GetOrder(order.Id);
        refreshed.LastTrackingSyncAt.Should().Be(existingSync); // unchanged
    }
}

/// <summary>
/// Wraps a SQLite in-memory database + a scope factory. SQLite is required
/// (not the EF InMemory provider) because <c>ShipmentTrackingJob</c> calls
/// <c>ExecuteUpdateAsync</c> for the ADR-016 CAS transition, which isn't
/// supported by the InMemory provider.
/// </summary>
internal sealed class SqliteScopeFactory : IDisposable
{
    private readonly Microsoft.Data.Sqlite.SqliteConnection _connection;
    private readonly ServiceProvider _provider;
    private readonly Dictionary<Type, object> _overrides = new();

    public SqliteScopeFactory()
    {
        _connection = new Microsoft.Data.Sqlite.SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var services = new ServiceCollection();
        services.AddDbContext<PhotoPrintDbContext>(options => options.UseSqlite(_connection));
        services.AddLogging();
        _provider = services.BuildServiceProvider();

        using var scope = _provider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PhotoPrintDbContext>();
        db.Database.EnsureCreated();
    }

    public IServiceScopeFactory Factory => new ScopeFactoryAdapter(_provider, _overrides);

    public void RegisterScopedSingleton<T>(T instance) where T : class
    {
        _overrides[typeof(T)] = instance;
    }

    public void Dispose()
    {
        _provider.Dispose();
        _connection.Dispose();
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
