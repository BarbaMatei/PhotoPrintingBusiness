using System.Reflection;
using FluentAssertions;
using Microsoft.Data.Sqlite;
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
    private static PhotoPrintDbContext CreateDb(SqliteConnection connection, Action<string>? sqlLog = null)
    {
        var builder = new DbContextOptionsBuilder<PhotoPrintDbContext>().UseSqlite(connection);
        if (sqlLog is not null) builder.LogTo(sqlLog, LogLevel.Information);
        return new(builder.Options);
    }

    private static SqliteConnection OpenConnection()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        using var db = CreateDb(connection);
        db.Database.EnsureCreated();
        return connection;
    }

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

    private static Task InvokeProcessBatchAsync(InvoiceUploadJob job)
    {
        var method = typeof(InvoiceUploadJob).GetMethod("ProcessBatchAsync",
            BindingFlags.NonPublic | BindingFlags.Instance)!;
        return (Task)method.Invoke(job, [CancellationToken.None])!;
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
        public required Mock<IInvoiceXmlBuilder> XmlBuilder { get; init; }
        public required Mock<Sentry.IHub> Hub { get; init; }
    }

    private static Harness Build(
        SqliteConnection connection, bool cloudEnabled,
        LogCapture? logCapture = null, int claimTtlMinutes = 10, Action<string>? sqlLog = null)
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

        var hub = new Mock<Sentry.IHub>();
        hub.SetupGet(h => h.IsEnabled).Returns(true);

        var services = new ServiceCollection();
        services.AddScoped(_ => CreateDb(connection, sqlLog));
        services.AddScoped(_ => xmlBuilder.Object);
        services.AddScoped(_ => pdfRenderer.Object);
        services.AddScoped(_ => router.Object);
        services.AddScoped(_ => anafClient.Object);
        services.AddScoped(_ => lifecycle.Object);
        services.AddScoped(_ => hub.Object);
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
                Options.Create(new AnafSettings { ClaimTtlMinutes = claimTtlMinutes }),
                TimeProvider.System,
                logCapture is null
                    ? sp.GetRequiredService<ILogger<InvoiceUploadJob>>()
                    : logCapture.LoggerFor<InvoiceUploadJob>(),
            ],
            culture: null)!;

        return new Harness
        {
            Job = job, Sp = sp, Router = router, Cloud = cloud, Local = local,
            Lifecycle = lifecycle, AnafClient = anafClient, XmlBuilder = xmlBuilder, Hub = hub,
        };
    }

    private static (Guid orderId, Guid invoiceId) SeedOrderAndInvoice(
        SqliteConnection connection, InvoiceAnafStatus status = InvoiceAnafStatus.Pending,
        DateTimeOffset? claimedAt = null, string? xmlPayload = "<Invoice/>")
    {
        var orderId = Guid.NewGuid();
        var invoiceId = Guid.NewGuid();
        using var seed = CreateDb(connection);
        var order = MakeOrder(orderId);
        seed.Orders.Add(order);
        var invoice = MakeInvoice(orderId, xmlPayload);
        invoice.Id = invoiceId;
        invoice.AnafStatus = status;
        invoice.ClaimedAt = claimedAt;
        seed.Invoices.Add(invoice);
        seed.SaveChanges();
        return (orderId, invoiceId);
    }

    [Fact]
    public async Task UploadPendingAsync_CloudEnabled_SavesPdfToCloudAdapterNotLocal()
    {
        using var connection = OpenConnection();
        var h = Build(connection, cloudEnabled: true);
        var (orderId, invoiceId) = SeedOrderAndInvoice(connection);

        h.AnafClient.Setup(c => c.UploadAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new AnafUploadResult("upload-1", DateTimeOffset.UtcNow));

        await InvokeUploadPendingAsync(h.Job, h.Sp, invoiceId, orderId);

        h.Cloud.Verify(s => s.SaveAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
        h.Local.Verify(s => s.SaveAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UploadPendingAsync_CloudDisabled_SavesPdfToLocalAdapter()
    {
        using var connection = OpenConnection();
        var h = Build(connection, cloudEnabled: false);
        var (orderId, invoiceId) = SeedOrderAndInvoice(connection);

        h.AnafClient.Setup(c => c.UploadAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new AnafUploadResult("upload-1", DateTimeOffset.UtcNow));

        await InvokeUploadPendingAsync(h.Job, h.Sp, invoiceId, orderId);

        h.Local.Verify(s => s.SaveAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UploadPendingAsync_AnafSucceedsButMarkSubmittedFails_LogsDistinctlyAndRethrows()
    {
        using var connection = OpenConnection();
        var logs = new LogCapture();
        var h = Build(connection, cloudEnabled: false, logs);
        var (orderId, invoiceId) = SeedOrderAndInvoice(connection);

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

    [Fact]
    public async Task UploadPendingAsync_RowAlreadyClaimedWithinTtl_SkipsWithoutCallingAnaf()
    {
        using var connection = OpenConnection();
        var h = Build(connection, cloudEnabled: false, claimTtlMinutes: 10);
        var (orderId, invoiceId) = SeedOrderAndInvoice(connection, claimedAt: DateTimeOffset.UtcNow.AddMinutes(-2));

        await InvokeUploadPendingAsync(h.Job, h.Sp, invoiceId, orderId);

        h.AnafClient.Verify(c => c.UploadAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UploadPendingAsync_ClaimExpired_ProceedsAndReclaims()
    {
        using var connection = OpenConnection();
        var h = Build(connection, cloudEnabled: false, claimTtlMinutes: 10);
        var (orderId, invoiceId) = SeedOrderAndInvoice(connection, claimedAt: DateTimeOffset.UtcNow.AddMinutes(-20));

        h.AnafClient.Setup(c => c.UploadAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new AnafUploadResult("upload-1", DateTimeOffset.UtcNow));

        await InvokeUploadPendingAsync(h.Job, h.Sp, invoiceId, orderId);

        h.AnafClient.Verify(c => c.UploadAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UploadPendingAsync_AnafRejectsWithContentErrors_ReleasesClaimForPromptRetry()
    {
        using var connection = OpenConnection();
        var h = Build(connection, cloudEnabled: false);
        var (orderId, invoiceId) = SeedOrderAndInvoice(connection);

        h.AnafClient.Setup(c => c.UploadAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
                    .ThrowsAsync(new AnafUploadException("bad CIF"));

        await InvokeUploadPendingAsync(h.Job, h.Sp, invoiceId, orderId);

        using var verify = CreateDb(connection);
        var claimedAt = await verify.Invoices.Where(i => i.Id == invoiceId).Select(i => i.ClaimedAt).FirstAsync();
        claimedAt.Should().BeNull();
    }

    [Fact]
    public async Task UploadPendingAsync_AnafUnreachable_RecordsErrorAndReleasesClaim()
    {
        using var connection = OpenConnection();
        var h = Build(connection, cloudEnabled: false);
        var (orderId, invoiceId) = SeedOrderAndInvoice(connection);

        h.AnafClient.Setup(c => c.UploadAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
                    .ThrowsAsync(new AnafUnreachableException("upload", httpStatus: 400));

        await InvokeUploadPendingAsync(h.Job, h.Sp, invoiceId, orderId);

        h.Lifecycle.Verify(l => l.RecordPendingErrorAsync(invoiceId, It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
        using var verify = CreateDb(connection);
        var claimedAt = await verify.Invoices.Where(i => i.Id == invoiceId).Select(i => i.ClaimedAt).FirstAsync();
        claimedAt.Should().BeNull();
    }

    [Fact]
    public async Task UploadPendingAsync_XmlBuildThrows_RecordsErrorAndReleasesClaimWithoutCallingAnaf()
    {
        using var connection = OpenConnection();
        var h = Build(connection, cloudEnabled: false);
        var (orderId, invoiceId) = SeedOrderAndInvoice(connection, xmlPayload: null);
        h.XmlBuilder.Setup(b => b.Build(It.IsAny<Order>(), It.IsAny<Invoice>(), It.IsAny<SellerSettings>()))
                    .Throws(new InvalidOperationException("order has zero items"));

        await InvokeUploadPendingAsync(h.Job, h.Sp, invoiceId, orderId);

        h.AnafClient.Verify(c => c.UploadAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()), Times.Never);
        h.Lifecycle.Verify(l => l.RecordPendingErrorAsync(invoiceId, It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
        using var verify = CreateDb(connection);
        var claimedAt = await verify.Invoices.Where(i => i.Id == invoiceId).Select(i => i.ClaimedAt).FirstAsync();
        claimedAt.Should().BeNull();
    }

    [Fact]
    public async Task UploadPendingAsync_XmlAndPdfAlreadyBuilt_SkipsOrderReloadAndProceedsToUpload()
    {
        using var connection = OpenConnection();
        var sqlLines = new List<string>();
        var h = Build(connection, cloudEnabled: false, sqlLog: sqlLines.Add);
        var orderId = Guid.NewGuid();
        var invoiceId = Guid.NewGuid();
        using (var seed = CreateDb(connection))
        {
            seed.Orders.Add(MakeOrder(orderId));
            var invoice = MakeInvoice(orderId, xmlPayload: "<Invoice/>");
            invoice.Id = invoiceId;
            invoice.PdfStoragePath = "invoices/2026/existing.pdf";
            seed.Invoices.Add(invoice);
            seed.SaveChanges();
        }

        h.AnafClient.Setup(c => c.UploadAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new AnafUploadResult("upload-1", DateTimeOffset.UtcNow));

        await InvokeUploadPendingAsync(h.Job, h.Sp, invoiceId, orderId);

        h.AnafClient.Verify(c => c.UploadAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()), Times.Once);
        sqlLines.Should().NotContain(l => l.Contains("FROM \"Orders\""));
    }

    [Fact]
    public async Task ProcessBatchAsync_AnafAuthFails_LogsDistinctlyAndCapturesToSentry()
    {
        using var connection = OpenConnection();
        var logs = new LogCapture();
        var h = Build(connection, cloudEnabled: false, logs);
        var (orderId, invoiceId) = SeedOrderAndInvoice(connection);

        h.AnafClient.Setup(c => c.UploadAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
                    .ThrowsAsync(new AnafAuthException("anaf-upload"));

        await InvokeProcessBatchAsync(h.Job);

        logs.Records.Should().ContainSingle(
            r => r.Level == LogLevel.Error &&
                 r.Message.StartsWith("anaf.upload-job.auth-failed", StringComparison.Ordinal) &&
                 r.Message.Contains(invoiceId.ToString()));
        logs.Records.Should().NotContain(r => r.Message.StartsWith("anaf.upload-job.row-failed", StringComparison.Ordinal));
        h.Hub.Verify(hub => hub.CaptureEvent(
            It.Is<Sentry.SentryEvent>(e => e.Exception is AnafAuthException),
            It.IsAny<Sentry.Scope>(), It.IsAny<Sentry.SentryHint>()), Times.Once);
    }
}
