using FluentAssertions;
using PhotoPrint.API.Configuration;
using PhotoPrint.API.Models;
using PhotoPrint.API.Services.Sameday;

namespace PhotoPrint.Tests.Unit.Services.Sameday;

public class OrderToAwbRequestMapperTests
{
    private static SamedaySettings Settings(string pickup = "PP1") => new()
    {
        Enabled = true,
        PickupPointId = pickup,
        BaseUrl = "https://api.sameday.ro",
    };

    private static ShippingAddressSnapshot Address(string recipient = "Alice Pop") => new()
    {
        RecipientName = recipient,
        Phone = "+40712345678",
        Street = "Str. Test",
        Number = "10",
        Block = "B",
        City = "Cluj-Napoca",
        County = "Cluj",
        PostalCode = "400000",
    };

    private static Order Order(DeliveryType type, EasyboxLocker? locker = null) => new()
    {
        Id = Guid.NewGuid(),
        OrderNumber = "FT-2026-0001",
        DeliveryType = type,
        ShippingAddress = Address(),
        EasyboxLocker = locker,
        EasyboxLockerId = locker?.Id,
        Items = new List<OrderItem>
        {
            new() { Id = Guid.NewGuid(), Quantity = 3, UnitPriceRon = 1, LineTotalRon = 3,
                    ProductSnapshot = new ProductSnapshot { ProductName = "x", Size = "x", Finish = "x" } },
        },
    };

    [Fact]
    public void Courier_uses_shipping_address_with_street_number_and_block_concat()
    {
        var order = Order(DeliveryType.Courier);
        var req = OrderToAwbRequestMapper.ToRequest(order, Settings());

        req.RecipientName.Should().Be("Alice Pop");
        req.RecipientPhone.Should().Be("+40712345678");
        req.RecipientAddress.Should().Be("Str. Test 10, B");
        req.RecipientCity.Should().Be("Cluj-Napoca");
        req.RecipientCounty.Should().Be("Cluj");
        req.RecipientPostalCode.Should().Be("400000");
    }

    [Fact]
    public void Courier_with_no_block_omits_the_comma_segment()
    {
        var order = Order(DeliveryType.Courier);
        order.ShippingAddress.Block = null;

        var req = OrderToAwbRequestMapper.ToRequest(order, Settings());

        req.RecipientAddress.Should().Be("Str. Test 10");
    }

    [Fact]
    public void Easybox_uses_locker_address_but_shipping_address_recipient_name()
    {
        var locker = new EasyboxLocker
        {
            Id = Guid.NewGuid(),
            SamedayId = "SMD-001",
            Name = "easybox Iulius",
            Address = "Str. Vaida 53-55",
            City = "Cluj-Napoca",
            County = "Cluj",
            Lat = 46.7, Lng = 23.6,
            IsActive = true,
        };
        var order = Order(DeliveryType.Easybox, locker);

        var req = OrderToAwbRequestMapper.ToRequest(order, Settings());

        req.RecipientName.Should().Be("Alice Pop");
        req.RecipientAddress.Should().Be("Str. Vaida 53-55");
        req.RecipientCity.Should().Be("Cluj-Napoca");
        req.RecipientCounty.Should().Be("Cluj");
        req.RecipientPostalCode.Should().NotBeNullOrEmpty(); // sentinel for locker drop-offs
    }

    [Fact]
    public void Throws_when_settings_have_no_pickup_point()
    {
        var order = Order(DeliveryType.Courier);
        var act = () => OrderToAwbRequestMapper.ToRequest(order, Settings(pickup: ""));
        act.Should().Throw<ArgumentException>().WithMessage("*PickupPointId*");
    }

    [Fact]
    public void Throws_when_Easybox_order_has_no_locker_nav()
    {
        var order = Order(DeliveryType.Easybox, locker: null);
        var act = () => OrderToAwbRequestMapper.ToRequest(order, Settings());
        act.Should().Throw<ArgumentException>().WithMessage("*EasyboxLocker*");
    }

    [Fact]
    public void Includes_parcel_weight_in_kilograms()
    {
        var order = Order(DeliveryType.Courier);
        var req = OrderToAwbRequestMapper.ToRequest(order, Settings());
        // 3 prints * 50 + 50 = 200 g = 0.200 kg
        req.ParcelWeightKg.Should().Be(0.200m);
        req.ParcelCount.Should().Be(1);
    }

    [Fact]
    public void Observations_includes_order_number()
    {
        var order = Order(DeliveryType.Courier);
        var req = OrderToAwbRequestMapper.ToRequest(order, Settings());
        req.Observations.Should().Contain("FT-2026-0001");
    }
}
