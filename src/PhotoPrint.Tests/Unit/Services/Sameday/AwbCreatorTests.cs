using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using PhotoPrint.API.Configuration;
using PhotoPrint.API.Data;
using PhotoPrint.API.Exceptions;
using PhotoPrint.API.Models;
using PhotoPrint.API.Observability;
using PhotoPrint.API.Services.Sameday;
using PhotoPrint.Tests.Helpers;

namespace PhotoPrint.Tests.Unit.Services.Sameday;

/// <summary>
/// Full outcome-matrix for <see cref="AwbCreator"/>. Uses a real PostgreSQL database (not EF
/// InMemory provider) because the guarded persist runs <c>ExecuteUpdateAsync</c>,
/// which InMemory doesn't support; the read-backs go through a FRESH context so
/// a missing/last-writer-wins persist reddens the test.
/// </summary>
public class AwbCreatorTests : IClassFixture<ForeignKeyFreeTestDatabase>
{
    private PostgresTestDatabase _database;

    public AwbCreatorTests(ForeignKeyFreeTestDatabase database)
    {
        _database = database;
        database.ResetForTest();
    }

    private PostgresTestDatabase UseIsolatedDatabase()
    {
        var own = PostgresTestDatabase.Throwaway();
        own.DropAllForeignKeys();
        _database = own;

        return own;
    }

    private PhotoPrintDbContext CreateDb() => _database.NewContext();

    private static readonly DateTimeOffset Now = new(2026, 6, 2, 12, 0, 0, TimeSpan.Zero);

    private static SamedaySettings Settings() => new()
    {
        Enabled = true,
        PickupPointId = "PP1",
    };

    private static AwbCreator Build(PhotoPrintDbContext db, Mock<ISamedayClient> client)
    {
        var clock = new FakeTimeProvider(Now);
        return new AwbCreator(
            db, client.Object, Options.Create(Settings()), clock,
            new LoggerFactory().CreateLogger<AwbCreator>());
    }

    private Order SeedOrder(
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
            PaidAt = Now,
            ShippingAddress = new ShippingAddressSnapshot
            {
                RecipientName = "Alice Pop", Phone = "+40712345678",
                Street = "Str. Test", Number = "10",
                City = "Cluj-Napoca", County = "Cluj", PostalCode = "400000",
            },
        };
        using var db = CreateDb();
        db.Orders.Add(order);
        if (withItems)
        {
            db.OrderItems.Add(new OrderItem
            {
                Id = Guid.NewGuid(),
                OrderId = order.Id,
                UploadId = Guid.NewGuid(),
                ProductId = Guid.NewGuid(),
                Quantity = 3, UnitPriceRon = 1, LineTotalRon = 3,
                ProductSnapshot = new ProductSnapshot { ProductName = "x", Size = "x", Finish = "x" },
            });
        }
        db.SaveChanges();
        return order;
    }

    private Order ReadBack(Guid id)
    {
        using var db = CreateDb();
        return db.Orders.AsNoTracking().Single(o => o.Id == id);
    }

    private static Mock<ISamedayClient> ClientReturning(string awb = "RO12345678", string url = "https://sameday/labels/abc.pdf")
    {
        var client = new Mock<ISamedayClient>();
        client.Setup(c => c.CreateAwbAsync(It.IsAny<AwbCreationRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AwbCreationResult(awb, url, 18.50m));
        return client;
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
        var order = SeedOrder(status: OrderStatus.Cancelled);
        using var db = CreateDb();
        var client = new Mock<ISamedayClient>(MockBehavior.Strict);
        var sut = Build(db, client);

        var outcome = await sut.CreateForOrderAsync(order.Id, attempt: 1);

        outcome.Should().BeOfType<AwbCreationOutcome.Skipped>();
        client.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Returns_Skipped_when_AwbNumber_already_populated()
    {
        // Load-bearing pre-check. Removing the IsNullOrWhiteSpace guard breaks this.
        var order = SeedOrder(awbNumber: "RO12345678");
        using var db = CreateDb();
        var client = new Mock<ISamedayClient>(MockBehavior.Strict);
        var sut = Build(db, client);

        var outcome = await sut.CreateForOrderAsync(order.Id, attempt: 1);

        outcome.Should().BeOfType<AwbCreationOutcome.Skipped>()
            .Which.Reason.Should().Contain("AwbNumber");
        client.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Persists_AwbNumber_LabelUrl_and_UpdatedAt_read_through_a_fresh_context()
    {
        var order = SeedOrder();
        using var db = CreateDb();
        var sut = Build(db, ClientReturning());

        var outcome = await sut.CreateForOrderAsync(order.Id, attempt: 1);

        var created = outcome.Should().BeOfType<AwbCreationOutcome.Created>().Subject;
        created.AwbNumber.Should().Be("RO12345678");

        // Fresh context — proves the value hit the store, not just the tracked entity.
        var refreshed = ReadBack(order.Id);
        refreshed.AwbNumber.Should().Be("RO12345678");
        refreshed.AwbLabelUrl.Should().Be("https://sameday/labels/abc.pdf");
        refreshed.UpdatedAt.Should().Be(Now);
    }

    [Fact]
    public async Task Does_not_write_the_AWB_onto_an_order_cancelled_during_the_Sameday_call()
    {
        var order = SeedOrder();

        // The vendor call "takes time"; an admin cancels the order in that window.
        var client = new Mock<ISamedayClient>();
        client.Setup(c => c.CreateAwbAsync(It.IsAny<AwbCreationRequest>(), It.IsAny<CancellationToken>()))
            .Returns<AwbCreationRequest, CancellationToken>((_, _) =>
            {
                SetStatus(order.Id, OrderStatus.Cancelled);
                return Task.FromResult(new AwbCreationResult("RO-ORPHAN", "https://x/y.pdf", 1m));
            });

        using var db = CreateDb();
        var sut = Build(db, client);

        var outcome = await sut.CreateForOrderAsync(order.Id, attempt: 1);

        outcome.Should().BeOfType<AwbCreationOutcome.Skipped>();
        var refreshed = ReadBack(order.Id);
        refreshed.Status.Should().Be(OrderStatus.Cancelled);
        refreshed.AwbNumber.Should().BeNull(); // the guard refused the write
    }

    [Fact]
    public async Task Skips_as_converged_when_the_AWB_was_set_to_the_same_number_during_the_call()
    {
        var order = SeedOrder();

        // A concurrent creator (vendor deduped on the order reference) already wrote
        // the SAME awb number while our Sameday call was in flight.
        var client = new Mock<ISamedayClient>();
        client.Setup(c => c.CreateAwbAsync(It.IsAny<AwbCreationRequest>(), It.IsAny<CancellationToken>()))
            .Returns<AwbCreationRequest, CancellationToken>((_, _) =>
            {
                SetAwb(order.Id, "RO-SAME");
                return Task.FromResult(new AwbCreationResult("RO-SAME", "https://x/y.pdf", 1m));
            });

        using var db = CreateDb();
        var sut = Build(db, client);

        var outcome = await sut.CreateForOrderAsync(order.Id, attempt: 1);

        outcome.Should().BeOfType<AwbCreationOutcome.Skipped>()
            .Which.Reason.Should().Contain("converged");
        ReadBack(order.Id).AwbNumber.Should().Be("RO-SAME"); // single value, no clobber
    }

    [Fact]
    public async Task Returns_transient_RetryLater_when_the_persist_fails_after_a_successful_create()
    {
        using var isolated = UseIsolatedDatabase();
        var order = SeedOrder();

        // The vendor AWB is created, then the DB write fails (table gone mid-call).
        var client = new Mock<ISamedayClient>();
        client.Setup(c => c.CreateAwbAsync(It.IsAny<AwbCreationRequest>(), It.IsAny<CancellationToken>()))
            .Returns<AwbCreationRequest, CancellationToken>((_, _) =>
            {
                _database.BreakOrdersTable();
                return Task.FromResult(new AwbCreationResult("RO-LOST", "https://x/y.pdf", 1m));
            });

        using var db = CreateDb();
        var sut = Build(db, client);

        var outcome = await sut.CreateForOrderAsync(order.Id, attempt: 1);

        outcome.Should().BeOfType<AwbCreationOutcome.RetryLater>()
            .Which.IsTransient.Should().BeTrue();
    }

    [Fact]
    public async Task Second_creator_skips_without_calling_the_vendor_when_a_fresh_claim_is_held()
    {
        // a concurrent creator (retry re-enqueue / second replica) must back off before
        // the billable vendor call when another worker holds a fresh claim.
        var order = SeedOrder();
        SetClaim(order.Id, Now); // another worker just claimed it

        using var db = CreateDb();
        var client = new Mock<ISamedayClient>(MockBehavior.Strict); // any vendor call fails the test
        var sut = Build(db, client);

        var outcome = await sut.CreateForOrderAsync(order.Id, attempt: 1);

        outcome.Should().BeOfType<AwbCreationOutcome.Skipped>()
            .Which.Reason.Should().Contain("claim");
        client.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Persists_the_AWB_when_the_order_advances_to_Printing_during_the_call()
    {
        // the persist guard is `!= Cancelled`, not `== Paid` — an admin advancing the
        // order Paid→Printing mid-call must still keep its label (else it's lost, since the
        // retry sweep only re-picks Paid).
        var order = SeedOrder();

        var client = new Mock<ISamedayClient>();
        client.Setup(c => c.CreateAwbAsync(It.IsAny<AwbCreationRequest>(), It.IsAny<CancellationToken>()))
            .Returns<AwbCreationRequest, CancellationToken>((_, _) =>
            {
                SetStatus(order.Id, OrderStatus.Printing);
                return Task.FromResult(new AwbCreationResult("RO-PRINT", "https://x/y.pdf", 1m));
            });

        using var db = CreateDb();
        var sut = Build(db, client);

        var outcome = await sut.CreateForOrderAsync(order.Id, attempt: 1);

        outcome.Should().BeOfType<AwbCreationOutcome.Created>();
        var refreshed = ReadBack(order.Id);
        refreshed.AwbNumber.Should().Be("RO-PRINT");
        refreshed.Status.Should().Be(OrderStatus.Printing);
    }

    [Fact]
    public async Task Returns_GiveUp_when_mapper_throws_for_invalid_input()
    {
        var order = SeedOrder(withItems: false);
        using var db = CreateDb();
        var client = new Mock<ISamedayClient>(MockBehavior.Strict);
        var sut = Build(db, client);

        var outcome = await sut.CreateForOrderAsync(order.Id, attempt: 1);

        outcome.Should().BeOfType<AwbCreationOutcome.GiveUp>()
            .Which.Reason.Should().Contain("invalid request");
        client.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Returns_transient_RetryLater_on_a_vendor_call_timeout()
    {
        var order = SeedOrder();
        using var db = CreateDb();
        var client = new Mock<ISamedayClient>();
        client.Setup(c => c.CreateAwbAsync(It.IsAny<AwbCreationRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException()); // HttpClient timeout, not shutdown

        var sut = Build(db, client);
        var outcome = await sut.CreateForOrderAsync(order.Id, attempt: 1);

        outcome.Should().BeOfType<AwbCreationOutcome.RetryLater>()
            .Which.IsTransient.Should().BeTrue();
    }

    [Fact]
    public async Task Returns_RetryLater_transient_on_SamedayUnreachableException()
    {
        // Transport failure (no HTTP status): the request never reached the vendor, so it's
        // pre-create — the claim is released for a prompt in-process retry.
        var order = SeedOrder();
        using var db = CreateDb();
        var client = new Mock<ISamedayClient>();
        client.Setup(c => c.CreateAwbAsync(It.IsAny<AwbCreationRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new SamedayUnreachableException("/api/awb"));

        var sut = Build(db, client);
        var outcome = await sut.CreateForOrderAsync(order.Id, attempt: 1);

        var retry = outcome.Should().BeOfType<AwbCreationOutcome.RetryLater>().Subject;
        retry.IsTransient.Should().BeTrue();
        retry.PreserveClaim.Should().BeFalse();
        ReadBack(order.Id).AwbClaimedAt.Should().BeNull(); // released
    }

    [Fact]
    public async Task Preserves_the_claim_on_a_retryable_status_from_the_create_call()
    {
        // A 5xx/408/429 response means the vendor received the request and may have billed the AWB;
        // hold the claim like the timeout path (unlike a status-less transport failure).
        var order = SeedOrder();
        using var db = CreateDb();
        var client = new Mock<ISamedayClient>();
        client.Setup(c => c.CreateAwbAsync(It.IsAny<AwbCreationRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new SamedayUnreachableException("/api/awb", httpStatus: 503));

        var sut = Build(db, client);
        var outcome = await sut.CreateForOrderAsync(order.Id, attempt: 1);

        var retry = outcome.Should().BeOfType<AwbCreationOutcome.RetryLater>().Subject;
        retry.IsTransient.Should().BeTrue();
        retry.PreserveClaim.Should().BeTrue();
        ReadBack(order.Id).AwbClaimedAt.Should().Be(Now); // held, not released
    }

    [Fact]
    public async Task Returns_RetryLater_non_transient_on_SamedayAuthException()
    {
        var order = SeedOrder();
        using var db = CreateDb();
        var client = new Mock<ISamedayClient>();
        client.Setup(c => c.CreateAwbAsync(It.IsAny<AwbCreationRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new SamedayAuthException("/api/awb"));

        var sut = Build(db, client);
        var outcome = await sut.CreateForOrderAsync(order.Id, attempt: 1);

        outcome.Should().BeOfType<AwbCreationOutcome.RetryLater>()
            .Which.IsTransient.Should().BeFalse();
    }

    [Fact]
    public async Task Returns_RetryLater_non_transient_on_SamedayProtocolException()
    {
        var order = SeedOrder();
        using var db = CreateDb();
        var client = new Mock<ISamedayClient>();
        client.Setup(c => c.CreateAwbAsync(It.IsAny<AwbCreationRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new SamedayProtocolException("/api/awb", "bad shape"));

        var sut = Build(db, client);
        var outcome = await sut.CreateForOrderAsync(order.Id, attempt: 1);

        outcome.Should().BeOfType<AwbCreationOutcome.RetryLater>()
            .Which.IsTransient.Should().BeFalse();
    }

    [Fact]
    public async Task Returns_GiveUp_on_SamedayValidationException()
    {
        var order = SeedOrder();
        using var db = CreateDb();
        var client = new Mock<ISamedayClient>();
        client.Setup(c => c.CreateAwbAsync(It.IsAny<AwbCreationRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new SamedayValidationException("/api/awb", 422, "validation failed"));

        var sut = Build(db, client);
        var outcome = await sut.CreateForOrderAsync(order.Id, attempt: 1);

        outcome.Should().BeOfType<AwbCreationOutcome.GiveUp>();
    }

    [Fact]
    public async Task Persists_the_AWB_but_drops_an_over_length_label_url()
    {
        // a vendor URL longer than the column must not throw the persist (which would loop the
        // billable retry); the AWB number is recorded and the label dropped (fetchable by number).
        var order = SeedOrder();
        var longUrl = "https://sameday/labels/" + new string('a', Order.MaxAwbLabelUrlLength);
        using var db = CreateDb();
        var sut = Build(db, ClientReturning(url: longUrl));

        var outcome = await sut.CreateForOrderAsync(order.Id, attempt: 1);

        outcome.Should().BeOfType<AwbCreationOutcome.Created>();
        var refreshed = ReadBack(order.Id);
        refreshed.AwbNumber.Should().Be("RO12345678");
        refreshed.AwbLabelUrl.Should().BeNull();
    }

    [Fact]
    public async Task Reclaims_a_stale_claim_and_creates_the_AWB()
    {
        // a worker that crashed mid-claim must not strand the order — after the TTL another
        // creator reclaims it.
        var order = SeedOrder();
        SetClaim(order.Id, Now.AddMinutes(-10)); // older than the claim TTL

        using var db = CreateDb();
        var sut = Build(db, ClientReturning());

        var outcome = await sut.CreateForOrderAsync(order.Id, attempt: 1);

        outcome.Should().BeOfType<AwbCreationOutcome.Created>();
        ReadBack(order.Id).AwbNumber.Should().Be("RO12345678");
    }

    [Fact]
    public async Task Releases_the_claim_after_a_definitive_failure()
    {
        // on a non-preserving failure (unreachable) the claim is released so an in-process
        // retry can re-claim promptly instead of waiting out the TTL.
        var order = SeedOrder();
        using var db = CreateDb();
        var client = new Mock<ISamedayClient>();
        client.Setup(c => c.CreateAwbAsync(It.IsAny<AwbCreationRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new SamedayUnreachableException("/api/awb"));
        var sut = Build(db, client);

        await sut.CreateForOrderAsync(order.Id, attempt: 1);

        ReadBack(order.Id).AwbClaimedAt.Should().BeNull();
    }

    [Fact]
    public async Task Preserves_the_claim_when_the_persist_fails_after_the_vendor_created_the_AWB()
    {
        using var isolated = UseIsolatedDatabase();
        // The AWB is created and billed but the DB write throws: the claim must be held so the
        // re-attempt waits out the TTL rather than re-calling the vendor in ~30 s for a 2nd label.
        var order = SeedOrder();
        using var db = CreateDb();
        var client = new Mock<ISamedayClient>();
        client.Setup(c => c.CreateAwbAsync(It.IsAny<AwbCreationRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
            {
                // Break the DB between the vendor call and the persist.
                _database.BreakOrdersTable();
                return new AwbCreationResult("RO12345678", "https://sameday/labels/abc.pdf", 18.50m);
            });
        var sut = Build(db, client);

        var outcome = await sut.CreateForOrderAsync(order.Id, attempt: 1);

        outcome.Should().BeOfType<AwbCreationOutcome.RetryLater>()
            .Which.PreserveClaim.Should().BeTrue();
    }

    [Fact]
    public async Task Preserves_the_claim_on_a_vendor_timeout()
    {
        // a timeout leaves the AWB state unknown (may be billed); the claim is held so the
        // re-attempt waits out the TTL rather than re-calling the vendor and risking a 2nd label.
        var order = SeedOrder();
        using var db = CreateDb();
        var client = new Mock<ISamedayClient>();
        client.Setup(c => c.CreateAwbAsync(It.IsAny<AwbCreationRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException());
        var sut = Build(db, client);

        var outcome = await sut.CreateForOrderAsync(order.Id, attempt: 1);

        outcome.Should().BeOfType<AwbCreationOutcome.RetryLater>()
            .Which.PreserveClaim.Should().BeTrue();
        ReadBack(order.Id).AwbClaimedAt.Should().Be(Now);
    }

    // ── Outcome metric ────────────────────────────────────────────────────────

    [Fact]
    public async Task Records_an_ok_outcome_on_the_awb_counter()
    {
        var order = SeedOrder();
        using var db = CreateDb();
        var sut = Build(db, ClientReturning());
        using var metrics = new MetricCapture(MetricNames.Instruments.AwbCreationTotal);

        await sut.CreateForOrderAsync(order.Id, attempt: 1);

        metrics.For(MetricNames.Instruments.AwbCreationTotal,
                (MetricNames.Labels.Result, MetricNames.AwbResultValues.Ok))
            .Should().HaveCount(1);
        metrics.ContractViolations().Should().BeEmpty();
    }

    [Fact]
    public async Task An_orphaned_label_records_its_own_outcome_rather_than_skipped()
    {
        var order = SeedOrder();

        var client = new Mock<ISamedayClient>();
        client.Setup(c => c.CreateAwbAsync(It.IsAny<AwbCreationRequest>(), It.IsAny<CancellationToken>()))
            .Returns<AwbCreationRequest, CancellationToken>((_, _) =>
            {
                SetStatus(order.Id, OrderStatus.Cancelled);
                return Task.FromResult(new AwbCreationResult("RO-ORPHAN", "https://x/y.pdf", 1m));
            });

        using var db = CreateDb();
        var sut = Build(db, client);
        using var metrics = new MetricCapture(MetricNames.Instruments.AwbCreationTotal);

        await sut.CreateForOrderAsync(order.Id, attempt: 1);

        metrics.For(MetricNames.Instruments.AwbCreationTotal,
                (MetricNames.Labels.Result, MetricNames.AwbResultValues.Orphaned))
            .Should().HaveCount(1,
                "a billable label nothing references is a real failure SLO 4 has to see, and "
                    + "`skipped` is excluded from both sides of that ratio — recording it there "
                    + "would make ops' worst AWB outcome the one the panel cannot show");
        metrics.For(MetricNames.Instruments.AwbCreationTotal,
                (MetricNames.Labels.Result, MetricNames.AwbResultValues.Skipped))
            .Should().BeEmpty();
        metrics.ContractViolations().Should().BeEmpty();
    }

    [Fact]
    public async Task A_thrown_db_failure_records_an_error_outcome_and_rethrows()
    {
        using var isolated = UseIsolatedDatabase();
        var order = SeedOrder();
        using var db = CreateDb();
        var sut = Build(db, new Mock<ISamedayClient>(MockBehavior.Strict));
        using var metrics = new MetricCapture(MetricNames.Instruments.AwbCreationTotal);

        // Stands in for an unreachable database under the creator.
        _database.BreakOrdersTable();

        var act = () => sut.CreateForOrderAsync(order.Id, attempt: 1);

        await act.Should().ThrowAsync<Exception>();
        metrics.For(MetricNames.Instruments.AwbCreationTotal,
                (MetricNames.Labels.Result, MetricNames.AwbResultValues.Error))
            .Should().HaveCount(1,
                "an outage that produces no outcome must still reach the SLO denominator");
        metrics.ContractViolations().Should().BeEmpty();
    }

    [Fact]
    public async Task Shutdown_cancellation_records_no_outcome()
    {
        var order = SeedOrder();
        using var db = CreateDb();
        var sut = Build(db, new Mock<ISamedayClient>(MockBehavior.Strict));
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();
        using var metrics = new MetricCapture(MetricNames.Instruments.AwbCreationTotal);

        var act = () => sut.CreateForOrderAsync(order.Id, attempt: 1, cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
        metrics.Measurements.Should().BeEmpty(
            "draining in-flight jobs on every deploy would permanently depress the AWB SLO");
    }

    private void SetStatus(Guid id, OrderStatus status)
    {
        using var db = CreateDb();
        db.Orders.Where(o => o.Id == id).ExecuteUpdate(s => s.SetProperty(o => o.Status, status));
    }

    private void SetAwb(Guid id, string awb)
    {
        using var db = CreateDb();
        db.Orders.Where(o => o.Id == id).ExecuteUpdate(s => s.SetProperty(o => o.AwbNumber, awb));
    }

    private void SetClaim(Guid id, DateTimeOffset claimedAt)
    {
        using var db = CreateDb();
        db.Orders.Where(o => o.Id == id).ExecuteUpdate(s => s.SetProperty(o => o.AwbClaimedAt, (DateTimeOffset?)claimedAt));
    }
}
