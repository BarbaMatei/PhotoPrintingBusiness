using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using PhotoPrint.API.Authentication;
using PhotoPrint.API.Controllers;
using PhotoPrint.API.Data;
using PhotoPrint.API.Models;
using PhotoPrint.API.Services;
using Xunit;

namespace PhotoPrint.Tests.Unit.Controllers;

public class InvoicesControllerTests
{
    private readonly PhotoPrintDbContext _db;

    public InvoicesControllerTests()
    {
        _db = new PhotoPrintDbContext(
            new DbContextOptionsBuilder<PhotoPrintDbContext>()
                .UseInMemoryDatabase($"Invoices_{Guid.NewGuid():N}")
                .Options);
    }

    private static Order MakeOrder(Guid id, Guid? userId = null, Guid? guestSessionId = null) => new()
    {
        Id = id,
        UserId = userId,
        GuestSessionId = guestSessionId,
        OrderNumber = $"ORD-{id:N}",
        ShippingAddress = new ShippingAddressSnapshot
        {
            Street = "Str. Test", Number = "1", City = "Cluj-Napoca", County = "Cluj",
            PostalCode = "400000", RecipientName = "Test User", Phone = "0700000000",
        },
    };

    private static InvoicesController MakeController(PhotoPrintDbContext db, IStorageRouter router, Guid userId) =>
        MakeControllerWithClaim(db, router, new Claim(ClaimTypes.NameIdentifier, userId.ToString()));

    private static InvoicesController MakeGuestController(PhotoPrintDbContext db, IStorageRouter router, Guid guestSessionId) =>
        MakeControllerWithClaim(db, router, new Claim(GuestAuthenticationHandler.GuestSessionIdClaimType, guestSessionId.ToString()));

    private static InvoicesController MakeControllerWithClaim(PhotoPrintDbContext db, IStorageRouter router, Claim claim) =>
        new(db, router)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity([claim], authenticationType: "Test")),
                },
            },
        };

    [Fact]
    public async Task GetInvoiceAsync_CloudEnabled_ReadsFromCloudAdapterNotLocal()
    {
        var userId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        _db.Orders.Add(MakeOrder(orderId, userId));
        _db.Invoices.Add(new Invoice
        {
            OrderId = orderId,
            InvoiceNumber = "FT-2026-00001",
            PdfStoragePath = "invoices/2026/FT-2026-00001.pdf",
        });
        await _db.SaveChangesAsync();

        var cloud = new Mock<IStorageService>();
        cloud.Setup(s => s.GetStreamAsync("invoices/2026/FT-2026-00001.pdf", It.IsAny<CancellationToken>()))
             .ReturnsAsync(new MemoryStream([1, 2, 3]));
        var local = new Mock<IStorageService>();

        var router = new Mock<IStorageRouter>();
        router.SetupGet(r => r.CloudEnabled).Returns(true);
        router.SetupGet(r => r.Cloud).Returns(cloud.Object);
        router.SetupGet(r => r.Local).Returns(local.Object);

        var controller = MakeController(_db, router.Object, userId);

        var result = await controller.GetInvoiceAsync(orderId, CancellationToken.None);

        result.Should().BeOfType<FileStreamResult>();
        cloud.Verify(s => s.GetStreamAsync("invoices/2026/FT-2026-00001.pdf", It.IsAny<CancellationToken>()), Times.Once);
        local.Verify(s => s.GetStreamAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetInvoiceAsync_CloudDisabled_ReadsFromLocalAdapter()
    {
        var userId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        _db.Orders.Add(MakeOrder(orderId, userId));
        _db.Invoices.Add(new Invoice
        {
            OrderId = orderId,
            InvoiceNumber = "FT-2026-00002",
            PdfStoragePath = "invoices/2026/FT-2026-00002.pdf",
        });
        await _db.SaveChangesAsync();

        var local = new Mock<IStorageService>();
        local.Setup(s => s.GetStreamAsync("invoices/2026/FT-2026-00002.pdf", It.IsAny<CancellationToken>()))
             .ReturnsAsync(new MemoryStream([1, 2, 3]));

        var router = new Mock<IStorageRouter>();
        router.SetupGet(r => r.CloudEnabled).Returns(false);
        router.SetupGet(r => r.Local).Returns(local.Object);

        var controller = MakeController(_db, router.Object, userId);

        var result = await controller.GetInvoiceAsync(orderId, CancellationToken.None);

        result.Should().BeOfType<FileStreamResult>();
        local.Verify(s => s.GetStreamAsync("invoices/2026/FT-2026-00002.pdf", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetInvoiceAsync_GuestOwnsOrder_ReturnsFile()
    {
        var guestSessionId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        _db.Orders.Add(MakeOrder(orderId, guestSessionId: guestSessionId));
        _db.Invoices.Add(new Invoice
        {
            OrderId = orderId,
            InvoiceNumber = "FT-2026-00003",
            PdfStoragePath = "invoices/2026/FT-2026-00003.pdf",
        });
        await _db.SaveChangesAsync();

        var local = new Mock<IStorageService>();
        local.Setup(s => s.GetStreamAsync("invoices/2026/FT-2026-00003.pdf", It.IsAny<CancellationToken>()))
             .ReturnsAsync(new MemoryStream([1, 2, 3]));
        var router = new Mock<IStorageRouter>();
        router.SetupGet(r => r.CloudEnabled).Returns(false);
        router.SetupGet(r => r.Local).Returns(local.Object);

        var controller = MakeGuestController(_db, router.Object, guestSessionId);

        var result = await controller.GetInvoiceAsync(orderId, CancellationToken.None);

        result.Should().BeOfType<FileStreamResult>();
    }

    [Fact]
    public async Task GetInvoiceAsync_DifferentUserOwnsOrder_ReturnsForbid()
    {
        var orderId = Guid.NewGuid();
        _db.Orders.Add(MakeOrder(orderId, Guid.NewGuid()));
        await _db.SaveChangesAsync();

        var controller = MakeController(_db, Mock.Of<IStorageRouter>(), Guid.NewGuid());

        var result = await controller.GetInvoiceAsync(orderId, CancellationToken.None);

        result.Should().BeOfType<ForbidResult>();
    }

    [Fact]
    public async Task GetInvoiceAsync_GuestSessionDoesNotMatch_ReturnsForbid()
    {
        var orderId = Guid.NewGuid();
        _db.Orders.Add(MakeOrder(orderId, guestSessionId: Guid.NewGuid()));
        await _db.SaveChangesAsync();

        var controller = MakeGuestController(_db, Mock.Of<IStorageRouter>(), Guid.NewGuid());

        var result = await controller.GetInvoiceAsync(orderId, CancellationToken.None);

        result.Should().BeOfType<ForbidResult>();
    }
}
