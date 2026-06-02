using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using PhotoPrint.API.Configuration;
using PhotoPrint.API.Data;
using PhotoPrint.API.Exceptions;
using PhotoPrint.API.Models;
using PhotoPrint.API.Services.Sameday;

namespace PhotoPrint.Tests.Unit.Services.Sameday;

/// <summary>
/// Full outcome-matrix for <see cref="AwbCreator"/>. Pinned tests for the
/// ADR-015 load-bearing re-check (<c>Status == Paid AND AwbNumber IS NULL</c>):
/// removing that guard breaks <c>Returns_Skipped_when_AwbNumber_already_populated</c>.
/// </summary>
public class AwbCreatorTests
{
    private static PhotoPrintDbContext CreateDb() =>
        new(new DbContextOptionsBuilder<PhotoPrintDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static SamedaySettings Settings() => new()
    {
        Enabled = true,
        PickupPointId = "PP1",
    };

    private static AwbCreator Build(PhotoPrintDbContext db, Mock<ISamedayClient> client)
    {
        var clock = new FakeTimeProvider(new DateTimeOffset(2026, 6, 2, 12, 0, 0, TimeSpan.Zero));
        return new AwbCreator(
            db, client.Object, Options.Create(Settings()), clock,
            new LoggerFactory().CreateLogger<AwbCreator>());
    }

    private static Order SeedOrder(
        PhotoPrintDbContext db,
        OrderStatus status = OrderStatus.Paid,
        string? awbNumber = null,
        bool withItems = true,
        DeliveryType deliveryType = DeliveryType.Courier)
    {
        var order = new Order
        {
            Id = Guid.NewGuid(),
            OrderNumber = $"FT-{Random.Shared.Next(100_000, 999_999)}",
            Status = status,
            AwbNumber = awbNumber,
            DeliveryType = deliveryType,
            PaidAt = DateTimeOffset.UtcNow,
            ShippingAddress = new ShippingAddressSnapshot
            {
                RecipientName = "Alice Pop", Phone = "+40712345678",
                Street = "Str. Test", Number = "10",
                City = "Cluj-Napoca", County = "Cluj", PostalCode = "400000",
            },
        };
        db.Orders.Add(order);
        if (withItems)
        {
            db.OrderItems.Add(new OrderItem
            {
                Id = Guid.NewGuid(),
                OrderId = order.Id, Order = order,
                UploadId = Guid.NewGuid(),
                ProductId = Guid.NewGuid(),
                Quantity = 3, UnitPriceRon = 1, LineTotalRon = 3,
                ProductSnapshot = new ProductSnapshot { ProductName = "x", Size = "x", Finish = "x" },
            });
        }
        db.SaveChanges();
        return order;
    }

    [Fact]
    public async Task Returns_Skipped_when_order_not_found()
    {
        using var db = CreateDb();
        var client = new Mock<ISamedayClient>(MockBehavior.Strict);
        var sut = Build(db, client);

        var outcome = await sut.CreateForOrderAsync(Guid.NewGuid(), attempt: 1);

        outcome.Should().BeOfType<AwbCreationOutcome.Skipped>()
            .Which.Reason.Should().Contain("not found");
        client.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Returns_Skipped_when_status_is_not_Paid()
    {
        using var db = CreateDb();
        var order = SeedOrder(db, status: OrderStatus.Cancelled);
        var client = new Mock<ISamedayClient>(MockBehavior.Strict);
        var sut = Build(db, client);

        var outcome = await sut.CreateForOrderAsync(order.Id, attempt: 1);

        outcome.Should().BeOfType<AwbCreationOutcome.Skipped>();
        client.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Returns_Skipped_when_AwbNumber_already_populated()
    {
        // ADR-015 load-bearing re-check. Removing the IsNullOrWhiteSpace guard
        // in AwbCreator breaks this test.
        using var db = CreateDb();
        var order = SeedOrder(db, awbNumber: "RO12345678");
        var client = new Mock<ISamedayClient>(MockBehavior.Strict);
        var sut = Build(db, client);

        var outcome = await sut.CreateForOrderAsync(order.Id, attempt: 1);

        outcome.Should().BeOfType<AwbCreationOutcome.Skipped>()
            .Which.Reason.Should().Contain("AwbNumber");
        client.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Returns_Created_and_persists_AwbNumber_and_LabelUrl_on_happy_path()
    {
        using var db = CreateDb();
        var order = SeedOrder(db);

        var client = new Mock<ISamedayClient>();
        client.Setup(c => c.CreateAwbAsync(It.IsAny<AwbCreationRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AwbCreationResult("RO12345678", "https://sameday/labels/abc.pdf", 18.50m));

        var sut = Build(db, client);
        var outcome = await sut.CreateForOrderAsync(order.Id, attempt: 1);

        var created = outcome.Should().BeOfType<AwbCreationOutcome.Created>().Subject;
        created.AwbNumber.Should().Be("RO12345678");
        created.LabelUrl.Should().Be("https://sameday/labels/abc.pdf");

        var refreshed = await db.Orders.FindAsync(order.Id);
        refreshed!.AwbNumber.Should().Be("RO12345678");
        refreshed.AwbLabelUrl.Should().Be("https://sameday/labels/abc.pdf");
    }

    [Fact]
    public async Task Returns_GiveUp_when_mapper_throws_for_invalid_input()
    {
        using var db = CreateDb();
        var order = SeedOrder(db, withItems: false);
        var client = new Mock<ISamedayClient>(MockBehavior.Strict);
        var sut = Build(db, client);

        var outcome = await sut.CreateForOrderAsync(order.Id, attempt: 1);

        outcome.Should().BeOfType<AwbCreationOutcome.GiveUp>()
            .Which.Reason.Should().Contain("invalid request");
        client.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Returns_RetryLater_transient_on_SamedayUnreachableException()
    {
        using var db = CreateDb();
        var order = SeedOrder(db);

        var client = new Mock<ISamedayClient>();
        client.Setup(c => c.CreateAwbAsync(It.IsAny<AwbCreationRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new SamedayUnreachableException("/api/awb"));

        var sut = Build(db, client);
        var outcome = await sut.CreateForOrderAsync(order.Id, attempt: 1);

        var retry = outcome.Should().BeOfType<AwbCreationOutcome.RetryLater>().Subject;
        retry.IsTransient.Should().BeTrue();
    }

    [Fact]
    public async Task Returns_RetryLater_non_transient_on_SamedayAuthException()
    {
        using var db = CreateDb();
        var order = SeedOrder(db);

        var client = new Mock<ISamedayClient>();
        client.Setup(c => c.CreateAwbAsync(It.IsAny<AwbCreationRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new SamedayAuthException("/api/awb"));

        var sut = Build(db, client);
        var outcome = await sut.CreateForOrderAsync(order.Id, attempt: 1);

        var retry = outcome.Should().BeOfType<AwbCreationOutcome.RetryLater>().Subject;
        retry.IsTransient.Should().BeFalse();
    }

    [Fact]
    public async Task Returns_RetryLater_non_transient_on_SamedayProtocolException()
    {
        using var db = CreateDb();
        var order = SeedOrder(db);

        var client = new Mock<ISamedayClient>();
        client.Setup(c => c.CreateAwbAsync(It.IsAny<AwbCreationRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new SamedayProtocolException("/api/awb", "bad shape"));

        var sut = Build(db, client);
        var outcome = await sut.CreateForOrderAsync(order.Id, attempt: 1);

        var retry = outcome.Should().BeOfType<AwbCreationOutcome.RetryLater>().Subject;
        retry.IsTransient.Should().BeFalse();
    }

    [Fact]
    public async Task Returns_GiveUp_on_SamedayValidationException()
    {
        using var db = CreateDb();
        var order = SeedOrder(db);

        var client = new Mock<ISamedayClient>();
        client.Setup(c => c.CreateAwbAsync(It.IsAny<AwbCreationRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new SamedayValidationException("/api/awb", 422, "validation failed"));

        var sut = Build(db, client);
        var outcome = await sut.CreateForOrderAsync(order.Id, attempt: 1);

        outcome.Should().BeOfType<AwbCreationOutcome.GiveUp>();
    }
}
