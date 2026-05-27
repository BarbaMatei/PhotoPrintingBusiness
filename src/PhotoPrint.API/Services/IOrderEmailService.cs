using PhotoPrint.API.Models;

namespace PhotoPrint.API.Services;

/// <summary>
/// Fires fire-and-forget transactional emails for order lifecycle events.
/// The Order parameter must have User (if registered) and EasyboxLocker nav properties loaded.
/// For order-confirmed, Items must also be loaded.
/// </summary>
public interface IOrderEmailService
{
    void FireOrderConfirmedEmail(Order order);
    void FireOrderShippedEmail(Order order);
    void FireOrderDeliveredEmail(Order order);
    void FireOrderCancelledEmail(Order order, string? reason);
}
