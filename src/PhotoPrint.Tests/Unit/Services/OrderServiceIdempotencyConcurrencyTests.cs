using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Options;
using Moq;
using PhotoPrint.API.Configuration;
using PhotoPrint.API.Data;
using PhotoPrint.API.DTOs.Payments;
using PhotoPrint.API.DTOs.Shipping;
using PhotoPrint.API.Exceptions;
using PhotoPrint.API.Models;
using PhotoPrint.API.Services;

namespace PhotoPrint.Tests.Unit.Services;

/// <summary>
/// Regression tests for the concurrent same-key race. These run
/// against a REAL SQLite database rather than the EF InMemory provider on purpose: the
/// bug is "the losing INSERT violates the unique index and throws an unhandled
/// DbUpdateException → 500", and InMemory does not enforce unique indexes, so it can
/// never reproduce it. SQLite enforces <c>ix_orders_idempotency_key</c>, which is what
/// makes these tests meaningful.
/// </summary>
public class OrderServiceIdempotencyConcurrencyTests : IDisposable
{
    private readonly SqliteConnection _connection;

    public OrderServiceIdempotencyConcurrencyTests()
    {
        // A single open connection keeps the in-memory database alive for the test and
        // lets multiple DbContexts (the racing "winner" + "loser") share it.
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        using var db = NewContext();
        db.Database.EnsureCreated(); // builds schema incl. the unique idempotency index
    }

    public void Dispose() => _connection.Dispose();

    private PhotoPrintDbContext NewContext(params IInterceptor[] interceptors)
    {
        var opts = new DbContextOptionsBuilder<PhotoPrintDbContext>()
            .UseSqlite(_connection)
            .AddInterceptors(interceptors)
            .Options;
        return new PhotoPrintDbContext(opts);
    }

    private static OrderService NewService(PhotoPrintDbContext db)
    {
        var counter = 0;
        var numberMock = new Mock<IOrderNumberService>();
        numberMock.Setup(s => s.GenerateAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => $"FT-{Interlocked.Increment(ref counter):D8}"); // unique per call
        var shippingMock = new Mock<IShippingService>();
        shippingMock.Setup(s => s.GetShippingCostAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ShippingCostDto(20.00m));
        return new OrderService(db, numberMock.Object, shippingMock.Object,
            Mock.Of<IStorageRouter>(), Options.Create(new StorageSettings()),
            Options.Create(new VatSettings()));
    }

    // The REAL OrderNumberService (SQLite COUNT+1 branch) — the mock's always-unique
    // Interlocked.Increment masked the concurrent order-number collision this exercises.
    private static OrderService NewServiceWithRealNumberService(PhotoPrintDbContext db)
    {
        var shippingMock = new Mock<IShippingService>();
        shippingMock.Setup(s => s.GetShippingCostAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ShippingCostDto(20.00m));
        return new OrderService(db, new OrderNumberService(db), shippingMock.Object,
            Mock.Of<IStorageRouter>(), Options.Create(new StorageSettings()),
            Options.Create(new VatSettings()));
    }

    // Build the concurrent "winner" by running the REAL
    // CreateFromCartAsync on a second context, so its totals + items come from the service
    // (same cart + same 20.00 shipping mock the caller uses) instead of hand-copied magic
    // numbers that silently drift from the pricing math and flip replay↔409 for the wrong
    // reason. Only the OrderNumber is pinned — it is the control knob for WHICH index the
    // caller collides on (distinct → idempotency only; equal → order-number first).
    private async Task<Order> InjectWinnerViaRealFlowAsync(Guid userId, string key, string orderNumber)
    {
        using var winnerDb = NewContext();
        var numberMock = new Mock<IOrderNumberService>();
        numberMock.Setup(s => s.GenerateAsync(It.IsAny<CancellationToken>())).ReturnsAsync(orderNumber);
        var shippingMock = new Mock<IShippingService>();
        shippingMock.Setup(s => s.GetShippingCostAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ShippingCostDto(20.00m));
        var svc = new OrderService(winnerDb, numberMock.Object, shippingMock.Object,
            Mock.Of<IStorageRouter>(), Options.Create(new StorageSettings()),
            Options.Create(new VatSettings()));

        var result = await svc.CreateFromCartAsync(userId, null, MakeRequest(), key);
        return result.Order;
    }

    private async Task<Guid> SeedUserAsync()
    {
        var id = Guid.NewGuid();
        using var db = NewContext();
        db.Users.Add(new User
        {
            Id = id,
            Email = $"u-{id:N}@test.com",
            NormalizedEmail = $"U-{id:N}@TEST.COM",
            FirstName = "Test",
            LastName = "User",
        });
        await db.SaveChangesAsync();
        return id;
    }

    private async Task<(Guid userId, Guid productId, Guid uploadId)> SeedCartAsync(
        decimal unitPrice = 2.00m, int quantity = 3)
    {
        var userId = await SeedUserAsync(); // SQLite enforces the CartItem/Upload → User FKs
        using var db = NewContext();

        // Shared canonical cart graph — see TestCartSeed.
        var graph = TestCartSeed.Build(userId: userId, unitPrice: unitPrice, quantity: quantity);
        graph.AddTo(db);
        await db.SaveChangesAsync();

        return (userId, graph.Product.Id, graph.Upload.Id);
    }

    // Courier delivery avoids the Order → EasyboxLocker FK (SQLite enforces it), so the
    // ONLY constraint a colliding INSERT can violate is the unique idempotency index.
    private static CreateOrderRequest MakeRequest()
        => new(PaymentProcessor.Stripe, DeliveryType.Courier, null, null);

    [Fact]
    public async Task CreateFromCart_CrossTenantKeyCollisionOnInsert_Returns409_NotServerError()
    {
        var (callerId, _, _) = await SeedCartAsync();
        const string key = "shared-key-collide";

        // A different tenant already holds the key (the global unique index spans all
        // tenants). The caller's owner-scoped read won't see it, so it proceeds to
        // INSERT — and collides.
        var ownerId = await SeedUserAsync();
        using (var seed = NewContext())
        {
            seed.Orders.Add(MinimalOrder("FT-OWNER", ownerId, key));
            await seed.SaveChangesAsync();
        }

        using var db = NewContext();
        var svc = NewService(db);

        // Before the fix this surfaced as an unhandled DbUpdateException (500). It must
        // now be a clean 409 (and must not disclose the other tenant's order).
        // The cross-tenant path throws the distinct IdempotencyKeyTaken
        // subtype (still a ConflictException → 409) so the abuse signal is observable.
        await Assert.ThrowsAsync<IdempotencyKeyTakenException>(
            () => svc.CreateFromCartAsync(callerId, null, MakeRequest(), key));

        using var verify = NewContext();
        Assert.Equal(1, await verify.Orders.CountAsync(o => o.IdempotencyKey == key));
    }

    [Fact]
    public async Task CreateFromCart_UnrelatedDbFailure_PropagatesHonestly_NotMaskedAs409()
    {
        // The recovery catches ONLY the two known unique
        // indexes — idempotency (resolve the winner) and order-number (regenerate + retry).
        // Any OTHER DbUpdateException must propagate honestly, never be masked as an
        // idempotency 409 (the old AnyAsync inference did exactly that). Here the INSERT fails
        // on an UNRELATED foreign key — Easybox delivery pointing at a non-existent locker —
        // which matches neither `when` filter, so it surfaces as a DbUpdateException.
        //
        // (Before the order-number fix this test used an order-number collision as its "unrelated" failure;
        // that is now a handled/retryable transient, so the unrelated case is an FK violation.)
        var (callerId, _, _) = await SeedCartAsync();
        const string key = "free-key-unrelated-fk"; // key is free — not the failure cause

        using var db = NewContext();
        var svc = NewService(db);

        var easyboxWithBogusLocker = new CreateOrderRequest(
            PaymentProcessor.Stripe, DeliveryType.Easybox,
            EasyboxLockerId: Guid.NewGuid(), ShippingAddress: null);

        await Assert.ThrowsAsync<DbUpdateException>(
            () => svc.CreateFromCartAsync(callerId, null, easyboxWithBogusLocker, key));
    }

    [Fact]
    public async Task CreateFromCart_ConcurrentSameOwnerSameKey_LoserReplaysWinner_OneOrder()
    {
        var (userId, _, _) = await SeedCartAsync();
        const string key = "race-key";

        // Deterministically inject the canonical double-submit race: just before the caller's
        // INSERT executes, a concurrent request with the SAME key + owner + logical request
        // commits first, so the caller loses the unique-index race. The winner is built via
        // the REAL flow — its totals/items match the caller's by construction — with
        // a DISTINCT order number so the caller collides only on the idempotency index.
        Order? winner = null;
        var interceptor = new WinnerInjectingInterceptor(
            async () => winner = await InjectWinnerViaRealFlowAsync(userId, key, "FT-WINNER"));

        using var db = NewContext(interceptor);
        var svc = NewService(db);

        var result = await svc.CreateFromCartAsync(userId, null, MakeRequest(), key);

        // The loser caught the unique violation, re-resolved the winner, and replayed it
        // — instead of 500ing. Exactly one order persisted.
        Assert.True(result.WasIdempotentReplay);
        Assert.Equal(winner!.Id, result.Order.Id);

        using var verify = NewContext();
        Assert.Equal(1, await verify.Orders.CountAsync());
    }

    [Fact]
    public async Task CreateFromCart_StaleKeyReuse_FreesOldAndInsertsNew_NoWithinBatchCollision()
    {
        // The stale-key free and the new-order INSERT now flush in a
        // SINGLE SaveChanges → one transaction on a real relational provider. This proves
        // EF's unique-index-aware ordering emits the UPDATE (free) before the INSERT, so
        // they do NOT collide on ix_orders_idempotency_key inside the one batch.
        var (userId, _, _) = await SeedCartAsync();
        const string key = "stale-reuse-key";

        using (var seed = NewContext())
        {
            var stale = MinimalOrder("FT-STALE", userId, key);
            stale.CreatedAt = DateTimeOffset.UtcNow.AddHours(-25); // outside the 24h window
            seed.Orders.Add(stale);
            await seed.SaveChangesAsync();
        }

        using var db = NewContext();
        var svc = NewService(db);
        var result = await svc.CreateFromCartAsync(userId, null, MakeRequest(), key);

        Assert.False(result.WasIdempotentReplay);      // stale → brand-new order, not a replay
        Assert.Equal(key, result.Order.IdempotencyKey); // new order owns the key

        using var verify = NewContext();
        Assert.Equal(1, await verify.Orders.CountAsync(o => o.IdempotencyKey == key)); // only the new one
        var staleRow = await verify.Orders.FirstAsync(o => o.OrderNumber == "FT-STALE");
        Assert.Null(staleRow.IdempotencyKey);           // old row freed
    }

    [Fact]
    public async Task CreateFromCart_StaleKeyReuse_InsertFails_FreeRollsBack_KeyPreserved()
    {
        // If the new-order INSERT fails, the stale-key free must roll
        // back WITH it (they share one transaction now) — otherwise the stale row loses its
        // key with no replacement, so a later retry finds no holder and the key stops
        // deduping. Before the fix the free committed in its own save, so the key was gone;
        // this assertion (key preserved after the failed insert) goes red pre-fix.
        var (userId, _, _) = await SeedCartAsync();
        const string key = "stale-reuse-fail-key";

        using (var seed = NewContext())
        {
            var stale = MinimalOrder("FT-STALE-FAIL", userId, key);
            stale.CreatedAt = DateTimeOffset.UtcNow.AddHours(-25);
            seed.Orders.Add(stale);
            await seed.SaveChangesAsync();
        }

        // Abort the save the moment a new Order INSERT is part of it.
        using var db = NewContext(new ThrowOnOrderInsertInterceptor());
        var svc = NewService(db);

        await Assert.ThrowsAnyAsync<Exception>(
            () => svc.CreateFromCartAsync(userId, null, MakeRequest(), key));

        using var verify = NewContext();
        var staleRow = await verify.Orders.FirstAsync(o => o.OrderNumber == "FT-STALE-FAIL");
        Assert.Equal(key, staleRow.IdempotencyKey);     // free rolled back with the failed insert
        Assert.Equal(1, await verify.Orders.CountAsync(o => o.IdempotencyKey == key)); // no orphan/new row
    }

    [Fact]
    public async Task SqliteIdempotencyCollision_SurfacesUniqueExtendedCode_AndColumnName()
    {
        // IsIdempotencyKeyViolation classifies the SQLite arm off the
        // EXTENDED result code SQLITE_CONSTRAINT_UNIQUE (2067) plus the column name in the
        // message (tied to nameof(Order.IdempotencyKey)). Pin both premises here so a
        // Microsoft.Data.Sqlite upgrade that re-words the message or changes the code fails
        // in THIS test — loudly — instead of silently degrading the canonical double-submit
        // from a clean 409 to an unhandled 500 (the fragility 5 lenses converged on).
        var ownerId = await SeedUserAsync();
        const string key = "dupe-key-classify";

        using (var seed = NewContext())
        {
            seed.Orders.Add(MinimalOrder("FT-DUP-A", ownerId, key));
            await seed.SaveChangesAsync();
        }

        using var db = NewContext();
        db.Orders.Add(MinimalOrder("FT-DUP-B", ownerId, key)); // same key → idempotency index

        var ex = await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
        var sqlite = Assert.IsType<SqliteException>(ex.InnerException);
        Assert.Equal(2067, sqlite.SqliteExtendedErrorCode); // SQLITE_CONSTRAINT_UNIQUE
        Assert.Contains(nameof(Order.IdempotencyKey), sqlite.Message,
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CreateFromCart_ConcurrentSameKey_OrderNumberCollidesFirst_RecoversToReplay_NotServerError()
    {
        // On SQLite the OrderNumber is a racy COUNT+1, so a same-key
        // concurrent double-submit can collide on ix_orders_order_number FIRST (it is created
        // before the idempotency index, so SQLite reports it first). That collision is NOT an
        // idempotency violation, so pre-fix it propagated as a 500 instead of replaying the
        // winner. The fix regenerates the number and retries; the genuine same-key collision
        // then surfaces and resolves to a clean replay. Uses the REAL OrderNumberService (the
        // mock's always-unique Interlocked.Increment masked this).
        var (userId, _, _) = await SeedCartAsync();
        const string key = "race-key-ordernum";

        // The caller (Orders count == 0) will generate this number via the real service; the
        // winner is pinned to the SAME number, so the caller's INSERT hits the order-number
        // index before the idempotency one. Winner totals/items come from the real flow.
        var collidingNumber = OrderNumberService.FormatOrderNumber(DateTime.UtcNow.Year, 1);

        Order? winner = null;
        var interceptor = new WinnerInjectingInterceptor(
            async () => winner = await InjectWinnerViaRealFlowAsync(userId, key, collidingNumber));

        using var db = NewContext(interceptor);
        var svc = NewServiceWithRealNumberService(db);

        var result = await svc.CreateFromCartAsync(userId, null, MakeRequest(), key);

        // The order-number collision was retried, the idempotency collision then resolved the
        // winner, and the loser replayed it — instead of 500ing. Exactly one order persisted.
        Assert.True(result.WasIdempotentReplay);
        Assert.Equal(winner!.Id, result.Order.Id);

        using var verify = NewContext();
        Assert.Equal(1, await verify.Orders.CountAsync());
    }

    private static Order MinimalOrder(string number, Guid userId, string key) => new()
    {
        OrderNumber = number,
        UserId = userId,
        IdempotencyKey = key,
        CreatedAt = DateTimeOffset.UtcNow,
        ShippingAddress = new ShippingAddressSnapshot
        {
            Street = "S", Number = "1", City = "C", County = "J",
            PostalCode = "010101", RecipientName = "R", Phone = "0700000000",
        },
    };

    /// <summary>Aborts any SaveChanges that includes a new Order INSERT — used to simulate
    /// the new-order INSERT failing mid-flight, so the test can assert the stale-key
    /// free rolled back with it rather than committing on its own.</summary>
    private sealed class ThrowOnOrderInsertInterceptor : SaveChangesInterceptor
    {
        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData, InterceptionResult<int> result, CancellationToken ct = default)
        {
            if (eventData.Context!.ChangeTracker.Entries<Order>().Any(e => e.State == EntityState.Added))
                throw new InvalidOperationException("boom: simulated INSERT failure (BUG-3 test)");
            return base.SavingChangesAsync(eventData, result, ct);
        }
    }

    /// <summary>Runs a one-shot injection the first time an Order INSERT is about to be
    /// saved on the intercepted context — simulating a concurrent winner committing
    /// between the caller's idempotency read and its own INSERT.</summary>
    private sealed class WinnerInjectingInterceptor : SaveChangesInterceptor
    {
        private readonly Func<Task> _inject;
        private bool _fired;

        public WinnerInjectingInterceptor(Func<Task> inject) => _inject = inject;

        public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData, InterceptionResult<int> result, CancellationToken ct = default)
        {
            if (!_fired &&
                eventData.Context!.ChangeTracker.Entries<Order>().Any(e => e.State == EntityState.Added))
            {
                _fired = true;
                await _inject();
            }
            return await base.SavingChangesAsync(eventData, result, ct);
        }
    }
}
