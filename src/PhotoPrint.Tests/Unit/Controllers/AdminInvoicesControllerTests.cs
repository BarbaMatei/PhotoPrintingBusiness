using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using PhotoPrint.API.Controllers;
using PhotoPrint.API.Data;
using PhotoPrint.API.DTOs.Invoices;
using PhotoPrint.API.Models;
using PhotoPrint.API.Services.Invoicing;
using PhotoPrint.Tests.Helpers;
using Xunit;

namespace PhotoPrint.Tests.Unit.Controllers;

public class AdminInvoicesControllerTests
{
    private readonly PhotoPrintDbContext _db = new(
        new DbContextOptionsBuilder<PhotoPrintDbContext>()
            .UseInMemoryDatabase($"AdminInvoices_{Guid.NewGuid():N}")
            .Options);

    private static AdminInvoicesController MakeController(
        PhotoPrintDbContext db, IInvoiceLifecycle lifecycle, LogCapture logs, Guid adminUserId)
        => new(db, lifecycle, logs.LoggerFor<AdminInvoicesController>())
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(
                        [new Claim(ClaimTypes.NameIdentifier, adminUserId.ToString())], authenticationType: "Test")),
                },
            },
        };

    [Fact]
    public async Task ListAsync_LogsAdminUserId()
    {
        var adminId = Guid.NewGuid();
        var logs = new LogCapture();
        var controller = MakeController(_db, Mock.Of<IInvoiceLifecycle>(), logs, adminId);

        await controller.ListAsync(new AdminInvoiceListQuery(), CancellationToken.None);

        logs.Records.Should().ContainSingle(
            r => r.Message.StartsWith("admin.invoice.list", StringComparison.Ordinal) &&
                 r.Message.Contains(adminId.ToString()));
    }

    [Fact]
    public async Task GetXmlAsync_LogsAdminUserId()
    {
        var adminId = Guid.NewGuid();
        var invoiceId = Guid.NewGuid();
        _db.Invoices.Add(new Invoice
        {
            Id = invoiceId, OrderId = Guid.NewGuid(), InvoiceNumber = "FT-2026-00001",
            Series = "FT", Number = 1, XmlPayload = "<Invoice/>",
        });
        await _db.SaveChangesAsync();
        var logs = new LogCapture();
        var controller = MakeController(_db, Mock.Of<IInvoiceLifecycle>(), logs, adminId);

        await controller.GetXmlAsync(invoiceId, CancellationToken.None);

        logs.Records.Should().ContainSingle(
            r => r.Message.StartsWith("admin.invoice.xml-download", StringComparison.Ordinal) &&
                 r.Message.Contains(adminId.ToString()) &&
                 r.Message.Contains(invoiceId.ToString()));
    }

    [Fact]
    public async Task RetryAsync_LogsAdminUserId()
    {
        var adminId = Guid.NewGuid();
        var invoiceId = Guid.NewGuid();
        _db.Invoices.Add(new Invoice
        {
            Id = invoiceId, OrderId = Guid.NewGuid(), InvoiceNumber = "FT-2026-00001",
            Series = "FT", Number = 1, AnafStatus = InvoiceAnafStatus.Rejected,
        });
        await _db.SaveChangesAsync();
        var lifecycle = new Mock<IInvoiceLifecycle>();
        lifecycle.Setup(l => l.RetryAsync(invoiceId, InvoiceAnafStatus.Rejected, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(true);
        var logs = new LogCapture();
        var controller = MakeController(_db, lifecycle.Object, logs, adminId);

        await controller.RetryAsync(invoiceId, CancellationToken.None);

        logs.Records.Should().ContainSingle(
            r => r.Message.StartsWith("admin.invoice.retry", StringComparison.Ordinal) &&
                 r.Message.Contains(adminId.ToString()));
    }
}
