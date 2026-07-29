namespace PhotoPrint.API.Models;

public class Order
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string OrderNumber { get; set; } = null!;

    public Guid? UserId { get; set; }
    public Guid? GuestSessionId { get; set; }

    public OrderStatus Status { get; set; } = OrderStatus.AwaitingPayment;
    public PaymentProcessor PaymentProcessor { get; set; }
    public string? PaymentIntentId { get; set; }
    public string? EuPlatescTransactionId { get; set; }

    public ShippingAddressSnapshot ShippingAddress { get; set; } = null!;
    public DeliveryType DeliveryType { get; set; }
    public Guid? EasyboxLockerId { get; set; }

    public decimal ShippingCostRon { get; set; }
    public decimal SubtotalRon { get; set; }
    public decimal TotalRon { get; set; }

    // ── VAT breakdown (bolt 038) ─────────────────────────────────────────────
    /// <summary>Net (VAT-exclusive) total in RON. Snapshot at order creation;
    /// not re-derived from a live config rate. Invariant:
    /// <c>NetTotalRon + VatRon ≈ TotalRon</c> within ±0.01.</summary>
    public decimal NetTotalRon { get; set; }

    /// <summary>VAT amount extracted from <see cref="TotalRon"/> at
    /// <see cref="VatRate"/>. Romanian convention — VAT is included in
    /// customer-facing prices and extracted, not added on top.</summary>
    public decimal VatRon { get; set; }

    /// <summary>The VAT rate applied to this order, snapshotted at creation
    /// (default 0.19 = 19%). Changing <c>Vat:Rate</c> in config later does
    /// NOT mutate existing orders — the legal trail records the rate at
    /// time of sale.</summary>
    public decimal VatRate { get; set; }

    public string? AwbNumber { get; set; }
    public string? TrackingUrl { get; set; }

    /// <summary>URL to the Sameday-hosted PDF shipping label, populated alongside
    /// <see cref="AwbNumber"/> when the AWB workflow (bolt 037) successfully creates
    /// the AWB. Nullable: existing orders + orders whose AWB creation has not yet
    /// succeeded have no label URL.</summary>
    public string? AwbLabelUrl { get; set; }

    /// <summary>UTC timestamp of the most recent successful tracking poll against
    /// Sameday for <see cref="AwbNumber"/>. Updated by the tracking job (bolt 037)
    /// on every successful poll; nullable until the first successful poll.</summary>
    public DateTimeOffset? LastTrackingSyncAt { get; set; }

    // ── Idempotency (bolt 035) ───────────────────────────────────────────────
    /// <summary>Client-supplied Idempotency-Key bound to this order. Set once at
    /// creation, never modified — except nulled when a stale (&gt;24h) row's key is
    /// reused by a new request (see ddd-02 technical design + migration comment).</summary>
    public string? IdempotencyKey { get; set; }

    /// <summary>Cached Stripe ClientSecret so an idempotent replay returns the exact
    /// same secret without a second Stripe round-trip.</summary>
    public string? StripeClientSecret { get; set; }

    /// <summary>Cached EuPlatesc redirect URL. Persisted on first initiate because
    /// the URL embeds a timestamp + nonce and is therefore NOT reproducible on a
    /// later call; replay returns this stored value verbatim.</summary>
    public string? EuPlatescRedirectUrl { get; set; }

    /// <summary>Captured at order creation for guest orders (no User nav property).</summary>
    public string? GuestEmail { get; set; }

    /// <summary>Admin-only notes — never exposed to the customer.</summary>
    public string? InternalNotes { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAt { get; set; }
    public DateTimeOffset? PaidAt { get; set; }

    /// <summary>UTC timestamp set when the order is marked <c>Shipped</c> —
    /// either by an admin action (<see cref="AdminOrderService"/>) or by a
    /// future automation path. Anchors the bolt-037 tracking job's 30-day
    /// polling window (<c>Sameday:Jobs:TrackingMaxAgeDays</c>); orders
    /// older than the window are excluded from polling after a one-shot
    /// warning. Manual <c>Delivered</c> transitions that skip <c>Shipped</c>
    /// (legacy data) may have this null.</summary>
    public DateTimeOffset? ShippedAt { get; set; }

    /// <summary>UTC timestamp set by the bolt-037 tracking job when it observes
    /// a Sameday <c>delivered</c> state and successfully CAS-transitions
    /// <see cref="Status"/> from <c>Shipped</c> to <c>Delivered</c>. Invariant:
    /// <c>DeliveredAt is not null ⇔ Status == Delivered</c> for orders that
    /// reached delivery via the tracking job. Manual admin transitions to
    /// <c>Delivered</c> may leave this null; consumers must tolerate that.</summary>
    public DateTimeOffset? DeliveredAt { get; set; }

    // Navigation
    public User? User { get; set; }
    public EasyboxLocker? EasyboxLocker { get; set; }
    public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
}
