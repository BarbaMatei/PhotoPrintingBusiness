namespace PhotoPrint.API.Configuration;

public sealed class ForwardedHeadersSettings
{
    public const string SectionName = "ForwardedHeaders";

    public string[] TrustedProxies { get; set; } = [];
}
