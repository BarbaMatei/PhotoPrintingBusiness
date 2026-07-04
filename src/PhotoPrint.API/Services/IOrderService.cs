using PhotoPrint.API.DTOs.Orders;
using PhotoPrint.API.DTOs.Payments;
using PhotoPrint.API.Models;

namespace PhotoPrint.API.Services;

public interface IOrderService
{
    /// <summary>
    /// Creates an Order (+ OrderItems) from the current cart of the given user/guest.
    /// Throws <see cref="Exceptions.BadRequestException"/> when the cart is empty.
    ///
    /// When <paramref name="idempotencyKey"/> is supplied and already maps to an
    /// order created within the 24h window, that order is returned with
    /// <c>WasIdempotentReplay = true</c> (no new row, no new payment intent).
    /// If the key maps to an order with a divergent logical request, throws
    /// <see cref="Exceptions.IdempotencyConflictException"/> (HTTP 409, see ADR-004).
    ///
    /// The 24h window and stale-key reclamation are <b>owner-scoped</b> (REQ-1, review
    /// 035-v8): a stale key is freed only when its original owner resubmits. A different
    /// caller presenting a key already held by another tenant gets an
    /// <see cref="Exceptions.IdempotencyKeyTakenException"/> (HTTP 409) from the global
    /// unique index — never a replay, and never the other tenant's order (SEC-1).
    /// </summary>
    Task<OrderCreationResult> CreateFromCartAsync(
        Guid? userId,
        Guid? guestSessionId,
        CreateOrderRequest request,
        string? idempotencyKey = null,
        CancellationToken ct = default);

    /// <summary>
    /// Returns the order bound to the given Idempotency-Key <b>for the supplied
    /// caller</b>, but only when it was created within the 24h idempotency window.
    /// Stale matches return null. The lookup is scoped to <paramref name="userId"/>
    /// / <paramref name="guestSessionId"/> so a caller can never resolve another
    /// tenant's order via a guessed/borrowed key (SEC-1, review 035-v1).
    /// </summary>
    Task<Order?> GetByIdempotencyKeyAsync(
        string key, Guid? userId, Guid? guestSessionId, CancellationToken ct = default);

    Task<Order?> GetByPaymentIntentIdAsync(string paymentIntentId, CancellationToken ct = default);

    Task<Order?> GetByIdAsync(Guid orderId, CancellationToken ct = default);

    /// <summary>
    /// Returns a paginated list of orders owned by the given user, newest first.
    /// </summary>
    Task<(IReadOnlyList<OrderSummaryDto> Items, int Total)> GetOrdersAsync(
        Guid userId, int page, int pageSize, CancellationToken ct = default);

    /// <summary>
    /// Returns the full detail for a single order.
    /// Throws <see cref="Exceptions.NotFoundException"/> if not found.
    /// Throws <see cref="Exceptions.ForbiddenException"/> if the order belongs to a different user.
    /// </summary>
    Task<OrderDetailDto> GetOrderDetailAsync(Guid orderId, Guid userId, CancellationToken ct = default);
}
