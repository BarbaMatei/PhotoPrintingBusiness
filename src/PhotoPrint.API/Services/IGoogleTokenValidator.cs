namespace PhotoPrint.API.Services;

public record GooglePayload(string Sub, string Email, string GivenName, string FamilyName, string? Picture);

public interface IGoogleTokenValidator
{
    Task<GooglePayload> ValidateAsync(string idToken, CancellationToken ct = default);
}
