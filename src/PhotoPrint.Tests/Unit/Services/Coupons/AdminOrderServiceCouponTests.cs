using FluentAssertions;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using PhotoPrint.API.Configuration;
using PhotoPrint.API.Data;
using PhotoPrint.API.Exceptions;
using PhotoPrint.API.Hubs;
using PhotoPrint.API.Models;
using PhotoPrint.API.Services;
using PhotoPrint.API.Services.Coupons;
using PhotoPrint.API.Services.Invoicing;
using PhotoPrint.API.Services.Sameday;
using PhotoPrint.Tests.Helpers;
using Stripe;
using Coupon = PhotoPrint.API.Models.Coupon;
using CouponService = PhotoPrint.API.Services.Coupons.CouponService;

namespace PhotoPrint.Tests.Unit.Services.Coupons;

public class AdminOrderServiceCouponTests
{
    private readonly PhotoPrintDbContext _db;
    private readonly CouponService _coupons;
    private readonly AdminOrderService _sut;
    private readonly Mock<IStorageRouter> _router = new();
    private readonly Mock<IOriginalPurger> _purger = new();

    public AdminOrderServiceCouponTests()
    {
        var options = new DbContextOptionsBuilder<PhotoPrintDbContext>()
            .UseInMemoryDatabase($"AdminOrderSvcCoupon_{Guid.NewGuid():N}")
            .Options;
        _db = new PhotoPrintDbContext(options);
        _coupons = TestCoupons.ServiceFor(_db);

        var hub = new Mock<IHubContext<AdminOrderHub>>();
        var hubClients = new Mock<IHubClients>();
        var clientProxy = new Mock<IClientProxy>();
        hub.Setup(h => h.Clients).Returns(hubClients.Object);
        hubClients.Setup(c => c.All).Returns(clientProxy.Object);
        clientProxy
            .Setup(c => c.SendCoreAsync(
                It.IsAny<string>(), It.IsAny<object[]>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _purger.Setup(p => p.PurgeOrderOriginalsAsync(
                It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(PurgeOutcome.Empty);
        _router.SetupGet(r => r.CloudEnabled).Returns(false);

        var awbNotifier = new Mock<IAwbCreationNotifier>();
        var invoiceCreator = new Mock<IInvoiceCreationService>();
        invoiceCreator
            .Setup(c => c.CreateForOrderAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PhotoPrint.API.Models.Invoice?)null);

        _sut = new AdminOrderService(
            _db,
            new Mock<IOrderEmailService>().Object,
            new Mock<IStripeClient>().Object,
            _router.Object,
            _purger.Object,
            Options.Create(new ArchiveSettings()),
            hub.Object,
            awbNotifier.Object,
            invoiceCreator.Object,
            _coupons,
            NullLogger<AdminOrderService>.Instance);
    }

    private async Task<(Order Order, Coupon Coupon)> SeedDiscountedOrderAsync(
        int redemptionsCount = 1)
    {
        var coupon = TestCoupons.Make(
            code: "VARA25", value: 15m, maxRedemptions: 5, redemptionsCount: redemptionsCount);
        _db.Coupons.Add(coupon);

        var order = new Order
        {
            OrderNumber = "FT-TEST-900",
            Status = OrderStatus.Paid,
            PaymentIntentId = null,
            DeliveryType = DeliveryType.Courier,
            ShippingAddress = new ShippingAddressSnapshot
            {
                RecipientName = "Test User",
                Street = "Str. Test",
                Number = "1",
                City = "București",
                County = "Ilfov",
                PostalCode = "010000",
                Phone = "0700000000",
            },
            SubtotalRon = 100m,
            ShippingCostRon = 15m,
            DiscountRon = 15m,
            CouponCode = "VARA25",
            TotalRon = 100m,
            Items =
            [
                new OrderItem
                {
                    UploadId = Guid.NewGuid(),
                    ProductSnapshot = new ProductSnapshot
                    {
                        ProductName = "Foto 10x15",
                        Size = "10x15",
                        Finish = "Lucios",
                    },
                    Quantity = 10,
                    UnitPriceRon = 10m,
                    LineTotalRon = 100m,
                },
            ],
        };
        _db.Orders.Add(order);
        _db.CouponRedemptions.Add(new CouponRedemption
        {
            CouponId = coupon.Id,
            OrderId = order.Id,
            DiscountRon = 15m,
        });
        await _db.SaveChangesAsync();
        return (order, coupon);
    }

    [Fact]
    public async Task CancelOrder_ReleasesRedemption_AndIsIdempotent()
    {
        var (order, coupon) = await SeedDiscountedOrderAsync();

        await _sut.CancelOrderAsync(order.Id, "test");

        _db.CouponRedemptions.AsNoTracking().Any(r => r.OrderId == order.Id)
            .Should().BeFalse();
        (await _db.Coupons.AsNoTracking().FirstAsync(c => c.Id == coupon.Id))
            .RedemptionsCount.Should().Be(0);

        await _coupons.ReleaseForOrderAsync(order.Id);

        (await _db.Coupons.AsNoTracking().FirstAsync(c => c.Id == coupon.Id))
            .RedemptionsCount.Should().Be(0);
    }

    [Fact]
    public async Task CancelOrder_TwiceIsRefusedByTheMachine_AndDoesNotDecrementAgain()
    {
        var (order, coupon) = await SeedDiscountedOrderAsync(redemptionsCount: 2);

        await _sut.CancelOrderAsync(order.Id, "test");

        var act = () => _sut.CancelOrderAsync(order.Id, "test");

        await act.Should().ThrowAsync<InvalidOrderTransitionException>();
        (await _db.Coupons.AsNoTracking().FirstAsync(c => c.Id == coupon.Id))
            .RedemptionsCount.Should().Be(1);
    }

    [Fact]
    public async Task CancelOrder_WithoutACoupon_TouchesNoRedemptionCount()
    {
        var (order, coupon) = await SeedDiscountedOrderAsync();
        var otherOrder = new Order
        {
            OrderNumber = "FT-TEST-901",
            Status = OrderStatus.Paid,
            DeliveryType = DeliveryType.Courier,
            ShippingAddress = new ShippingAddressSnapshot
            {
                RecipientName = "Test User",
                Street = "Str. Test",
                Number = "2",
                City = "București",
                County = "Ilfov",
                PostalCode = "010000",
                Phone = "0700000000",
            },
            SubtotalRon = 50m,
            ShippingCostRon = 15m,
            TotalRon = 65m,
            Items =
            [
                new OrderItem
                {
                    UploadId = Guid.NewGuid(),
                    ProductSnapshot = new ProductSnapshot
                    {
                        ProductName = "Foto 10x15",
                        Size = "10x15",
                        Finish = "Lucios",
                    },
                    Quantity = 5,
                    UnitPriceRon = 10m,
                    LineTotalRon = 50m,
                },
            ],
        };
        _db.Orders.Add(otherOrder);
        await _db.SaveChangesAsync();

        await _sut.CancelOrderAsync(otherOrder.Id, "test");

        (await _db.Coupons.AsNoTracking().FirstAsync(c => c.Id == coupon.Id))
            .RedemptionsCount.Should().Be(1);
        _db.CouponRedemptions.AsNoTracking().Any(r => r.OrderId == order.Id)
            .Should().BeTrue();
    }
}
