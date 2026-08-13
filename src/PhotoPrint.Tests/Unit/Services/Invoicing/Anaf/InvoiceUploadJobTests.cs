using System.Reflection;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using PhotoPrint.API.Configuration;
using PhotoPrint.API.Data;
using PhotoPrint.API.Models;
using PhotoPrint.API.Services;
using PhotoPrint.API.Services.Invoicing;
using PhotoPrint.API.Services.Invoicing.Anaf;
using PhotoPrint.Tests.Helpers;
using Xunit;

namespace PhotoPrint.Tests.Unit.Services.Invoicing.Anaf;

public class InvoiceUploadJobTests
{
    private static PhotoPrintDbContext CreateDb(string name) =>
        new(new DbContextOptionsBuilder<PhotoPrintDbContext>()
            .UseInMemoryDatabase(name)
            .Options);

    private static Order MakeOrder(Guid id) => new()
    {
        Id = id,
        OrderNumber = $"FT-{Random.Shared.Next(100_000, 999_999)}",
        ShippingAddress = new ShippingAddressSnapshot
        {
            RecipientName = "x", Phone = "x",
            Street = "x", Number = "1",
            City = "x", County = "x", PostalCode = "x",
        },
    };

    private static Invoice MakeInvoice(Guid orderId, string? xmlPayload = "<Invoice/>") => new()
    {
        OrderId = orderId,
        InvoiceNumber = "FT-2026-00001",
        Series = "FT",
        Number = 1,
        XmlPayload = xmlPayload,
    };

    private static Task InvokeUploadPendingAsync(InvoiceUploadJob job, IServiceProvider sp, Guid invoiceId, Guid orderId)
    {
        var method = typeof(InvoiceUploadJob).GetMethod("UploadPendingAsync",
            BindingFlags.NonPublic | BindingFlags.Instance)!;
        return (Task)method.Invoke(job, [sp, invoiceId, orderId, CancellationToken.None])!;
    }

    private sealed class Harness
    {
        public required InvoiceUploadJob Job { get; init; }
        public required IServiceProvider Sp { get; init; }
        public required Mock<IStorageRouter> Router { get; init; }
        public required Mock<IStorageService> Cloud { get; init; }
        public required Mock<IStorageService> Local { get; init; }
        public required Mock<IInvoiceLifecycle> Lifecycle { get; init; }
        public required Mock<IAnafSpvClient> AnafClient { get; init; }
    }

    private static Harness Build(string dbName, bool cloudEnabled, LogCapture? logCapture = null)
    {
        var router = new Mock<IStorageRouter>();
        var cloud = new Mock<IStorageService>();
        var local = new Mock<IStorageService>();
        router.SetupGet(r => r.CloudEnabled).Returns(cloudEnabled);
        router.SetupGet(r => r.Cloud).Returns(cloud.Object);
        router.SetupGet(r => r.Local).Returns(local.Object);

        var xmlBuilder = new Mock<IInvoiceXmlBuilder>();
        var pdfRenderer = new Mock<IInvoicePdfRenderer>();
        pdfRenderer.Setup(r => r.Render(It.IsAny<Order>(), It.IsAny<Invoice>(), It.IsAny<SellerSettings>()))
                   .Returns([1, 2, 3, 4]);

        var anafClient = new Mock<IAnafSpvClient>();
        var lifecycle = new Mock<IInvoiceLifecycle>();
        lifecycle.Setup(l => l.MarkSubmittedAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(true);

        var services = new ServiceCollection();
        services.AddScoped(_ => CreateDb(dbName));
        services.AddScoped(_ => xmlBuilder.Object);
        services.AddScoped(_ => pdfRenderer.Object);
        services.AddScoped(_ => router.Object);
        services.AddScoped(_ => anafClient.Object);
        services.AddScoped(_ => lifecycle.Object);
        services.AddScoped<IOptions<SellerSettings>>(_ => Options.Create(new SellerSettings()));
        services.AddScoped<IOptions<InvoicingSettings>>(_ => Options.Create(new InvoicingSettings()));
        services.AddLogging();
        services.AddScoped<InvoicePdfReadyNotifier>();

        var sp = services.BuildServiceProvider();

        var job = (InvoiceUploadJob)Activator.CreateInstance(
            typeof(InvoiceUploadJob),
            BindingFlags.Public | BindingFlags.Instance,
            binder: null,
            args: [
                sp.GetRequiredService<IServiceScopeFactory>(),
                Options.Create(new AnafSettings()),
                TimeProvider.System,
                logCapture is null
                    ? sp.GetRequiredService<ILogger<InvoiceUploadJob>>()
                    : logCapture.LoggerFor<InvoiceUploadJob>(),
            ],
            culture: null)!;

        return new Harness
        {
            Job = job, Sp = sp, Router = router, Cloud = cloud, Local = local,
            Lifecycle = lifecycle, AnafClient = anafClient,
        };
    }

    [Fact]
    public async Task UploadPendingAsync_CloudEnabled_SavesPdfToCloudAdapterNotLocal()
    {
        var dbName = $"InvoiceUpload_{Guid.NewGuid():N}";
        var h = Build(dbName, cloudEnabled: true);
        var orderId = Guid.NewGuid();
        var invoiceId = Guid.NewGuid();

        using (var seed = CreateDb(dbName))
        {
            var order = MakeOrder(orderId);
            order.Id = orderId;
            seed.Orders.Add(order);
            var invoice = MakeInvoice(orderId);
            invoice.Id = invoiceId;
            seed.Invoices.Add(invoice);
            await seed.SaveChangesAsync();
        }

        h.AnafClient.Setup(c => c.UploadAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new AnafUploadResult("upload-1", DateTimeOffset.UtcNow));

        await InvokeUploadPendingAsync(h.Job, h.Sp, invoiceId, orderId);

        h.Cloud.Verify(s => s.SaveAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
        h.Local.Verify(s => s.SaveAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UploadPendingAsync_CloudDisabled_SavesPdfToLocalAdapter()
    {
        var dbName = $"InvoiceUpload_{Guid.NewGuid():N}";
        var h = Build(dbName, cloudEnabled: false);
        var orderId = Guid.NewGuid();
        var invoiceId = Guid.NewGuid();

        using (var seed = CreateDb(dbName))
        {
            var order = MakeOrder(orderId);
            order.Id = orderId;
            seed.Orders.Add(order);
            var invoice = MakeInvoice(orderId);
            invoice.Id = invoiceId;
            seed.Invoices.Add(invoice);
            await seed.SaveChangesAsync();
        }

        h.AnafClient.Setup(c => c.UploadAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new AnafUploadResult("upload-1", DateTimeOffset.UtcNow));

        await InvokeUploadPendingAsync(h.Job, h.Sp, invoiceId, orderId);

        h.Local.Verify(s => s.SaveAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UploadPendingAsync_AnafSucceedsButMarkSubmittedFails_LogsDistinctlyAndRethrows()
    {
        var dbName = $"InvoiceUpload_{Guid.NewGuid():N}";
        var logs = new LogCapture();
        var h = Build(dbName, cloudEnabled: false, logs);
        var orderId = Guid.NewGuid();
        var invoiceId = Guid.NewGuid();

        using (var seed = CreateDb(dbName))
        {
            var order = MakeOrder(orderId);
            order.Id = orderId;
            seed.Orders.Add(order);
            var invoice = MakeInvoice(orderId);
            invoice.Id = invoiceId;
            seed.Invoices.Add(invoice);
            await seed.SaveChangesAsync();
        }

        h.AnafClient.Setup(c => c.UploadAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new AnafUploadResult("upload-1", DateTimeOffset.UtcNow));
        h.Lifecycle.Setup(l => l.MarkSubmittedAsync(invoiceId, "upload-1", It.IsAny<CancellationToken>()))
                   .ThrowsAsync(new InvalidOperationException("transient DB failure"));

        var act = () => InvokeUploadPendingAsync(h.Job, h.Sp, invoiceId, orderId);

        await act.Should().ThrowAsync<InvalidOperationException>();
        logs.Records.Should().ContainSingle(
            r => r.Level == LogLevel.Error &&
                 r.Message.StartsWith("anaf.upload-job.submitted-but-not-recorded", StringComparison.Ordinal) &&
                 r.Message.Contains(invoiceId.ToString()) &&
                 r.Message.Contains("upload-1"));
    }
}
