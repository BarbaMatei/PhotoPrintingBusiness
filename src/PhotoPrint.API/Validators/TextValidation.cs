using System.Text.RegularExpressions;

namespace PhotoPrint.API.Validators;

public static class TextValidation
{
    // XML 1.0 forbids these outside tab/LF/CR; shared by every validator whose field can reach the e-Factura XML.
    private static readonly Regex XmlInvalidChars = new(@"[\x00-\x08\x0B\x0C\x0E-\x1F\x7F]");

    public static bool HasNoXmlInvalidChars(string? value) =>
        value is null || !XmlInvalidChars.IsMatch(value);
}
