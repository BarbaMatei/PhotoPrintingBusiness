using PhotoPrint.API.Models;

namespace PhotoPrint.API.DTOs.Orders;

/// <summary>
/// Result of <c>IOrderService.CreateFromCartAsync</c>. <see cref="WasIdempotentReplay"/>
/// is true when the supplied Idempotency-Key matched an existing order within the
/// 24h window and that order was returned instead of creating a new one (bolt 035).
/// </summary>
public record OrderCreationResult(Order Order, bool WasIdempotentReplay);
