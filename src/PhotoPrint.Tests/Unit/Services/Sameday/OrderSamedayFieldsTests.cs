using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using PhotoPrint.API.Data;
using PhotoPrint.API.Models;

namespace PhotoPrint.Tests.Unit.Services.Sameday;

/// <summary>
/// Verifies that the new <c>AwbLabelUrl</c> + <c>LastTrackingSyncAt</c> columns
/// on <c>Order</c> round-trip through EF Core correctly on the same in-memory
/// provider used elsewhere in the test suite. The Postgres migration is
/// validated separately by the build (designer file scaffolds from the model).
/// </summary>
public class OrderSamedayFieldsTests : IDisposable
{
    private readonly PhotoPrintDbContext _db;

    public OrderSamedayFieldsTests()
    {
        var opts = new DbContextOptionsBuilder<PhotoPrintDbContext>()
            .UseInMemoryDatabase($"SamedayOrder_{Guid.NewGuid():N}")
            .Options;
        _db = new PhotoPrintDbContext(opts);
    }

    public void Dispose() => _db.Dispose();

    [Fact]
    public async Task Both_fields_default_to_null_for_a_fresh_order()
    {
        var order = SeedOrder();
        _db.Orders.Add(order);
        await _db.SaveChangesAsync();

        var roundTripped = await _db.Orders.AsNoTracking().FirstAsync(o => o.Id == order.Id);
        roundTripped.AwbLabelUrl.Should().BeNull();
        roundTripped.LastTrackingSyncAt.Should().BeNull();
    }

    [Fact]
    public async Task AwbLabelUrl_round_trips()
    {
        var order = SeedOrder();
        order.AwbLabelUrl = "https://sameday.cdn/labels/abc.pdf";
        _db.Orders.Add(order);
        await _db.SaveChangesAsync();

        var roundTripped = await _db.Orders.AsNoTracking().FirstAsync(o => o.Id == order.Id);
        roundTripped.AwbLabelUrl.Should().Be("https://sameday.cdn/labels/abc.pdf");
    }

    [Fact]
    public async Task LastTrackingSyncAt_round_trips()
    {
        var ts = new DateTimeOffset(2026, 6, 2, 14, 30, 0, TimeSpan.Zero);
        var order = SeedOrder();
        order.LastTrackingSyncAt = ts;
        _db.Orders.Add(order);
        await _db.SaveChangesAsync();

        var roundTripped = await _db.Orders.AsNoTracking().FirstAsync(o => o.Id == order.Id);
        roundTripped.LastTrackingSyncAt.Should().Be(ts);
    }

    private static Order SeedOrder() => new()
    {
        Id = Guid.NewGuid(),
        OrderNumber = $"FT-{Guid.NewGuid():N}".Substring(0, 12),
        ShippingAddress = new ShippingAddressSnapshot
        {
            RecipientName = "Test User",
            Phone         = "+40712345678",
            Street        = "Str. Test",
            Number        = "1",
            City          = "Bucuresti",
            County        = "Bucuresti",
            PostalCode    = "010101",
        },
        DeliveryType    = DeliveryType.Easybox,
        SubtotalRon     = 100m,
        ShippingCostRon = 20m,
        TotalRon        = 120m,
        Status          = OrderStatus.AwaitingPayment,
    };
}
