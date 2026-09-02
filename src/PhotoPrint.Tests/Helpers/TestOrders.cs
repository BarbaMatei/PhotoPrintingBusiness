using PhotoPrint.API.Models;

namespace PhotoPrint.Tests.Helpers;

public static class TestOrders
{
    public static Order Make(Guid id) => new()
    {
        Id = id,
        OrderNumber = $"FT-{Random.Shared.Next(100_000, 999_999)}",
        Status = OrderStatus.AwaitingPayment,
        NetTotalRon = 84.03m,
        VatRon = 15.97m,
        TotalRon = 100m,
        VatRate = 0.19m,
        ShippingAddress = new ShippingAddressSnapshot
        {
            RecipientName = "x", Phone = "x",
            Street = "Str. Test", Number = "1",
            City = "Cluj", County = "Cluj", PostalCode = "400100",
        },
    };

    public static Invoice MakeInvoice(
        Guid orderId, string series = "FT", int number = 1, string? invoiceNumber = null) => new()
    {
        OrderId = orderId,
        Series = series,
        Number = number,
        InvoiceNumber = invoiceNumber ?? $"{series}-2026-{number:D5}",
        IssuedAt = new DateTimeOffset(2026, 6, 3, 12, 0, 0, TimeSpan.Zero),
        AnafStatus = InvoiceAnafStatus.Pending,
    };
}
