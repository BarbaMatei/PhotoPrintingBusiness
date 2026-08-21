using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PhotoPrint.Tests.Helpers;
using PhotoPrint.API.Data;
using PhotoPrint.API.Models;
using PhotoPrint.API.Services.Invoicing;

namespace PhotoPrint.Tests.Unit.Services.Invoicing;

/// <summary>
/// CAS transition tests for <see cref="InvoiceLifecycle"/> (ADR-016). Uses
/// a real PostgreSQL database (NOT EF InMemory) because <c>ExecuteUpdateAsync</c> is
/// what we're verifying — it doesn't translate cleanly under EF InMemory.
/// </summary>
public class InvoiceLifecycleTests : IDisposable
{
    private readonly PostgresTestDatabase _database = new();
    private readonly PhotoPrintDbContext _db;
    private readonly InvoiceLifecycle _sut;
    private readonly DateTimeOffset _now = new(2026, 6, 3, 12, 0, 0, TimeSpan.Zero);

    public InvoiceLifecycleTests()
    {
        _db = _database.NewContext();
        _sut = new InvoiceLifecycle(
            _db,
            new FakeClock(_now),
            new LoggerFactory().CreateLogger<InvoiceLifecycle>());
    }

    public void Dispose()
    {
        _db.Dispose();
        _database.Dispose();
    }

    private async Task<Guid> SeedInvoiceAsync(
        InvoiceAnafStatus status, string? lastError = null,
        string? xmlPayload = null, string? pdfStoragePath = null)
    {
        var order = new Order
        {
            Id = Guid.NewGuid(),
            OrderNumber = $"FT-{Random.Shared.Next(100_000, 999_999)}",
            Status = OrderStatus.Paid,
            DeliveryType = DeliveryType.Courier,
            ShippingAddress = new ShippingAddressSnapshot
            {
                RecipientName = "x", Phone = "x", Street = "x", Number = "1",
                City = "x", County = "x", PostalCode = "x",
            },
        };
        _db.Orders.Add(order);

        var invoice = new Invoice
        {
            Id = Guid.NewGuid(),
            OrderId = order.Id,
            InvoiceNumber = "FT-2026-00001",
            Series = "FT",
            Number = 1,
            IssuedAt = _now,
            NetTotalRon = 100m, VatRon = 19m, TotalRon = 119m,
            AnafStatus = status,
            LastError = lastError,
            XmlPayload = xmlPayload,
            PdfStoragePath = pdfStoragePath,
            CreatedAt = _now,
        };
        _db.Invoices.Add(invoice);
        await _db.SaveChangesAsync();
        return invoice.Id;
    }

    [Fact]
    public async Task MarkSubmitted_from_Pending_sets_status_and_upload_id()
    {
        var id = await SeedInvoiceAsync(InvoiceAnafStatus.Pending);

        var ok = await _sut.MarkSubmittedAsync(id, "anaf-upload-42", CancellationToken.None);

        ok.Should().BeTrue();
        var fresh = await _db.Invoices.AsNoTracking().FirstAsync(i => i.Id == id);
        fresh.AnafStatus.Should().Be(InvoiceAnafStatus.Submitted);
        fresh.AnafUploadId.Should().Be("anaf-upload-42");
        fresh.LastError.Should().BeNull();
    }

    [Fact]
    public async Task MarkSubmitted_from_wrong_state_loses_cas_and_returns_false()
    {
        var id = await SeedInvoiceAsync(InvoiceAnafStatus.Accepted);

        var ok = await _sut.MarkSubmittedAsync(id, "anaf-x", CancellationToken.None);

        ok.Should().BeFalse();
        var fresh = await _db.Invoices.AsNoTracking().FirstAsync(i => i.Id == id);
        fresh.AnafStatus.Should().Be(InvoiceAnafStatus.Accepted);  // untouched
        fresh.AnafUploadId.Should().BeNull();
    }

    [Fact]
    public async Task RecordPendingError_keeps_status_pending_but_writes_message()
    {
        var id = await SeedInvoiceAsync(InvoiceAnafStatus.Pending);

        var ok = await _sut.RecordPendingErrorAsync(id, "Invalid CUI", CancellationToken.None);

        ok.Should().BeTrue();
        var fresh = await _db.Invoices.AsNoTracking().FirstAsync(i => i.Id == id);
        fresh.AnafStatus.Should().Be(InvoiceAnafStatus.Pending);
        fresh.LastError.Should().Be("Invalid CUI");
    }

    [Fact]
    public async Task MarkAccepted_from_Submitted_clears_last_error()
    {
        var id = await SeedInvoiceAsync(InvoiceAnafStatus.Submitted, lastError: "stale error");

        var ok = await _sut.MarkAcceptedAsync(id, CancellationToken.None);

        ok.Should().BeTrue();
        var fresh = await _db.Invoices.AsNoTracking().FirstAsync(i => i.Id == id);
        fresh.AnafStatus.Should().Be(InvoiceAnafStatus.Accepted);
        fresh.LastError.Should().BeNull();
    }

    [Fact]
    public async Task MarkRejected_records_error_and_transitions_to_Rejected()
    {
        var id = await SeedInvoiceAsync(InvoiceAnafStatus.Submitted);

        var ok = await _sut.MarkRejectedAsync(id, "ANAF: CUI mismatch", CancellationToken.None);

        ok.Should().BeTrue();
        var fresh = await _db.Invoices.AsNoTracking().FirstAsync(i => i.Id == id);
        fresh.AnafStatus.Should().Be(InvoiceAnafStatus.Rejected);
        fresh.LastError.Should().Be("ANAF: CUI mismatch");
    }

    [Fact]
    public async Task MarkFailed_from_Submitted_records_terminal_state()
    {
        var id = await SeedInvoiceAsync(InvoiceAnafStatus.Submitted);

        var ok = await _sut.MarkFailedAsync(id, "Budget exhausted after 85h", CancellationToken.None);

        ok.Should().BeTrue();
        var fresh = await _db.Invoices.AsNoTracking().FirstAsync(i => i.Id == id);
        fresh.AnafStatus.Should().Be(InvoiceAnafStatus.Failed);
        fresh.LastError.Should().Contain("85h");
    }

    [Theory]
    [InlineData(InvoiceAnafStatus.Rejected)]
    [InlineData(InvoiceAnafStatus.Failed)]
    public async Task Retry_from_terminal_state_resets_to_Pending_and_clears_fields(InvoiceAnafStatus from)
    {
        var id = await SeedInvoiceAsync(from, lastError: "prior error",
            xmlPayload: "<Invoice>rejected-content</Invoice>", pdfStoragePath: "invoices/2026/FT-2026-00001.pdf");
        // Pre-set AnafUploadId so we can verify it's cleared.
        await _db.Invoices.Where(i => i.Id == id)
            .ExecuteUpdateAsync(s => s.SetProperty(i => i.AnafUploadId, "old-id"));

        var ok = await _sut.RetryAsync(id, from, CancellationToken.None);

        ok.Should().BeTrue();
        var fresh = await _db.Invoices.AsNoTracking().FirstAsync(i => i.Id == id);
        fresh.AnafStatus.Should().Be(InvoiceAnafStatus.Pending);
        fresh.AnafUploadId.Should().BeNull();
        fresh.LastError.Should().BeNull();
        fresh.XmlPayload.Should().BeNull();
        fresh.PdfStoragePath.Should().Be("invoices/2026/FT-2026-00001.pdf");
    }

    [Fact]
    public async Task RecordUnknownUploadOutcome_below_the_budget_counts_and_stays_Pending()
    {
        var id = await SeedInvoiceAsync(InvoiceAnafStatus.Pending);

        var outcome = await _sut.RecordUnknownUploadOutcomeAsync(
            id, "no answer", "budget spent", maxOutcomes: 3, CancellationToken.None);

        outcome.Should().Be(new UnknownUploadOutcome(1, false));
        var fresh = await _db.Invoices.AsNoTracking().FirstAsync(i => i.Id == id);
        fresh.AnafStatus.Should().Be(InvoiceAnafStatus.Pending);
        fresh.LastError.Should().Be("no answer");
    }

    [Fact]
    public async Task RecordUnknownUploadOutcome_at_the_budget_parks_the_row_and_drops_its_claim()
    {
        var id = await SeedInvoiceAsync(InvoiceAnafStatus.Pending);
        await _db.Invoices.Where(i => i.Id == id)
            .ExecuteUpdateAsync(s => s
                .SetProperty(i => i.UnknownUploadOutcomes, 2)
                .SetProperty(i => i.ClaimedAt, (DateTimeOffset?)_now));

        var outcome = await _sut.RecordUnknownUploadOutcomeAsync(
            id, "no answer", "budget spent", maxOutcomes: 3, CancellationToken.None);

        outcome.Should().Be(new UnknownUploadOutcome(3, true));
        var fresh = await _db.Invoices.AsNoTracking().FirstAsync(i => i.Id == id);
        fresh.AnafStatus.Should().Be(InvoiceAnafStatus.Failed);
        fresh.LastError.Should().Be("budget spent");
        fresh.ClaimedAt.Should().BeNull("a parked row is nobody's to hold");
    }

    // The upload runs outside any transaction, so an admin retry or another worker can move the row while it is in flight.
    [Fact]
    public async Task RecordUnknownUploadOutcome_on_a_row_that_left_Pending_counts_nothing()
    {
        var id = await SeedInvoiceAsync(InvoiceAnafStatus.Submitted);

        var outcome = await _sut.RecordUnknownUploadOutcomeAsync(
            id, "no answer", "budget spent", maxOutcomes: 3, CancellationToken.None);

        outcome.Should().Be(new UnknownUploadOutcome(0, false));
        var fresh = await _db.Invoices.AsNoTracking().FirstAsync(i => i.Id == id);
        fresh.AnafStatus.Should().Be(InvoiceAnafStatus.Submitted);
        fresh.UnknownUploadOutcomes.Should().Be(0);
        fresh.LastError.Should().BeNull("a row that moved on must not be relabelled by a stale upload");
    }

    // Without the reset an operator's retry of a parked row would be parked again on its first tick.
    [Fact]
    public async Task Retry_clears_the_blind_repost_budget()
    {
        var id = await SeedInvoiceAsync(InvoiceAnafStatus.Failed, lastError: "outcome unknown");
        await _db.Invoices.Where(i => i.Id == id)
            .ExecuteUpdateAsync(s => s.SetProperty(i => i.UnknownUploadOutcomes, 3));

        var ok = await _sut.RetryAsync(id, InvoiceAnafStatus.Failed, CancellationToken.None);

        ok.Should().BeTrue();
        var fresh = await _db.Invoices.AsNoTracking().FirstAsync(i => i.Id == id);
        fresh.UnknownUploadOutcomes.Should().Be(0);
    }

    [Fact]
    public async Task Retry_with_mismatched_expected_returns_false()
    {
        var id = await SeedInvoiceAsync(InvoiceAnafStatus.Accepted);

        // Caller expects Rejected, but row is Accepted — CAS loses.
        var ok = await _sut.RetryAsync(id, InvoiceAnafStatus.Rejected, CancellationToken.None);

        ok.Should().BeFalse();
        var fresh = await _db.Invoices.AsNoTracking().FirstAsync(i => i.Id == id);
        fresh.AnafStatus.Should().Be(InvoiceAnafStatus.Accepted);  // untouched
    }

    private sealed class FakeClock : TimeProvider
    {
        private readonly DateTimeOffset _now;
        public FakeClock(DateTimeOffset now) { _now = now; }
        public override DateTimeOffset GetUtcNow() => _now;
    }
}
