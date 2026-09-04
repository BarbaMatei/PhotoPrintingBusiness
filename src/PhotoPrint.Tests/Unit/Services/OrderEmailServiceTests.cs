using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using PhotoPrint.API.Configuration;
using PhotoPrint.API.Models;
using PhotoPrint.API.Services;

namespace PhotoPrint.Tests.Unit.Services;

public class OrderEmailServiceTests
{
    private static OrderEmailService CreateSut(IEmailService? emailService = null)
    {
        var email = emailService ?? Mock.Of<IEmailService>();
        var app = Options.Create(new AppSettings { BaseUrl = "https://fototipar.ro" });
        return new OrderEmailService(email, app, NullLogger<OrderEmailService>.Instance);
    }

    private static Order BuildRegisteredOrder(string awbNumber = "", string? trackingUrl = null)
    {
        var user = new User { FirstName = "Ion", Email = "ion@test.com" };
        return new Order
        {
            OrderNumber = "FT-0001",
            User = user,
            UserId = user.Id,
            DeliveryType = DeliveryType.Courier,
            ShippingAddress = new ShippingAddressSnapshot
            {
                RecipientName = "Ion Popescu",
                Street = "Str. Florilor",
                Number = "1",
                City = "București",
                County = "Ilfov",
                PostalCode = "077190",
                Phone = "0700000000",
            },
            SubtotalRon = 20m,
            ShippingCostRon = 15m,
            TotalRon = 35m,
            AwbNumber = string.IsNullOrEmpty(awbNumber) ? null : awbNumber,
            TrackingUrl = trackingUrl,
            Items = new List<OrderItem>
            {
                new()
                {
                    ProductSnapshot = new ProductSnapshot
                    {
                        ProductName = "Foto 10x15",
                        Size = "10x15",
                        Finish = "Lucios",
                    },
                    Quantity = 5,
                    UnitPriceRon = 2m,
                    LineTotalRon = 10m,
                },
            },
        };
    }

    private static Order BuildGuestOrder()
    {
        return new Order
        {
            OrderNumber = "FT-0002",
            GuestEmail = "guest@test.com",
            DeliveryType = DeliveryType.Courier,
            ShippingAddress = new ShippingAddressSnapshot
            {
                RecipientName = "Maria Ionescu",
                Street = "Str. Exemplu",
                Number = "5",
                City = "Cluj-Napoca",
                County = "Cluj",
                PostalCode = "400001",
                Phone = "0711111111",
            },
            SubtotalRon = 10m,
            ShippingCostRon = 15m,
            TotalRon = 25m,
            Items = new List<OrderItem>
            {
                new()
                {
                    ProductSnapshot = new ProductSnapshot
                    {
                        ProductName = "Foto 13x18",
                        Size = "13x18",
                        Finish = "Mat",
                    },
                    Quantity = 2,
                    UnitPriceRon = 3m,
                    LineTotalRon = 6m,
                },
            },
        };
    }

    // ── FireOrderConfirmedEmail ───────────────────────────────────────────────

    [Fact]
    public async Task FireOrderConfirmedEmail_RegisteredUser_SendsEmailToUserAddress()
    {
        var emailMock = new Mock<IEmailService>();
        emailMock
            .Setup(e => e.SendTemplatedAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var sut = CreateSut(emailMock.Object);
        var order = BuildRegisteredOrder();

        sut.FireOrderConfirmedEmail(order);

        await Task.Delay(200); // allow fire-and-forget to complete

        emailMock.Verify(e => e.SendTemplatedAsync(
            "ion@test.com",
            It.IsAny<string>(),
            "OrderConfirmed",
            It.IsAny<object>(),
            CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task FireOrderConfirmedEmail_GuestOrder_SendsEmailToGuestEmail()
    {
        var emailMock = new Mock<IEmailService>();
        emailMock
            .Setup(e => e.SendTemplatedAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var sut = CreateSut(emailMock.Object);
        var order = BuildGuestOrder();

        sut.FireOrderConfirmedEmail(order);

        await Task.Delay(200);

        emailMock.Verify(e => e.SendTemplatedAsync(
            "guest@test.com",
            It.IsAny<string>(),
            "OrderConfirmed",
            It.IsAny<object>(),
            CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task FireOrderConfirmedEmail_NoEmailAvailable_DoesNotSend()
    {
        var emailMock = new Mock<IEmailService>();
        var sut = CreateSut(emailMock.Object);

        var order = new Order
        {
            OrderNumber = "FT-9999",
            GuestEmail = null,
            DeliveryType = DeliveryType.Courier,
            ShippingAddress = new ShippingAddressSnapshot
            {
                RecipientName = "Unknown",
                Street = "Str.", Number = "1",
                City = "X", County = "X", PostalCode = "000000", Phone = "0",
            },
            Items = new List<OrderItem>(),
        };

        sut.FireOrderConfirmedEmail(order);

        await Task.Delay(100);

        emailMock.Verify(e => e.SendTemplatedAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<object>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // ── FireOrderShippedEmail ─────────────────────────────────────────────────

    [Fact]
    public async Task FireOrderShippedEmail_RegisteredUser_SendsOrderShippedTemplate()
    {
        var emailMock = new Mock<IEmailService>();
        emailMock
            .Setup(e => e.SendTemplatedAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var sut = CreateSut(emailMock.Object);
        var order = BuildRegisteredOrder(awbNumber: "AWB123", trackingUrl: "https://track.example.com");

        sut.FireOrderShippedEmail(order);

        await Task.Delay(200);

        emailMock.Verify(e => e.SendTemplatedAsync(
            "ion@test.com",
            It.IsAny<string>(),
            "OrderShipped",
            It.IsAny<object>(),
            CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task FireOrderShippedEmail_GuestOrder_SendsToGuestEmail()
    {
        var emailMock = new Mock<IEmailService>();
        emailMock
            .Setup(e => e.SendTemplatedAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var sut = CreateSut(emailMock.Object);
        var order = BuildGuestOrder();

        sut.FireOrderShippedEmail(order);

        await Task.Delay(200);

        emailMock.Verify(e => e.SendTemplatedAsync(
            "guest@test.com",
            It.IsAny<string>(),
            "OrderShipped",
            It.IsAny<object>(),
            CancellationToken.None), Times.Once);
    }

    // ── FireOrderDeliveredEmail ───────────────────────────────────────────────

    [Fact]
    public async Task FireOrderDeliveredEmail_RegisteredUser_SendsOrderDeliveredTemplate()
    {
        var emailMock = new Mock<IEmailService>();
        emailMock
            .Setup(e => e.SendTemplatedAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var sut = CreateSut(emailMock.Object);
        var order = BuildRegisteredOrder();

        sut.FireOrderDeliveredEmail(order);

        await Task.Delay(200);

        emailMock.Verify(e => e.SendTemplatedAsync(
            "ion@test.com",
            It.IsAny<string>(),
            "OrderDelivered",
            It.IsAny<object>(),
            CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task FireOrderDeliveredEmail_GuestOrder_SendsToGuestEmail()
    {
        var emailMock = new Mock<IEmailService>();
        emailMock
            .Setup(e => e.SendTemplatedAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var sut = CreateSut(emailMock.Object);
        var order = BuildGuestOrder();

        sut.FireOrderDeliveredEmail(order);

        await Task.Delay(200);

        emailMock.Verify(e => e.SendTemplatedAsync(
            "guest@test.com",
            It.IsAny<string>(),
            "OrderDelivered",
            It.IsAny<object>(),
            CancellationToken.None), Times.Once);
    }

    // ── Easybox delivery ─────────────────────────────────────────────────────

    [Fact]
    public async Task FireOrderConfirmedEmail_EasyboxDelivery_IncludesLockerInfo()
    {
        string? capturedTemplate = null;
        OrderConfirmedEmailModel? capturedModel = null;

        var emailMock = new Mock<IEmailService>();
        emailMock
            .Setup(e => e.SendTemplatedAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .Callback<string, string, string, object, CancellationToken>(
                (_, _, tmpl, model, _) =>
                {
                    capturedTemplate = tmpl;
                    capturedModel = model as OrderConfirmedEmailModel;
                })
            .Returns(Task.CompletedTask);

        var sut = CreateSut(emailMock.Object);

        var locker = new EasyboxLocker
        {
            Name = "Locker Centru",
            Address = "Calea Victoriei 1",
            City = "București",
            County = "Ilfov",
        };

        var order = new Order
        {
            OrderNumber = "FT-0010",
            User = new User { FirstName = "Ana", Email = "ana@test.com" },
            DeliveryType = DeliveryType.Easybox,
            EasyboxLocker = locker,
            ShippingAddress = new ShippingAddressSnapshot
            {
                RecipientName = "Ana Popa",
                Street = "", Number = "", City = "", County = "", PostalCode = "", Phone = "",
            },
            SubtotalRon = 8m,
            ShippingCostRon = 0m,
            TotalRon = 8m,
            Items = new List<OrderItem>
            {
                new()
                {
                    ProductSnapshot = new ProductSnapshot
                    {
                        ProductName = "Foto 10x15", Size = "10x15", Finish = "Lucios",
                    },
                    Quantity = 4,
                    UnitPriceRon = 2m,
                    LineTotalRon = 8m,
                },
            },
        };

        sut.FireOrderConfirmedEmail(order);

        await Task.Delay(200);

        capturedTemplate.Should().Be("OrderConfirmed");
        capturedModel.Should().NotBeNull();
        capturedModel!.IsEasybox.Should().Be(true);
        capturedModel.LockerName.Should().Be("Locker Centru");
        capturedModel.ShippingAddress.Should().BeNull();
    }

    private static (OrderEmailService Sut, Func<OrderConfirmedEmailModel?> Captured) CaptureConfirmed()
    {
        OrderConfirmedEmailModel? captured = null;

        var emailMock = new Mock<IEmailService>();
        emailMock
            .Setup(e => e.SendTemplatedAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .Callback<string, string, string, object, CancellationToken>(
                (_, _, _, model, _) => captured = model as OrderConfirmedEmailModel)
            .Returns(Task.CompletedTask);

        return (CreateSut(emailMock.Object), () => captured);
    }

    [Fact]
    public async Task FireOrderConfirmedEmail_DiscountedOrder_CarriesTheDiscountAndCode()
    {
        var (sut, captured) = CaptureConfirmed();
        var order = BuildGuestOrder();
        order.DiscountRon = 5m;
        order.CouponCode = "VARA30";
        order.TotalRon = 20m;

        sut.FireOrderConfirmedEmail(order);

        await Task.Delay(200);

        var model = captured();
        model.Should().NotBeNull();
        model!.DiscountRon.Should().Be(5m);
        model.CouponCode.Should().Be("VARA30");
        model.TotalRon.Should().Be(20m);
    }

    [Fact]
    public async Task FireOrderConfirmedEmail_UndiscountedOrder_CarriesNoDiscountRow()
    {
        var (sut, captured) = CaptureConfirmed();

        sut.FireOrderConfirmedEmail(BuildGuestOrder());

        await Task.Delay(200);

        var model = captured();
        model.Should().NotBeNull();
        model!.DiscountRon.Should().Be(0m);
        model.CouponCode.Should().BeNull();
    }
}
