namespace PhotoPrint.API.Services.Invoicing;

public static class InvoiceAddressFormatter
{
    public const int StreetNameMaxLength = 150;
    public const int CityNameMaxLength   = 50;
    public const int PartyNameMaxLength  = 200;

    public static string FormatStreetName(string? street, string? number, string? block) =>
        string.Join(' ', new[] { street, number, block }.Where(s => !string.IsNullOrWhiteSpace(s)));

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
