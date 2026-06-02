namespace PhotoPrint.API.Services.Sameday;

/// <summary>
/// Successful outcome of <c>ISamedayClient.CreateAwbAsync</c>. Returned by
/// bolt 037's AWB workflow — the type is declared here so the
/// <see cref="ISamedayClient"/> interface is stable and bolt 037 does not
/// need to re-touch DI to land the workflow.
/// </summary>
public sealed record AwbCreationResult(string AwbNumber, string LabelUrl, decimal CalculatedPrice);
