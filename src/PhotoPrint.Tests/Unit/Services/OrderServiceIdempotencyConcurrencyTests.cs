using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Moq;
using PhotoPrint.API.Data;
using PhotoPrint.API.DTOs.Payments;
using PhotoPrint.API.DTOs.Shipping;
using PhotoPrint.API.Exceptions;
using PhotoPrint.API.Models;
using PhotoPrint.API.Services;

namespace PhotoPrint.Tests.Unit.Services;

/// <summary>
/// BUG-1 (review 035-v1) regression tests for the concurrent same-key race. These run
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
        return new OrderService(db, numberMock.Object, shippingMock.Object);
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

        var product = new Product { Name = "Foto 10x15", IsActive = true };
        var size = new ProductSize { ProductId = product.Id, Label = "10x15", WidthMm = 100, HeightMm = 150, IsActive = true };
        var tier = new PricingTier { ProductSizeId = size.Id, MinQuantity = 1, MaxQuantity = null, UnitPrice = unitPrice };
        var finish = new ProductFinish { ProductId = product.Id, Name = "Lucios" };
        var upload = new Upload { UserId = userId, OriginalFileName = "p.jpg", FilePath = "/p.jpg", ContentType = "image/jpeg", WidthPx = 1800, HeightPx = 1200 };
        var cartItem = new CartItem { UserId = userId, UploadId = upload.Id, ProductId = product.Id, SizeId = size.Id, Quantity = quantity };

        db.Products.Add(product);
        db.ProductSizes.Add(size);
        db.PricingTiers.Add(tier);
        db.ProductFinishes.Add(finish);
        db.Uploads.Add(upload);
        db.CartItems.Add(cartItem);
        await db.SaveChangesAsync();

        return (userId, product.Id, upload.Id);
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
        await Assert.ThrowsAsync<ConflictException>(
            () => svc.CreateFromCartAsync(callerId, null, MakeRequest(), key));

        using var verify = NewContext();
        Assert.Equal(1, await verify.Orders.CountAsync(o => o.IdempotencyKey == key));
    }

    [Fact]
    public async Task CreateFromCart_ConcurrentSameOwnerSameKey_LoserReplaysWinner_OneOrder()
    {
        var (userId, productId, uploadId) = await SeedCartAsync();
        const string key = "race-key";
        var winnerId = Guid.NewGuid();

        // Deterministically inject the canonical double-submit race: just before the
        // caller's INSERT executes, a concurrent request with the SAME key + owner +
        // logical request commits first, so the caller loses the unique-index race.
        var interceptor = new WinnerInjectingInterceptor(async () =>
        {
            using var winnerDb = NewContext();
            var winner = MinimalOrder("FT-WINNER", userId, key);
            winner.Id = winnerId;
            winner.PaymentProcessor = PaymentProcessor.Stripe;
            winner.DeliveryType = DeliveryType.Courier;
            winner.SubtotalRon = 6.00m;
            winner.ShippingCostRon = 20.00m;
            winner.TotalRon = 26.00m; // 3 × 2.00 + 20.00 shipping — matches the caller's request
            winner.Items.Add(new OrderItem
            {
                ProductId = productId,
                UploadId = uploadId,
                Quantity = 3,
                UnitPriceRon = 2.00m,
                LineTotalRon = 6.00m,
                ProductSnapshot = new ProductSnapshot { ProductName = "Foto 10x15", Size = "10x15", Finish = "Lucios" },
            });
            winnerDb.Orders.Add(winner);
            await winnerDb.SaveChangesAsync();
        });

        using var db = NewContext(interceptor);
        var svc = NewService(db);

        var result = await svc.CreateFromCartAsync(userId, null, MakeRequest(), key);

        // The loser caught the unique violation, re-resolved the winner, and replayed it
        // — instead of 500ing. Exactly one order persisted.
        Assert.True(result.WasIdempotentReplay);
        Assert.Equal(winnerId, result.Order.Id);

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
