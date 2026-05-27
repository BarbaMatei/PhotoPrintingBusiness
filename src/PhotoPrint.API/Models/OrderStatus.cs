namespace PhotoPrint.API.Models;

public enum OrderStatus
{
    AwaitingPayment,
    Paid,
    Printing,
    Shipped,
    Delivered,
    PaymentFailed,
    Cancelled,
}
