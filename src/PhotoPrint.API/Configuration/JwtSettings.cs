namespace PhotoPrint.API.Configuration;

public sealed class JwtSettings
{
    public string PrivateKeyPem { get; init; } = "";
    public string Issuer { get; init; } = "fototipar";
    public string Audience { get; init; } = "fototipar-spa";
    public int AccessTokenMinutes { get; init; } = 15;
    public int RefreshTokenDays { get; init; } = 30;
}
