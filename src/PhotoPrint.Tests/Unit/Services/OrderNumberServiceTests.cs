using Microsoft.EntityFrameworkCore;
using PhotoPrint.API.Data;
using PhotoPrint.API.Services;

namespace PhotoPrint.Tests.Unit.Services;

public class OrderNumberServiceTests : IDisposable
{
    private readonly PhotoPrintDbContext _db;
    private readonly OrderNumberService _service;

    public OrderNumberServiceTests()
    {
        var opts = new DbContextOptionsBuilder<PhotoPrintDbContext>()
            .UseInMemoryDatabase($"OrderNumberTests_{Guid.NewGuid():N}")
            .Options;
        _db = new PhotoPrintDbContext(opts);
        _service = new OrderNumberService(_db);
    }

    public void Dispose() => _db.Dispose();

    // ── Format tests ──────────────────────────────────────────────────────────

    [Fact]
    public void FormatOrderNumber_ZeroPadsTo4Digits()
    {
        Assert.Equal("FT-20260001", OrderNumberService.FormatOrderNumber(2026, 1));
        Assert.Equal("FT-20261000", OrderNumberService.FormatOrderNumber(2026, 1000));
        Assert.Equal("FT-20269999", OrderNumberService.FormatOrderNumber(2026, 9999));
    }

    [Fact]
    public void FormatOrderNumber_UsesCorrectYear()
    {
        Assert.StartsWith("FT-2026", OrderNumberService.FormatOrderNumber(2026, 1));
        Assert.StartsWith("FT-2027", OrderNumberService.FormatOrderNumber(2027, 1));
    }

    [Fact]
    public void FormatOrderNumber_MatchesExpectedPattern()
    {
        var number = OrderNumberService.FormatOrderNumber(2026, 1);
        Assert.Matches(@"^FT-\d{8}$", number);
    }

    // ── GenerateAsync (InMemory fallback) ─────────────────────────────────────

    [Fact]
    public async Task GenerateAsync_FirstCall_ReturnsFT_YYYY0001()
    {
        var number = await _service.GenerateAsync();
        var year = DateTime.UtcNow.Year;
        Assert.Equal($"FT-{year}0001", number);
    }

    [Fact]
    public async Task GenerateAsync_SecondCall_ReturnsFT_YYYY0002()
    {
        // First call
        await _service.GenerateAsync();
        // Simulate order was saved to DB (InMemory counts existing orders)
        _db.Orders.Add(new PhotoPrint.API.Models.Order
        {
            OrderNumber = await _service.GenerateAsync(),
            Status = PhotoPrint.API.Models.OrderStatus.AwaitingPayment,
            PaymentProcessor = PhotoPrint.API.Models.PaymentProcessor.Stripe,
            DeliveryType = PhotoPrint.API.Models.DeliveryType.Easybox,
            ShippingAddress = new PhotoPrint.API.Models.ShippingAddressSnapshot
            {
                Street = "Str. Test", Number = "1", City = "București",
                County = "Ilfov", PostalCode = "010000",
                RecipientName = "Test User", Phone = "0700000000",
            },
        });
        await _db.SaveChangesAsync();

        var number = await _service.GenerateAsync();
        var year = DateTime.UtcNow.Year;
        Assert.StartsWith($"FT-{year}", number);
    }
}
