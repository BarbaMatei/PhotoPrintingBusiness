namespace PhotoPrint.API.Services.Sameday;

/// <summary>
/// Typed-client surface for the Sameday courier API. The
/// anti-corruption seam: every method here returns domain types — vendor
/// JSON shapes never escape this interface.
///
/// Bolt 036 fully implements <see cref="AuthenticateAsync"/>; the other
/// methods are declared (so DI is stable and consumers compile) but throw
/// <see cref="NotImplementedException"/> until bolt 037 lands the AWB /
/// tracking workflows.
/// </summary>
public interface ISamedayClient : ISamedayAuthenticator
{
    Task<AwbCreationResult> CreateAwbAsync(AwbCreationRequest request, CancellationToken ct = default);
    Task<Stream> GetLabelPdfAsync(string awbNumber, CancellationToken ct = default);
    Task<TrackingSnapshot> GetTrackingAsync(string awbNumber, CancellationToken ct = default);
}
