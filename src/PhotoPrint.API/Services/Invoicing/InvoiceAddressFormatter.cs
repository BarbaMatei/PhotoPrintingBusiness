using PhotoPrint.API.Models;

using System.Text;

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

        value = StripXmlInvalid(value);
        if (value.Length <= maxLength) return value;

        // Never cut between a surrogate pair — a lone half is not valid XML and wedges the invoice in Pending.
        var end = maxLength;
        if (char.IsHighSurrogate(value[end - 1])) end--;
        return value[..end];
    }

    // XmlWriter emits a character XML 1.0 forbids as a reference no parser accepts, so a name
    // pasted from a word processor would wedge the invoice in Pending for ever.
    public static string StripXmlInvalid(string? value)
    {
        if (string.IsNullOrEmpty(value)) return string.Empty;

        var kept = new StringBuilder(value.Length);
        foreach (var ch in value)
        {
            var code = (int)ch;
            var legal = code is 0x09 or 0x0A or 0x0D ||
                        (code >= 0x20 && code <= 0xD7FF) ||
                        (code >= 0xE000 && code <= 0xFFFD) ||
                        char.IsSurrogate(ch);
            if (legal) kept.Append(ch);
        }
        return kept.ToString();
    }
}
