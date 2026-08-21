using PhotoPrint.API.Models;

namespace PhotoPrint.API.Services.Invoicing;

public static class InvoiceAddressFormatter
{
    public const int StreetNameMaxLength = 150;
    public const int CityNameMaxLength   = 50;
    public const int PartyNameMaxLength  = 200;

    public static string FormatStreetName(string? street, string? number, string? block) =>
        string.Join(' ', new[] { street, number, block }.Where(s => !string.IsNullOrWhiteSpace(s)));

    // StreetName, CityName and PostalZone are mandatory on a CIUS-RO invoice, so XML and PDF refuse the same snapshot.
    public static void EnsureBuyerAddressUsable(Order order)
    {
        var addr = order.ShippingAddress;
        if (addr is null)
            throw new InvoiceNotBuildableException(
                $"Order {order.OrderNumber} has no buyer address: no shipping-address snapshot was recorded.");

        var missing = new[]
        {
            string.IsNullOrWhiteSpace(FormatStreetName(addr.Street, addr.Number, addr.Block)) ? "StreetName" : null,
            string.IsNullOrWhiteSpace(addr.City) ? "CityName" : null,
            string.IsNullOrWhiteSpace(addr.PostalCode) ? "PostalZone" : null,
        }.Where(f => f is not null).ToList();

        if (missing.Count > 0)
            throw new InvoiceNotBuildableException(
                $"Order {order.OrderNumber} has no buyer address: {string.Join(", ", missing)} would be empty, " +
                "and all three are mandatory.");
    }

    // Null-tolerant: the snapshot's non-nullable strings are still null when a client omits the field, and the validators bound length without requiring presence.
    public static string Truncate(string? value, int maxLength)
    {
        if (string.IsNullOrEmpty(value)) return string.Empty;
        if (value.Length <= maxLength) return value;

        // Never cut between a surrogate pair — a lone half is not valid XML and wedges the invoice in Pending.
        var end = maxLength;
        if (char.IsHighSurrogate(value[end - 1])) end--;
        return value[..end];
    }
}
