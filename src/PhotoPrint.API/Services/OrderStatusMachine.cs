using PhotoPrint.API.Exceptions;
using PhotoPrint.API.Models;

namespace PhotoPrint.API.Services;

/// <summary>
/// Enforces the OrderStatus state machine.
/// Valid transitions:
///   AwaitingPayment → Paid
///   AwaitingPayment → PaymentFailed
///   Paid            → Printing
///   Printing        → Shipped
///   Printing        → Cancelled
///   Shipped         → Delivered
/// </summary>
public static class OrderStatusMachine
{
    private static readonly HashSet<(OrderStatus From, OrderStatus To)> ValidTransitions =
    [
        (OrderStatus.AwaitingPayment, OrderStatus.Paid),
        (OrderStatus.AwaitingPayment, OrderStatus.PaymentFailed),
        (OrderStatus.Paid,            OrderStatus.Printing),
        (OrderStatus.Paid,            OrderStatus.Cancelled),
        (OrderStatus.Printing,        OrderStatus.Shipped),
        (OrderStatus.Printing,        OrderStatus.Cancelled),
        (OrderStatus.Shipped,         OrderStatus.Delivered),
    ];

    // Not an enum comparison: PaymentFailed and Cancelled sort after Delivered but are not paid.
    private static readonly HashSet<OrderStatus> PaidStatuses =
    [
        OrderStatus.Paid,
        OrderStatus.Printing,
        OrderStatus.Shipped,
        OrderStatus.Delivered,
    ];

    /// <summary>Returns true if transitioning from <paramref name="from"/> to <paramref name="to"/> is allowed.</summary>
    public static bool CanTransition(OrderStatus from, OrderStatus to)
        => ValidTransitions.Contains((from, to));

    public static bool HasBeenPaid(OrderStatus status) => PaidStatuses.Contains(status);

    /// <summary>
    /// Validates and applies a status transition.
    /// Throws <see cref="InvalidOrderTransitionException"/> if the transition is not allowed.
    /// </summary>
    public static void Transition(Order order, OrderStatus to)
    {
        if (!CanTransition(order.Status, to))
            throw new InvalidOrderTransitionException(order.Status.ToString(), to.ToString());

        order.Status = to;
        order.UpdatedAt = DateTimeOffset.UtcNow;
    }
}
