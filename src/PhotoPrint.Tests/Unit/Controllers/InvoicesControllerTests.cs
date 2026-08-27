using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using PhotoPrint.API.Authentication;
using PhotoPrint.API.Configuration;
using PhotoPrint.API.Controllers;
using PhotoPrint.API.Data;
using PhotoPrint.API.Models;
using PhotoPrint.API.Services;
using PhotoPrint.Tests.Helpers;
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

    private static InvoicesController MakeController(PhotoPrintDbContext db, IStorageRouter router, Guid userId, LogCapture? logs = null) =>
        MakeControllerWithClaim(db, router, new Claim(ClaimTypes.NameIdentifier, userId.ToString()), logs);

    private static InvoicesController MakeAdminController(PhotoPrintDbContext db, IStorageRouter router, LogCapture? logs = null) =>
        new(db, router, AnafOptions(), logs is null ? NullLogger<InvoicesController>.Instance : logs.LoggerFor<InvoicesController>())
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(
                        [new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
                         new Claim(ClaimTypes.Role, "Admin")],
                        authenticationType: "Test")),
                },
            },
        };

    private static InvoicesController MakeGuestController(PhotoPrintDbContext db, IStorageRouter router, Guid guestSessionId) =>
        MakeControllerWithClaim(db, router, new Claim(GuestAuthenticationHandler.GuestSessionIdClaimType, guestSessionId.ToString()));

    [Fact]
    public async Task GetInvoice_PdfNotRenderedYet_SendsARetryAfterMatchingTheProducerInterval()
    {
        var userId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        _db.Orders.Add(MakeOrder(orderId, userId));
        _db.Invoices.Add(new Invoice
        {
            OrderId = orderId,
            InvoiceNumber = "FT-2026-00099",
            AnafStatus = InvoiceAnafStatus.Pending,
        });
        await _db.SaveChangesAsync();

        var controller = MakeController(_db, new Mock<IStorageRouter>().Object, userId);

        var result = await controller.GetInvoiceAsync(orderId, CancellationToken.None);

        result.Should().BeOfType<NotFoundResult>();
        controller.Response.Headers["Retry-After"].ToString().Should().Be("1800",
            "the 30-minute poll is the only producer of the PDF, so a 30-second hint just burns requests");
    }
    private static int _pollIntervalMinutes = 30;

    private static IOptions<AnafSettings> AnafOptions() =>
        Options.Create(new AnafSettings { PollIntervalMinutes = _pollIntervalMinutes });

    private static InvoicesController MakeControllerWithClaim(PhotoPrintDbContext db, IStorageRouter router, Claim claim, LogCapture? logs = null) =>
        new(db, router, AnafOptions(), logs is null ? NullLogger<InvoicesController>.Instance : logs.LoggerFor<InvoicesController>())
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
            StorageLocation = StorageLocation.Cloud,
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
        router.Setup(r => r.For(StorageLocation.Cloud)).Returns(cloud.Object);
        router.Setup(r => r.For(StorageLocation.Local)).Returns(local.Object);

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
        router.Setup(r => r.For(StorageLocation.Local)).Returns(local.Object);

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
        router.Setup(r => r.For(StorageLocation.Local)).Returns(local.Object);

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

    // Without the stamped tier a read follows the live provider flag, so flipping it orphans every PDF.
    [Fact]
    public async Task GetInvoiceAsync_RowStampedLocalWhileCloudIsOn_ReadsLocalNotCloud()
    {
        var userId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        _db.Orders.Add(MakeOrder(orderId, userId));
        _db.Invoices.Add(new Invoice
        {
            OrderId = orderId,
            InvoiceNumber = "FT-2026-00007",
            PdfStoragePath = "invoices/2026/FT-2026-00007.pdf",
            StorageLocation = StorageLocation.Local,
        });
        await _db.SaveChangesAsync();

        var local = new Mock<IStorageService>();
        local.Setup(s => s.GetStreamAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
             .ReturnsAsync(new MemoryStream([1, 2, 3]));
        var cloud = new Mock<IStorageService>();

        var router = new Mock<IStorageRouter>();
        router.SetupGet(r => r.CloudEnabled).Returns(true);
        router.SetupGet(r => r.Local).Returns(local.Object);
        router.SetupGet(r => r.Cloud).Returns(cloud.Object);
        router.Setup(r => r.For(StorageLocation.Local)).Returns(local.Object);
        router.Setup(r => r.For(StorageLocation.Cloud)).Returns(cloud.Object);

        var result = await MakeController(_db, router.Object, userId)
            .GetInvoiceAsync(orderId, CancellationToken.None);

        result.Should().BeOfType<FileStreamResult>();
        local.Verify(s => s.GetStreamAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
        cloud.Verify(s => s.GetStreamAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetInvoiceAsync_RowStampedCloudWhileCloudIsOff_DoesNotThrowAndReports404()
    {
        var userId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        _db.Orders.Add(MakeOrder(orderId, userId));
        _db.Invoices.Add(new Invoice
        {
            OrderId = orderId,
            InvoiceNumber = "FT-2026-00008",
            PdfStoragePath = "invoices/2026/FT-2026-00008.pdf",
            StorageLocation = StorageLocation.Cloud,
        });
        await _db.SaveChangesAsync();

        var local = new Mock<IStorageService>();
        local.Setup(s => s.GetStreamAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
             .ThrowsAsync(new FileNotFoundException("not here"));
        var router = new Mock<IStorageRouter>();
        router.SetupGet(r => r.CloudEnabled).Returns(false);
        router.SetupGet(r => r.Local).Returns(local.Object);
        router.Setup(r => r.For(StorageLocation.Local)).Returns(local.Object);
        // Cloud would throw InvalidOperationException if resolved; the controller must not resolve it.
        router.SetupGet(r => r.Cloud).Throws(new InvalidOperationException("cloud tier is off"));

        var logs = new LogCapture();
        var result = await MakeController(_db, router.Object, userId, logs)
            .GetInvoiceAsync(orderId, CancellationToken.None);

        result.Should().BeOfType<ObjectResult>()
            .Which.StatusCode.Should().Be(StatusCodes.Status404NotFound);
        logs.Records.Should().ContainSingle(
            r => r.Message.StartsWith("invoice.pdf.blob-missing", StringComparison.Ordinal));
    }

    // Must stay distinguishable from the not-yet-rendered 404 that tells the caller to retry.
    [Fact]
    public async Task GetInvoiceAsync_BlobIsMissing_LogsADistinctEventAndDoesNotInviteARetry()
    {
        var userId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        _db.Orders.Add(MakeOrder(orderId, userId));
        _db.Invoices.Add(new Invoice
        {
            OrderId = orderId,
            InvoiceNumber = "FT-2026-00042",
            Series = "FT",
            Number = 42,
            IssuedAt = new DateTimeOffset(2026, 6, 3, 12, 0, 0, TimeSpan.Zero),
            PdfStoragePath = "invoices/2026/FT-2026-00042.pdf",
        });
        await _db.SaveChangesAsync();

        var local = new Mock<IStorageService>();
        local.Setup(s => s.GetStreamAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
             .ThrowsAsync(new FileNotFoundException("gone"));
        var router = new Mock<IStorageRouter>();
        router.SetupGet(r => r.CloudEnabled).Returns(false);
        router.SetupGet(r => r.Local).Returns(local.Object);
        router.Setup(r => r.For(StorageLocation.Local)).Returns(local.Object);

        var logs = new LogCapture();
        var controller = MakeController(_db, router.Object, userId, logs);

        var result = await controller.GetInvoiceAsync(orderId, CancellationToken.None);

        result.Should().BeOfType<ObjectResult>()
            .Which.StatusCode.Should().Be(StatusCodes.Status404NotFound);
        controller.Response.Headers.Should().NotContainKey("Retry-After");
        logs.Records.Should().ContainSingle(
            r => r.Level == Microsoft.Extensions.Logging.LogLevel.Error &&
                 r.Message.StartsWith("invoice.pdf.blob-missing", StringComparison.Ordinal) &&
                 r.Message.Contains("FT-2026-00042"));
    }

    [Fact]
    public async Task GetInvoiceAsync_StampedCloudButBlobIsOnLocal_FallsBackAndLogsTheMismatch()
    {
        var userId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        _db.Orders.Add(MakeOrder(orderId, userId));
        _db.Invoices.Add(new Invoice
        {
            OrderId = orderId,
            InvoiceNumber = "FT-2026-00050",
            PdfStoragePath = "invoices/2026/FT-2026-00050.pdf",
            StorageLocation = StorageLocation.Cloud,
        });
        await _db.SaveChangesAsync();

        var cloud = new Mock<IStorageService>();
        cloud.Setup(s => s.GetStreamAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
             .ThrowsAsync(new FileNotFoundException("not in the bucket"));
        var local = new Mock<IStorageService>();
        local.Setup(s => s.GetStreamAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
             .ReturnsAsync(new MemoryStream([1, 2, 3]));

        var router = new Mock<IStorageRouter>();
        router.SetupGet(r => r.CloudEnabled).Returns(true);
        router.SetupGet(r => r.Cloud).Returns(cloud.Object);
        router.SetupGet(r => r.Local).Returns(local.Object);
        router.Setup(r => r.For(StorageLocation.Cloud)).Returns(cloud.Object);
        router.Setup(r => r.For(StorageLocation.Local)).Returns(local.Object);

        var logs = new LogCapture();
        var result = await MakeController(_db, router.Object, userId, logs)
            .GetInvoiceAsync(orderId, CancellationToken.None);

        result.Should().BeOfType<FileStreamResult>();
        cloud.Verify(s => s.GetStreamAsync("invoices/2026/FT-2026-00050.pdf", It.IsAny<CancellationToken>()), Times.Once);
        local.Verify(s => s.GetStreamAsync("invoices/2026/FT-2026-00050.pdf", It.IsAny<CancellationToken>()), Times.Once);
        logs.Records.Should().ContainSingle(
            r => r.Level == Microsoft.Extensions.Logging.LogLevel.Warning &&
                 r.Message.StartsWith("invoice.pdf.tier-mismatch", StringComparison.Ordinal) &&
                 r.Message.Contains("FT-2026-00050"));
    }

    [Fact]
    public async Task GetInvoiceAsync_StampedLocalButBlobIsOnCloud_FallsBackAndLogsTheMismatch()
    {
        var userId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        _db.Orders.Add(MakeOrder(orderId, userId));
        _db.Invoices.Add(new Invoice
        {
            OrderId = orderId,
            InvoiceNumber = "FT-2026-00051",
            PdfStoragePath = "invoices/2026/FT-2026-00051.pdf",
            StorageLocation = StorageLocation.Local,
        });
        await _db.SaveChangesAsync();

        var local = new Mock<IStorageService>();
        local.Setup(s => s.GetStreamAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
             .ThrowsAsync(new FileNotFoundException("not on disk"));
        var cloud = new Mock<IStorageService>();
        cloud.Setup(s => s.GetStreamAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
             .ReturnsAsync(new MemoryStream([1, 2, 3]));

        var router = new Mock<IStorageRouter>();
        router.SetupGet(r => r.CloudEnabled).Returns(true);
        router.SetupGet(r => r.Cloud).Returns(cloud.Object);
        router.SetupGet(r => r.Local).Returns(local.Object);
        router.Setup(r => r.For(StorageLocation.Cloud)).Returns(cloud.Object);
        router.Setup(r => r.For(StorageLocation.Local)).Returns(local.Object);

        var logs = new LogCapture();
        var result = await MakeController(_db, router.Object, userId, logs)
            .GetInvoiceAsync(orderId, CancellationToken.None);

        result.Should().BeOfType<FileStreamResult>();
        local.Verify(s => s.GetStreamAsync("invoices/2026/FT-2026-00051.pdf", It.IsAny<CancellationToken>()), Times.Once);
        cloud.Verify(s => s.GetStreamAsync("invoices/2026/FT-2026-00051.pdf", It.IsAny<CancellationToken>()), Times.Once);
        logs.Records.Should().ContainSingle(
            r => r.Level == Microsoft.Extensions.Logging.LogLevel.Warning &&
                 r.Message.StartsWith("invoice.pdf.tier-mismatch", StringComparison.Ordinal) &&
                 r.Message.Contains("FT-2026-00051"));
    }

    // A miss on the fallback tier is still a missing blob, not a server fault.
    [Fact]
    public async Task GetInvoiceAsync_BlobIsMissingFromBothTiers_Returns404AndLogsBlobMissing()
    {
        var userId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        _db.Orders.Add(MakeOrder(orderId, userId));
        _db.Invoices.Add(new Invoice
        {
            OrderId = orderId,
            InvoiceNumber = "FT-2026-00052",
            PdfStoragePath = "invoices/2026/FT-2026-00052.pdf",
            StorageLocation = StorageLocation.Cloud,
        });
        await _db.SaveChangesAsync();

        var cloud = new Mock<IStorageService>();
        cloud.Setup(s => s.GetStreamAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
             .ThrowsAsync(new FileNotFoundException("not in the bucket"));
        var local = new Mock<IStorageService>();
        local.Setup(s => s.GetStreamAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
             .ThrowsAsync(new FileNotFoundException("not on disk either"));

        var router = new Mock<IStorageRouter>();
        router.SetupGet(r => r.CloudEnabled).Returns(true);
        router.SetupGet(r => r.Cloud).Returns(cloud.Object);
        router.SetupGet(r => r.Local).Returns(local.Object);
        router.Setup(r => r.For(StorageLocation.Cloud)).Returns(cloud.Object);
        router.Setup(r => r.For(StorageLocation.Local)).Returns(local.Object);

        var logs = new LogCapture();
        var controller = MakeController(_db, router.Object, userId, logs);

        var result = await controller.GetInvoiceAsync(orderId, CancellationToken.None);

        result.Should().BeOfType<ObjectResult>()
            .Which.StatusCode.Should().Be(StatusCodes.Status404NotFound);
        controller.Response.Headers.Should().NotContainKey("Retry-After");
        logs.Records.Should().ContainSingle(
            r => r.Level == Microsoft.Extensions.Logging.LogLevel.Error &&
                 r.Message.StartsWith("invoice.pdf.blob-missing", StringComparison.Ordinal) &&
                 r.Message.Contains("FT-2026-00052") &&
                 r.Message.Contains("tiers_tried=2"));
        logs.Records.Should().NotContain(
            r => r.Message.StartsWith("invoice.pdf.tier-mismatch", StringComparison.Ordinal));
    }
    [Fact]
    public async Task GetInvoiceAsync_AdminReadsAnotherCustomersInvoice_ReturnsFileAndLogsTheAccess()
    {
        var orderId = Guid.NewGuid();
        _db.Orders.Add(MakeOrder(orderId, Guid.NewGuid()));
        _db.Invoices.Add(new Invoice
        {
            OrderId = orderId,
            InvoiceNumber = "FT-2026-09001",
            PdfStoragePath = "invoices/2026/FT-2026-09001.pdf",
        });
        await _db.SaveChangesAsync();

        var local = new Mock<IStorageService>();
        local.Setup(x => x.GetStreamAsync("invoices/2026/FT-2026-09001.pdf", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MemoryStream([1, 2, 3]));
        var router = new Mock<IStorageRouter>();
        router.SetupGet(r => r.CloudEnabled).Returns(false);
        router.SetupGet(r => r.Local).Returns(local.Object);
        router.Setup(r => r.For(StorageLocation.Local)).Returns(local.Object);

        var logs = new LogCapture();
        var controller = MakeAdminController(_db, router.Object, logs);

        var result = await controller.GetInvoiceAsync(orderId, CancellationToken.None);

        result.Should().BeOfType<FileStreamResult>();
        logs.CountStartingWith("invoice.pdf.admin-read", LogLevel.Information).Should().Be(1);
    }

    [Fact]
    public async Task GetInvoiceAsync_NonAdminStillCannotReadAnotherCustomersInvoice()
    {
        var orderId = Guid.NewGuid();
        _db.Orders.Add(MakeOrder(orderId, Guid.NewGuid()));
        _db.Invoices.Add(new Invoice
        {
            OrderId = orderId,
            InvoiceNumber = "FT-2026-09002",
            PdfStoragePath = "invoices/2026/FT-2026-09002.pdf",
        });
        await _db.SaveChangesAsync();

        var controller = MakeController(_db, Mock.Of<IStorageRouter>(), Guid.NewGuid());

        var result = await controller.GetInvoiceAsync(orderId, CancellationToken.None);

        result.Should().BeOfType<ForbidResult>();
    }
}