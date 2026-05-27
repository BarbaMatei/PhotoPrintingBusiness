namespace PhotoPrint.API.DTOs.Auth;

/// <param name="AccessToken">Short-lived JWT (Bearer token).</param>
/// <param name="ExpiresIn">Seconds until the access token expires.</param>
public record LoginResponse(string AccessToken, int ExpiresIn);
