using Microsoft.EntityFrameworkCore;
using PhotoPrint.API.Data;
using PhotoPrint.API.Models;
using Xunit;

namespace PhotoPrint.Tests.Unit.Data;

/// <summary>
/// Regression guard for the <c>Order.StripeClientSecret</c> column
/// width. The column was sized at exactly Stripe's documented 255-char ceiling — zero
/// headroom. On SQLite/InMemory (dev/test) <c>HasMaxLength</c> is not enforced, so an
/// over-length secret stores silently and every idempotency test stays green; only prod
/// Postgres (<c>character varying(N)</c>) throws "value too long" on SaveChangesAsync,
/// after Stripe already created the charge. Asserting the configured max length here is a
/// provider-independent guard: it fails the moment the width regresses below the agreed
/// margin, without needing a Postgres connection in the test matrix.
/// </summary>
public class OrderIdempotencyColumnTests
{
    private static PhotoPrintDbContext NewContext()
    {
        var options = new DbContextOptionsBuilder<PhotoPrintDbContext>()
            .UseInMemoryDatabase($"order-col-{Guid.NewGuid():N}")
            .Options;
        return new PhotoPrintDbContext(options);
    }

    [Fact]
    public void StripeClientSecret_HasHeadroomAboveStripesIdCeiling()
    {
        using var db = NewContext();

        var maxLength = db.Model
            .FindEntityType(typeof(Order))!
            .FindProperty(nameof(Order.StripeClientSecret))!
            .GetMaxLength();

        // Must clear 255 (Stripe's documented ceiling, which today's ~60–90 char secrets
        // sit just under) with real margin against Stripe lengthening IDs again.
        Assert.NotNull(maxLength);
        Assert.True(
            maxLength >= 512,
            $"StripeClientSecret max length is {maxLength}; expected >= 512 of headroom (DB-2).");
    }
}
