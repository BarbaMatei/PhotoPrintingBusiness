using PhotoPrint.API.Exceptions;
using PhotoPrint.API.Models;
using PhotoPrint.API.Services;

namespace PhotoPrint.Tests.Unit.Services;

public class OrderStatusMachineTests
{
    // ── Valid transitions ──────────────────────────────────────────────────────

    [Fact]
    public void AwaitingPayment_To_Paid_IsValid()
    {
        var order = MakeOrder(OrderStatus.AwaitingPayment);
        OrderStatusMachine.Transition(order, OrderStatus.Paid);
        Assert.Equal(OrderStatus.Paid, order.Status);
        Assert.NotNull(order.UpdatedAt);
    }

    [Fact]
    public void AwaitingPayment_To_PaymentFailed_IsValid()
    {
        var order = MakeOrder(OrderStatus.AwaitingPayment);
        OrderStatusMachine.Transition(order, OrderStatus.PaymentFailed);
        Assert.Equal(OrderStatus.PaymentFailed, order.Status);
    }

    [Fact]
    public void Paid_To_Printing_IsValid()
    {
        var order = MakeOrder(OrderStatus.Paid);
        OrderStatusMachine.Transition(order, OrderStatus.Printing);
        Assert.Equal(OrderStatus.Printing, order.Status);
    }

    [Fact]
    public void Printing_To_Shipped_IsValid()
    {
        var order = MakeOrder(OrderStatus.Printing);
        OrderStatusMachine.Transition(order, OrderStatus.Shipped);
        Assert.Equal(OrderStatus.Shipped, order.Status);
    }

    [Fact]
    public void Printing_To_Cancelled_IsValid()
    {
        var order = MakeOrder(OrderStatus.Printing);
        OrderStatusMachine.Transition(order, OrderStatus.Cancelled);
        Assert.Equal(OrderStatus.Cancelled, order.Status);
    }

    [Fact]
    public void Paid_To_Cancelled_IsValid()
    {
        var order = MakeOrder(OrderStatus.Paid);
        OrderStatusMachine.Transition(order, OrderStatus.Cancelled);
        Assert.Equal(OrderStatus.Cancelled, order.Status);
    }

    [Fact]
    public void Shipped_To_Delivered_IsValid()
    {
        var order = MakeOrder(OrderStatus.Shipped);
        OrderStatusMachine.Transition(order, OrderStatus.Delivered);
        Assert.Equal(OrderStatus.Delivered, order.Status);
    }

    // ── Invalid transitions ────────────────────────────────────────────────────

    [Fact]
    public void AwaitingPayment_To_Printing_ThrowsInvalidTransition()
    {
        var order = MakeOrder(OrderStatus.AwaitingPayment);
        Assert.Throws<InvalidOrderTransitionException>(() =>
            OrderStatusMachine.Transition(order, OrderStatus.Printing));
    }

    [Fact]
    public void Paid_To_Delivered_ThrowsInvalidTransition()
    {
        var order = MakeOrder(OrderStatus.Paid);
        Assert.Throws<InvalidOrderTransitionException>(() =>
            OrderStatusMachine.Transition(order, OrderStatus.Delivered));
    }

    [Fact]
    public void Delivered_To_Anything_ThrowsInvalidTransition()
    {
        var order = MakeOrder(OrderStatus.Delivered);
        Assert.Throws<InvalidOrderTransitionException>(() =>
            OrderStatusMachine.Transition(order, OrderStatus.Printing));
        Assert.Throws<InvalidOrderTransitionException>(() =>
            OrderStatusMachine.Transition(order, OrderStatus.Paid));
    }

    [Fact]
    public void Cancelled_To_Printing_ThrowsInvalidTransition()
    {
        var order = MakeOrder(OrderStatus.Cancelled);
        Assert.Throws<InvalidOrderTransitionException>(() =>
            OrderStatusMachine.Transition(order, OrderStatus.Printing));
    }

    [Fact]
    public void PaymentFailed_To_Paid_ThrowsInvalidTransition()
    {
        var order = MakeOrder(OrderStatus.PaymentFailed);
        Assert.Throws<InvalidOrderTransitionException>(() =>
            OrderStatusMachine.Transition(order, OrderStatus.Paid));
    }

    // ── CanTransition guard ───────────────────────────────────────────────────

    [Fact]
    public void CanTransition_ReturnsTrue_ForValidPair()
    {
        Assert.True(OrderStatusMachine.CanTransition(OrderStatus.AwaitingPayment, OrderStatus.Paid));
        Assert.True(OrderStatusMachine.CanTransition(OrderStatus.Printing, OrderStatus.Cancelled));
    }

    [Fact]
    public void CanTransition_ReturnsFalse_ForInvalidPair()
    {
        Assert.False(OrderStatusMachine.CanTransition(OrderStatus.Delivered, OrderStatus.Printing));
        Assert.False(OrderStatusMachine.CanTransition(OrderStatus.AwaitingPayment, OrderStatus.Delivered));
    }

    // ── Exception message format ──────────────────────────────────────────────

    [Fact]
    public void InvalidTransition_ExceptionMessage_ContainsFromAndTo()
    {
        var order = MakeOrder(OrderStatus.Delivered);
        var ex = Assert.Throws<InvalidOrderTransitionException>(() =>
            OrderStatusMachine.Transition(order, OrderStatus.Printing));
        Assert.Contains("Delivered", ex.Message);
        Assert.Contains("Printing", ex.Message);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static Order MakeOrder(OrderStatus status) => new Order
    {
        OrderNumber = "FT-20260001",
        Status = status,
        ShippingAddress = new ShippingAddressSnapshot
        {
            Street = "Str. Test", Number = "1", City = "București",
            County = "Ilfov", PostalCode = "010000",
            RecipientName = "Test User", Phone = "0700000000",
        },
    };
}
