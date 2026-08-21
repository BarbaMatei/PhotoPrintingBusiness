using System.Reflection;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
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
    private static int _seedNumber;

    private static PhotoPrintDbContext CreateDb(PostgresTestDatabase database, Action<string>? sqlLog = null)
    {
        var builder = new DbContextOptionsBuilder<PhotoPrintDbContext>()
            .UseNpgsql(database.ConnectionString);
        if (sqlLog is not null) builder.LogTo(sqlLog, LogLevel.Information);
        return new(builder.Options);
    }

    private static PostgresTestDatabase OpenDatabase() => new();

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

    // IssuedAt must be a real instant: the uq_invoices_series_year_number index extracts a
    // year from it, and EXTRACT on the -infinity that a default DateTimeOffset maps to
    // cannot be cast to int.
    private static Invoice MakeInvoice(Guid orderId, string? xmlPayload = "<Invoice/>") => new()
    {
        OrderId = orderId,
        InvoiceNumber = "FT-2026-00001",
        Series = "FT",
        Number = 1,
        IssuedAt = new DateTimeOffset(2026, 6, 3, 12, 0, 0, TimeSpan.Zero),
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

    private static Task InvokePollSubmittedAsync(InvoiceUploadJob job, IServiceProvider sp, Guid invoiceId)
    {
        var method = typeof(InvoiceUploadJob).GetMethod("PollSubmittedAsync",
            BindingFlags.NonPublic | BindingFlags.Instance)!;
        return (Task)method.Invoke(job, [sp, invoiceId, CancellationToken.None])!;
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
        PostgresTestDatabase database, bool cloudEnabled,
        LogCapture? logCapture = null, int claimTtlMinutes = 10, Action<string>? sqlLog = null,
        TimeProvider? clock = null, int[]? backoffHours = null, bool realLifecycle = false,
        AnafOutageRegistry? outages = null, int pollIntervalMinutes = 30)
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
        services.AddScoped(_ => CreateDb(database, sqlLog));
        services.AddScoped(_ => xmlBuilder.Object);
        services.AddScoped(_ => pdfRenderer.Object);
        services.AddScoped(_ => router.Object);
        services.AddScoped(_ => anafClient.Object);
        // A mocked lifecycle cannot see a CAS that refuses, so tests about persisted state use the real one.
        if (realLifecycle)
            services.AddScoped<IInvoiceLifecycle>(p => new InvoiceLifecycle(
                p.GetRequiredService<PhotoPrintDbContext>(),
                clock ?? TimeProvider.System,
                NullLogger<InvoiceLifecycle>.Instance));
        else
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
                Options.Create(new AnafSettings
                {
                    ClaimTtlMinutes = claimTtlMinutes,
                    PollIntervalMinutes = pollIntervalMinutes,
                    BackoffHours = backoffHours ?? new AnafSettings().BackoffHours,
                }),
                outages ?? new AnafOutageRegistry(new MemoryCache(new MemoryCacheOptions())),
                clock ?? TimeProvider.System,
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
        PostgresTestDatabase database, InvoiceAnafStatus status = InvoiceAnafStatus.Pending,
        DateTimeOffset? claimedAt = null, string? xmlPayload = "<Invoice/>", string? anafUploadId = null,
        DateTimeOffset? createdAt = null)
    {
        var orderId = Guid.NewGuid();
        var invoiceId = Guid.NewGuid();
        using var seed = CreateDb(database);
        var order = MakeOrder(orderId);
        seed.Orders.Add(order);
        var invoice = MakeInvoice(orderId, xmlPayload);
        invoice.Id = invoiceId;
        // Distinct number per seed: uq_invoices_series_year_number rejects a repeat within the year.
        invoice.Number = Interlocked.Increment(ref _seedNumber);
        invoice.InvoiceNumber = $"FT-2026-{invoice.Number:D5}";
        invoice.AnafStatus = status;
        invoice.ClaimedAt = claimedAt;
        invoice.AnafUploadId = anafUploadId;
        if (createdAt is not null) invoice.CreatedAt = createdAt.Value;
        seed.Invoices.Add(invoice);
        seed.SaveChanges();
        return (orderId, invoiceId);
    }

    [Fact]
    public async Task UploadPendingAsync_CloudEnabled_SavesPdfToCloudAdapterNotLocal()
    {
        using var database = OpenDatabase();
        var h = Build(database, cloudEnabled: true);
        var (orderId, invoiceId) = SeedOrderAndInvoice(database);

        h.AnafClient.Setup(c => c.UploadAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new AnafUploadResult("upload-1", DateTimeOffset.UtcNow));

        await InvokeUploadPendingAsync(h.Job, h.Sp, invoiceId, orderId);

        h.Cloud.Verify(s => s.SaveAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
        h.Local.Verify(s => s.SaveAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UploadPendingAsync_CloudDisabled_SavesPdfToLocalAdapter()
    {
        using var database = OpenDatabase();
        var h = Build(database, cloudEnabled: false);
        var (orderId, invoiceId) = SeedOrderAndInvoice(database);

        h.AnafClient.Setup(c => c.UploadAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new AnafUploadResult("upload-1", DateTimeOffset.UtcNow));

        await InvokeUploadPendingAsync(h.Job, h.Sp, invoiceId, orderId);

        h.Local.Verify(s => s.SaveAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UploadPendingAsync_AnafSucceedsButMarkSubmittedFails_LogsDistinctlyAndRethrows()
    {
        using var database = OpenDatabase();
        var logs = new LogCapture();
        var h = Build(database, cloudEnabled: false, logs);
        var (orderId, invoiceId) = SeedOrderAndInvoice(database);

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
        using var database = OpenDatabase();
        var h = Build(database, cloudEnabled: false, claimTtlMinutes: 10);
        var (orderId, invoiceId) = SeedOrderAndInvoice(database, claimedAt: DateTimeOffset.UtcNow.AddMinutes(-2));

        await InvokeUploadPendingAsync(h.Job, h.Sp, invoiceId, orderId);

        h.AnafClient.Verify(c => c.UploadAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UploadPendingAsync_ClaimExpired_ProceedsAndReclaims()
    {
        using var database = OpenDatabase();
        var h = Build(database, cloudEnabled: false, claimTtlMinutes: 10);
        var (orderId, invoiceId) = SeedOrderAndInvoice(database, claimedAt: DateTimeOffset.UtcNow.AddMinutes(-20));

        h.AnafClient.Setup(c => c.UploadAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new AnafUploadResult("upload-1", DateTimeOffset.UtcNow));

        await InvokeUploadPendingAsync(h.Job, h.Sp, invoiceId, orderId);

        h.AnafClient.Verify(c => c.UploadAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UploadPendingAsync_AnafRejectsWithContentErrors_ReleasesClaimForPromptRetry()
    {
        using var database = OpenDatabase();
        var h = Build(database, cloudEnabled: false);
        var (orderId, invoiceId) = SeedOrderAndInvoice(database);

        h.AnafClient.Setup(c => c.UploadAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
                    .ThrowsAsync(new AnafUploadException("bad CIF"));

        await InvokeUploadPendingAsync(h.Job, h.Sp, invoiceId, orderId);

        using var verify = CreateDb(database);
        var claimedAt = await verify.Invoices.Where(i => i.Id == invoiceId).Select(i => i.ClaimedAt).FirstAsync();
        claimedAt.Should().BeNull();
    }

    [Fact]
    public async Task UploadPendingAsync_AnafUnreachable_RecordsErrorAndReleasesClaim()
    {
        using var database = OpenDatabase();
        var h = Build(database, cloudEnabled: false);
        var (orderId, invoiceId) = SeedOrderAndInvoice(database);

        h.AnafClient.Setup(c => c.UploadAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
                    .ThrowsAsync(new AnafUnreachableException("upload", httpStatus: 400));

        await InvokeUploadPendingAsync(h.Job, h.Sp, invoiceId, orderId);

        h.Lifecycle.Verify(l => l.RecordPendingErrorAsync(invoiceId, It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
        using var verify = CreateDb(database);
        var claimedAt = await verify.Invoices.Where(i => i.Id == invoiceId).Select(i => i.ClaimedAt).FirstAsync();
        claimedAt.Should().BeNull();
    }

    [Fact]
    public async Task UploadPendingAsync_XmlBuildThrows_RecordsErrorAndReleasesClaimWithoutCallingAnaf()
    {
        using var database = OpenDatabase();
        var h = Build(database, cloudEnabled: false);
        var (orderId, invoiceId) = SeedOrderAndInvoice(database, xmlPayload: null);
        h.XmlBuilder.Setup(b => b.Build(It.IsAny<Order>(), It.IsAny<Invoice>(), It.IsAny<SellerSettings>()))
                    .Throws(new InvalidOperationException("order has zero items"));

        await InvokeUploadPendingAsync(h.Job, h.Sp, invoiceId, orderId);

        h.AnafClient.Verify(c => c.UploadAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()), Times.Never);
        h.Lifecycle.Verify(l => l.RecordPendingErrorAsync(invoiceId, It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
        using var verify = CreateDb(database);
        var claimedAt = await verify.Invoices.Where(i => i.Id == invoiceId).Select(i => i.ClaimedAt).FirstAsync();
        claimedAt.Should().BeNull();
    }

    [Fact]
    public async Task UploadPendingAsync_XmlAndPdfAlreadyBuilt_SkipsOrderReloadAndProceedsToUpload()
    {
        using var database = OpenDatabase();
        var sqlLines = new List<string>();
        var h = Build(database, cloudEnabled: false, sqlLog: sqlLines.Add);
        var orderId = Guid.NewGuid();
        var invoiceId = Guid.NewGuid();
        using (var seed = CreateDb(database))
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
        using var database = OpenDatabase();
        var logs = new LogCapture();
        var h = Build(database, cloudEnabled: false, logs);
        var (orderId, invoiceId) = SeedOrderAndInvoice(database);

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

    [Fact]
    public async Task PollSubmittedAsync_UnrecognizedStatus_LogsDistinctlyFromInProgressAndDoesNotTransition()
    {
        using var database = OpenDatabase();
        var logs = new LogCapture();
        var h = Build(database, cloudEnabled: false, logs);
        var (_, invoiceId) = SeedOrderAndInvoice(
            database, status: InvoiceAnafStatus.Submitted, anafUploadId: "upload-1");

        h.AnafClient.Setup(c => c.GetStatusAsync("upload-1", It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new AnafStatusResult(AnafExternalStatus.Unknown));

        await InvokePollSubmittedAsync(h.Job, h.Sp, invoiceId);

        logs.Records.Should().ContainSingle(
            r => r.Level == LogLevel.Warning &&
                 r.Message.StartsWith("anaf.upload-job.status-unknown", StringComparison.Ordinal) &&
                 r.Message.Contains(invoiceId.ToString()));
        h.Lifecycle.Verify(l => l.MarkAcceptedAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        h.Lifecycle.Verify(l => l.MarkRejectedAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task PollSubmittedAsync_RejectedWithinBackoffBudget_MarksRejectedNotFailed()
    {
        using var database = OpenDatabase();
        var now = new DateTimeOffset(2026, 6, 3, 12, 0, 0, TimeSpan.Zero);
        var h = Build(database, cloudEnabled: false, clock: new FakeClock(now), backoffHours: [1, 4]);
        var (_, invoiceId) = SeedOrderAndInvoice(
            database, status: InvoiceAnafStatus.Submitted, anafUploadId: "upload-1",
            createdAt: now.AddHours(-2));   // elapsed 2h < budget (1+4=5h)

        h.AnafClient.Setup(c => c.GetStatusAsync("upload-1", It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new AnafStatusResult(AnafExternalStatus.Rejected, "date incorecte"));

        await InvokePollSubmittedAsync(h.Job, h.Sp, invoiceId);

        h.Lifecycle.Verify(l => l.MarkRejectedAsync(invoiceId, "date incorecte", It.IsAny<CancellationToken>()), Times.Once);
        h.Lifecycle.Verify(l => l.MarkFailedAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task PollSubmittedAsync_RejectedBudgetExhausted_MarksFailedNotRejected()
    {
        using var database = OpenDatabase();
        var now = new DateTimeOffset(2026, 6, 3, 12, 0, 0, TimeSpan.Zero);
        var h = Build(database, cloudEnabled: false, clock: new FakeClock(now), backoffHours: [1, 4]);
        var (_, invoiceId) = SeedOrderAndInvoice(
            database, status: InvoiceAnafStatus.Submitted, anafUploadId: "upload-1",
            createdAt: now.AddHours(-6));   // elapsed 6h > budget (1+4=5h)

        h.AnafClient.Setup(c => c.GetStatusAsync("upload-1", It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new AnafStatusResult(AnafExternalStatus.Rejected, "date incorecte"));

        await InvokePollSubmittedAsync(h.Job, h.Sp, invoiceId);

        h.Lifecycle.Verify(l => l.MarkFailedAsync(invoiceId, "date incorecte", It.IsAny<CancellationToken>()), Times.Once);
        h.Lifecycle.Verify(l => l.MarkRejectedAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    private static Task InvokeRunTickAsync(InvoiceUploadJob job, CancellationToken ct)
    {
        var method = typeof(InvoiceUploadJob).GetMethod("RunTickAsync",
            BindingFlags.NonPublic | BindingFlags.Instance)!;
        return (Task)method.Invoke(job, [ct])!;
    }

    // A BackgroundService that throws stops the host, so a timeout anywhere in the tick must not escape.
    [Fact]
    public async Task RunTickAsync_TimeoutSurfacingAsCancellation_DoesNotEscapeAndStopTheHost()
    {
        using var database = new PostgresTestDatabase();
        var logs = new LogCapture();
        var h = Build(database, cloudEnabled: false, logs);
        var (_, _) = SeedOrderAndInvoice(database);

        h.AnafClient.Setup(c => c.UploadAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
                    .ThrowsAsync(new OperationCanceledException("client timeout"));

        var act = () => InvokeRunTickAsync(h.Job, CancellationToken.None);

        await act.Should().NotThrowAsync();
        logs.Records.Should().ContainSingle(
            r => r.Message.StartsWith("anaf.upload-job.tick-cancelled", StringComparison.Ordinal));
    }

    [Fact]
    public async Task RunTickAsync_RealShutdown_StillPropagates()
    {
        using var database = new PostgresTestDatabase();
        var h = Build(database, cloudEnabled: false);
        SeedOrderAndInvoice(database);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var act = () => InvokeRunTickAsync(h.Job, cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>(
            "a genuine shutdown must still stop the worker");
    }

    // The 10-minute claim expires two ticks before the 30-minute cooldown lets the row back in, so it never delayed the re-post; the count is what bounds the blind re-posts.
    [Fact]
    public async Task UploadPendingAsync_UploadTimesOut_CountsTheUnknownOutcomeAndLeavesTheClaimToExpire()
    {
        using var database = new PostgresTestDatabase();
        var logs = new LogCapture();
        var h = Build(database, cloudEnabled: false, logs, realLifecycle: true);
        var (orderId, invoiceId) = SeedOrderAndInvoice(database);

        h.AnafClient.Setup(c => c.UploadAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
                    .ThrowsAsync(new AnafUploadTimeoutException("upload?standard=UBL"));

        await InvokeUploadPendingAsync(h.Job, h.Sp, invoiceId, orderId);

        logs.Records.Should().ContainSingle(
            r => r.Message.StartsWith("anaf.upload-job.upload-outcome-unknown", StringComparison.Ordinal));

        using var verify = database.NewContext();
        var row = await verify.Invoices.FirstAsync(i => i.Id == invoiceId);
        row.UnknownUploadOutcomes.Should().Be(1, "the row has to remember that ANAF may already hold this number");
        row.LastError.Should().NotBeNullOrEmpty();
        row.ClaimedAt.Should().NotBeNull("the claim keeps a second replica out of a row whose ANAF answer nobody has yet");
    }

    // A timeout may already have filed this invoice number, so re-posting it every hour forever is a duplicate-filing machine.
    [Fact]
    public async Task ProcessBatchAsync_UploadKeepsTimingOut_StopsReuploadingWhenTheBlindRepostBudgetIsSpent()
    {
        using var database = new PostgresTestDatabase();
        var clock = new AdvanceableClock(new DateTimeOffset(2026, 6, 3, 12, 0, 0, TimeSpan.Zero));
        var logs = new LogCapture();
        var h = Build(database, cloudEnabled: false, logs, clock: clock, realLifecycle: true);
        var (_, invoiceId) = SeedOrderAndInvoice(database);
        var budget = new AnafSettings().MaxUnknownUploadOutcomes;

        h.AnafClient.Setup(c => c.UploadAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
                    .ThrowsAsync(new AnafUploadTimeoutException("upload?standard=UBL"));

        for (var tick = 0; tick < budget + 2; tick++)
        {
            await InvokeProcessBatchAsync(h.Job);
            clock.Advance(TimeSpan.FromMinutes(60));
        }

        h.AnafClient.Verify(c => c.UploadAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()),
            Times.Exactly(budget),
            "every re-post after an unknown outcome risks a second copy of the same invoice number at ANAF");
        logs.Records.Should().ContainSingle(
            r => r.Level == LogLevel.Error &&
                 r.Message.StartsWith("anaf.upload-job.blind-repost-budget-spent", StringComparison.Ordinal) &&
                 r.Message.Contains(invoiceId.ToString()));
    }

    [Fact]
    public async Task ProcessBatchAsync_BlindRepostBudgetSpent_ParksTheRowWhereTheAdminRetryCanReachIt()
    {
        using var database = new PostgresTestDatabase();
        var clock = new AdvanceableClock(new DateTimeOffset(2026, 6, 3, 12, 0, 0, TimeSpan.Zero));
        var h = Build(database, cloudEnabled: false, clock: clock, realLifecycle: true);
        var (_, invoiceId) = SeedOrderAndInvoice(database);
        var budget = new AnafSettings().MaxUnknownUploadOutcomes;

        h.AnafClient.Setup(c => c.UploadAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
                    .ThrowsAsync(new AnafUploadTimeoutException("upload?standard=UBL"));

        for (var tick = 0; tick < budget; tick++)
        {
            await InvokeProcessBatchAsync(h.Job);
            clock.Advance(TimeSpan.FromMinutes(60));
        }

        using var verify = database.NewContext();
        var row = await verify.Invoices.FirstAsync(i => i.Id == invoiceId);
        row.AnafStatus.Should().Be(InvoiceAnafStatus.Failed,
            "Rejected and Failed are the only states POST /api/admin/invoices/{id}/retry accepts, so this is the operator's way back in");
        row.LastError.Should().Contain("SPV", "the operator has to know to reconcile the number before retrying");
        row.ClaimedAt.Should().BeNull("a parked row is nobody's to hold");
    }

    [Fact]
    public async Task ProcessBatchAsync_OneInvoiceSpendingItsBudget_LeavesAnothersUntouched()
    {
        using var database = new PostgresTestDatabase();
        var clock = new AdvanceableClock(new DateTimeOffset(2026, 6, 3, 12, 0, 0, TimeSpan.Zero));
        var h = Build(database, cloudEnabled: false, clock: clock, realLifecycle: true);
        var (_, doomedId) = SeedOrderAndInvoice(database);
        var (_, healthyId) = SeedOrderAndInvoice(database, xmlPayload: "<InvoiceB/>");
        var budget = new AnafSettings().MaxUnknownUploadOutcomes;
        var healthyCalls = 0;

        h.AnafClient.Setup(c => c.UploadAsync(It.Is<byte[]>(b => IsHealthy(b)), It.IsAny<CancellationToken>()))
                    .Returns(() => ++healthyCalls == 1
                        ? Task.FromException<AnafUploadResult>(new AnafUploadTimeoutException("upload?standard=UBL"))
                        : Task.FromResult(new AnafUploadResult("upload-b", clock.GetUtcNow())));
        h.AnafClient.Setup(c => c.UploadAsync(It.Is<byte[]>(b => !IsHealthy(b)), It.IsAny<CancellationToken>()))
                    .ThrowsAsync(new AnafUploadTimeoutException("upload?standard=UBL"));

        for (var tick = 0; tick < budget; tick++)
        {
            await InvokeProcessBatchAsync(h.Job);
            clock.Advance(TimeSpan.FromMinutes(60));
        }

        using var verify = database.NewContext();
        var doomed = await verify.Invoices.FirstAsync(i => i.Id == doomedId);
        var healthy = await verify.Invoices.FirstAsync(i => i.Id == healthyId);

        doomed.AnafStatus.Should().Be(InvoiceAnafStatus.Failed);
        healthy.AnafStatus.Should().Be(InvoiceAnafStatus.Submitted,
            "the budget is per invoice — one row spending its own must not park a neighbour");
        healthy.UnknownUploadOutcomes.Should().Be(1);
    }

    private static bool IsHealthy(byte[] xml) =>
        System.Text.Encoding.UTF8.GetString(xml).Contains("InvoiceB", StringComparison.Ordinal);

    // One dead credential is one incident, not one per row in the batch.
    [Fact]
    public async Task ProcessBatchAsync_AuthFailsForEveryRow_LogsOnceAndSummarisesTheRest()
    {
        using var database = new PostgresTestDatabase();
        var logs = new LogCapture();
        var h = Build(database, cloudEnabled: false, logs);
        for (var i = 0; i < 4; i++)
            SeedOrderAndInvoice(database, status: InvoiceAnafStatus.Submitted, anafUploadId: $"upload-{i}");

        h.AnafClient.Setup(c => c.GetStatusAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                    .ThrowsAsync(new AnafAuthException("stareMesaj"));

        await InvokeProcessBatchAsync(h.Job);

        logs.Records.Count(r => r.Message.StartsWith("anaf.upload-job.auth-failed", StringComparison.Ordinal))
            .Should().Be(1, "four rows failing on one credential is one incident");
        h.Hub.Verify(hub => hub.CaptureEvent(
            It.IsAny<Sentry.SentryEvent>(), It.IsAny<Sentry.Scope>(), It.IsAny<Sentry.SentryHint>()), Times.Once);
        logs.Records.Should().ContainSingle(
            r => r.Message.StartsWith("anaf.upload-job.auth-failure-skipped", StringComparison.Ordinal));
    }

    // A credential outage lasts days, so paging once per poll interval re-reports it ~48 times a day.
    [Fact]
    public async Task ProcessBatchAsync_AuthStillFailingOnTheNextTick_DoesNotPageASecondTime()
    {
        using var database = new PostgresTestDatabase();
        var logs = new LogCapture();
        var h = Build(database, cloudEnabled: false, logs);
        var (_, invoiceId) = SeedOrderAndInvoice(
            database, status: InvoiceAnafStatus.Submitted, anafUploadId: "upload-1");

        h.AnafClient.Setup(c => c.GetStatusAsync("upload-1", It.IsAny<CancellationToken>()))
                    .ThrowsAsync(new AnafAuthException("stareMesaj"));

        await InvokeProcessBatchAsync(h.Job);
        await InvokeProcessBatchAsync(h.Job);

        logs.Records.Count(r => r.Level == LogLevel.Error &&
                                r.Message.StartsWith("anaf.upload-job.auth-failed", StringComparison.Ordinal))
            .Should().Be(1, "one outage is one page, however many ticks it spans");
        h.Hub.Verify(hub => hub.CaptureEvent(
            It.IsAny<Sentry.SentryEvent>(), It.IsAny<Sentry.Scope>(), It.IsAny<Sentry.SentryHint>()), Times.Once);
        logs.Records.Should().ContainSingle(
            r => r.Level == LogLevel.Warning &&
                 r.Message.StartsWith("anaf.upload-job.auth-outage-continues", StringComparison.Ordinal),
            "with the page suppressed this is the only per-tick trace that the outage is still live");
        h.Lifecycle.Verify(l => l.RecordErrorAsync(invoiceId, It.IsAny<string>(), It.IsAny<InvoiceAnafStatus>(), It.IsAny<CancellationToken>()),
            Times.Exactly(2));
    }

    // The window is a heartbeat, not a mute: a credential nobody has fixed must page again.
    [Fact]
    public async Task ProcessBatchAsync_AuthStillFailingAfterTheAlertWindowExpires_PagesAgain()
    {
        using var database = new PostgresTestDatabase();
        var logs = new LogCapture();
        var cacheClock = new FakeSystemClock(new DateTimeOffset(2026, 6, 3, 12, 0, 0, TimeSpan.Zero));
        using var cache = new MemoryCache(new MemoryCacheOptions { Clock = cacheClock });
        var h = Build(database, cloudEnabled: false, logs, outages: new AnafOutageRegistry(cache));
        SeedOrderAndInvoice(database, status: InvoiceAnafStatus.Submitted, anafUploadId: "upload-1");

        h.AnafClient.Setup(c => c.GetStatusAsync("upload-1", It.IsAny<CancellationToken>()))
                    .ThrowsAsync(new AnafAuthException("stareMesaj"));

        await InvokeProcessBatchAsync(h.Job);
        cacheClock.UtcNow = cacheClock.UtcNow.AddHours(2).AddMinutes(1);
        await InvokeProcessBatchAsync(h.Job);

        logs.Records.Count(r => r.Level == LogLevel.Error &&
                                r.Message.StartsWith("anaf.upload-job.auth-failed", StringComparison.Ordinal))
            .Should().Be(2, "an outage that outlives the alert window has to be re-reported");
    }

    // A window shorter than a tick puts every tick outside the previous one, which is the storm it exists to stop.
    [Fact]
    public async Task ProcessBatchAsync_AuthStillFailingOnTheNextTickOfASlowPoll_DoesNotPageASecondTime()
    {
        using var database = new PostgresTestDatabase();
        var logs = new LogCapture();
        var cacheClock = new FakeSystemClock(new DateTimeOffset(2026, 6, 3, 12, 0, 0, TimeSpan.Zero));
        using var cache = new MemoryCache(new MemoryCacheOptions { Clock = cacheClock });
        var h = Build(database, cloudEnabled: false, logs,
                      outages: new AnafOutageRegistry(cache), pollIntervalMinutes: 180);
        SeedOrderAndInvoice(database, status: InvoiceAnafStatus.Submitted, anafUploadId: "upload-1");

        h.AnafClient.Setup(c => c.GetStatusAsync("upload-1", It.IsAny<CancellationToken>()))
                    .ThrowsAsync(new AnafAuthException("stareMesaj"));

        await InvokeProcessBatchAsync(h.Job);
        // Two intervals of cache time: the widest gap the window must span, since the per-row cooldown delays a failed row by a tick.
        cacheClock.UtcNow = cacheClock.UtcNow.AddMinutes(360);
        await InvokeProcessBatchAsync(h.Job);

        logs.CountStartingWith("anaf.upload-job.auth-failed", LogLevel.Error)
            .Should().Be(1, "the window has to outlast the poll interval an operator configured");
        logs.Records.Should().ContainSingle(
            r => r.Level == LogLevel.Warning &&
                 r.Message.StartsWith("anaf.upload-job.auth-outage-continues", StringComparison.Ordinal) &&
                 r.Message.Contains("alert_window_minutes=720 interval_minutes=180", StringComparison.Ordinal),
            "the operator can only see the window outlasts a tick if both numbers are on the line");
    }

    // At the slowest legal interval the window must still expire inside the 5-business-day submission deadline.
    [Fact]
    public async Task ProcessBatchAsync_AuthOutageAtTheSlowestLegalPoll_PagesAgainInsideTheSubmissionDeadline()
    {
        using var database = new PostgresTestDatabase();
        var logs = new LogCapture();
        var cacheClock = new FakeSystemClock(new DateTimeOffset(2026, 6, 3, 12, 0, 0, TimeSpan.Zero));
        using var cache = new MemoryCache(new MemoryCacheOptions { Clock = cacheClock });
        var h = Build(database, cloudEnabled: false, logs,
                      outages: new AnafOutageRegistry(cache), pollIntervalMinutes: 1440);
        SeedOrderAndInvoice(database, status: InvoiceAnafStatus.Submitted, anafUploadId: "upload-1");

        h.AnafClient.Setup(c => c.GetStatusAsync("upload-1", It.IsAny<CancellationToken>()))
                    .ThrowsAsync(new AnafAuthException("stareMesaj"));

        await InvokeProcessBatchAsync(h.Job);
        cacheClock.UtcNow = cacheClock.UtcNow.AddHours(48);
        await InvokeProcessBatchAsync(h.Job);

        logs.CountStartingWith("anaf.upload-job.auth-failed", LogLevel.Error)
            .Should().Be(1, "two daily ticks into one outage is still one incident");

        cacheClock.UtcNow = cacheClock.UtcNow.AddHours(48).AddMinutes(1);
        await InvokeProcessBatchAsync(h.Job);

        logs.CountStartingWith("anaf.upload-job.auth-failed", LogLevel.Error)
            .Should().Be(2, "96 h is inside the 5 business days ANAF allows, so the outage re-pages in time");
    }

    // The floor stops a fast cadence shrinking the window to minutes, and keeps it positive — MemoryCache rejects a non-positive expiry.
    [Fact]
    public async Task ProcessBatchAsync_AuthOutageAtTheFastestLegalPoll_KeepsTheTwoHourFloor()
    {
        using var database = new PostgresTestDatabase();
        var logs = new LogCapture();
        var cacheClock = new FakeSystemClock(new DateTimeOffset(2026, 6, 3, 12, 0, 0, TimeSpan.Zero));
        using var cache = new MemoryCache(new MemoryCacheOptions { Clock = cacheClock });
        var h = Build(database, cloudEnabled: false, logs,
                      outages: new AnafOutageRegistry(cache), pollIntervalMinutes: 1);
        SeedOrderAndInvoice(database, status: InvoiceAnafStatus.Submitted, anafUploadId: "upload-1");

        h.AnafClient.Setup(c => c.GetStatusAsync("upload-1", It.IsAny<CancellationToken>()))
                    .ThrowsAsync(new AnafAuthException("stareMesaj"));

        await InvokeProcessBatchAsync(h.Job);
        cacheClock.UtcNow = cacheClock.UtcNow.AddMinutes(119);
        await InvokeProcessBatchAsync(h.Job);

        logs.CountStartingWith("anaf.upload-job.auth-failed", LogLevel.Error)
            .Should().Be(1, "four one-minute intervals is not an alert window");

        cacheClock.UtcNow = cacheClock.UtcNow.AddMinutes(2);
        await InvokeProcessBatchAsync(h.Job);

        logs.CountStartingWith("anaf.upload-job.auth-failed", LogLevel.Error)
            .Should().Be(2, "past the floor it is still a heartbeat, not a mute");
    }

    [Fact]
    public async Task ProcessBatchAsync_AuthFailsOnASubmittedRow_RecordsTheReasonOnThatRow()
    {
        using var database = new PostgresTestDatabase();
        var h = Build(database, cloudEnabled: false, realLifecycle: true);
        var (_, invoiceId) = SeedOrderAndInvoice(
            database, status: InvoiceAnafStatus.Submitted, anafUploadId: "upload-1");

        h.AnafClient.Setup(c => c.GetStatusAsync("upload-1", It.IsAny<CancellationToken>()))
                    .ThrowsAsync(new AnafAuthException("stareMesaj"));

        await InvokeProcessBatchAsync(h.Job);

        using var verify = database.NewContext();
        var lastError = await verify.Invoices.Where(i => i.Id == invoiceId).Select(i => i.LastError).FirstAsync();
        lastError.Should().NotBeNullOrEmpty(
            "a Submitted row is the dominant auth-failure case, and the admin list shows LastError");
    }

    // Without a cooldown the oldest broken invoice heads every batch forever, starving healthy rows.
    [Fact]
    public async Task ProcessBatchAsync_RowThatJustFailed_IsSkippedUntilItsCooldownExpires()
    {
        using var database = new PostgresTestDatabase();
        var now = new DateTimeOffset(2026, 6, 3, 12, 0, 0, TimeSpan.Zero);
        var h = Build(database, cloudEnabled: false, clock: new FakeClock(now));

        var (_, justFailedId) = SeedOrderAndInvoice(database, createdAt: now.AddDays(-9));
        var (_, healthyId) = SeedOrderAndInvoice(database, createdAt: now.AddMinutes(-1));

        using (var seed = database.NewContext())
        {
            var row = await seed.Invoices.FirstAsync(i => i.Id == justFailedId);
            row.LastError = "order has zero items";
            row.UpdatedAt = now.AddSeconds(-30);   // well inside the 30-minute poll cadence
            await seed.SaveChangesAsync();
        }

        h.AnafClient.Setup(c => c.UploadAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new AnafUploadResult("upload-1", now));

        await InvokeProcessBatchAsync(h.Job);

        using var verify = database.NewContext();
        var skipped = await verify.Invoices.FirstAsync(i => i.Id == justFailedId);
        var worked = await verify.Invoices.FirstAsync(i => i.Id == healthyId);

        skipped.ClaimedAt.Should().BeNull("a row inside its cooldown must not be claimed again");
        worked.ClaimedAt.Should().NotBeNull("the healthy row must still get its turn");
    }

    private sealed class FakeClock : TimeProvider
    {
        private readonly DateTimeOffset _now;
        public FakeClock(DateTimeOffset now) { _now = now; }
        public override DateTimeOffset GetUtcNow() => _now;
    }

    private sealed class AdvanceableClock : TimeProvider
    {
        private DateTimeOffset _now;
        public AdvanceableClock(DateTimeOffset now) { _now = now; }
        public override DateTimeOffset GetUtcNow() => _now;
        public void Advance(TimeSpan delta) => _now = _now.Add(delta);
    }
}
