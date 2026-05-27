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

    public string? AwbNumber { get; set; }
    public string? TrackingUrl { get; set; }

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

    // Navigation
    public User? User { get; set; }
    public EasyboxLocker? EasyboxLocker { get; set; }
    public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
}
