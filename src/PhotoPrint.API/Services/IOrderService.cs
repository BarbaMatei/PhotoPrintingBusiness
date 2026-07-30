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
    /// <see cref="Exceptions.IdempotencyConflictException"/> (HTTP 409).
    ///
    /// The 24h window and stale-key reclamation are <b>owner-scoped</b>: a stale key is freed only when its original owner resubmits. A different
    /// caller presenting a key already held by another tenant gets an
    /// <see cref="Exceptions.IdempotencyKeyTakenException"/> (HTTP 409) from the global
    /// unique index — never a replay, and never the other tenant's order.
    /// </summary>
    Task<OrderCreationResult> CreateFromCartAsync(
        Guid? userId,
        Guid? guestSessionId,
        CreateOrderRequest request,
        string? idempotencyKey = null,
        CancellationToken ct = default);

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
    /// scoped to the calling user. Pre-promotion uploads and post-retention
    /// blobs are omitted from the result (not errored). Cloud-tier-off returns an empty
    /// photos list.
    /// Throws <see cref="Exceptions.NotFoundException"/> if the order is not found.
    /// Throws <see cref="Exceptions.ForbiddenException"/> if the order belongs to a different user.
    /// </summary>
    Task<OrderPhotosDto> GetOrderPhotosAsync(Guid orderId, Guid userId, CancellationToken ct = default);
}
