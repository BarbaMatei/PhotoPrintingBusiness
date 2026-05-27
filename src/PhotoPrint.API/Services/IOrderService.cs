using PhotoPrint.API.DTOs.Orders;
using PhotoPrint.API.DTOs.Payments;
using PhotoPrint.API.Models;

namespace PhotoPrint.API.Services;

public interface IOrderService
{
    /// <summary>
    /// Creates an Order (+ OrderItems) from the current cart of the given user/guest.
    /// Throws <see cref="Exceptions.BadRequestException"/> when the cart is empty.
    /// </summary>
    Task<Order> CreateFromCartAsync(
        Guid? userId,
        Guid? guestSessionId,
        CreateOrderRequest request,
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
}
