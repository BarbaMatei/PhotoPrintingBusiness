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
    /// </summary>
    Task<OrderCreationResult> CreateFromCartAsync(
        Guid? userId,
        Guid? guestSessionId,
        CreateOrderRequest request,
        string? idempotencyKey = null,
        CancellationToken ct = default);

    /// <summary>
    /// Returns the order bound to the given Idempotency-Key, but only when it was
    /// created within the 24h idempotency window. Stale matches return null.
    /// </summary>
    Task<Order?> GetByIdempotencyKeyAsync(string key, CancellationToken ct = default);

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

    /// <summary>
    /// Returns the order's photos as presigned cloud URLs (thumbnail + large preview),
    /// scoped to the calling user (bolt 053). Pre-promotion uploads and post-retention
    /// blobs are omitted from the result (not errored). Cloud-tier-off returns an empty
    /// photos list — see ADR-008.
    /// Throws <see cref="Exceptions.NotFoundException"/> if the order is not found.
    /// Throws <see cref="Exceptions.ForbiddenException"/> if the order belongs to a different user.
    /// </summary>
    Task<OrderPhotosDto> GetOrderPhotosAsync(Guid orderId, Guid userId, CancellationToken ct = default);
}
