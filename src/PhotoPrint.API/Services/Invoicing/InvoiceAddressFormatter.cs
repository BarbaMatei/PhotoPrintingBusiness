namespace PhotoPrint.API.Services.Invoicing;

public static class InvoiceAddressFormatter
{
    public const int StreetNameMaxLength = 150;
    public const int CityNameMaxLength   = 50;
    public const int PartyNameMaxLength  = 200;

    public static string FormatStreetName(string? street, string? number, string? block) =>
        string.Join(' ', new[] { street, number, block }.Where(s => !string.IsNullOrWhiteSpace(s)));

    public static string Truncate(string value, int maxLength) =>
        value.Length <= maxLength ? value : value[..maxLength];
}
