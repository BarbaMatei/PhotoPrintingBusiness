namespace PhotoPrint.API.Configuration;

public class EuPlatescSettings
{
    public const string SectionName = "EuPlatesc";

    public string MerchantId { get; set; } = "";
    public string SecretKey { get; set; } = "";
    public string GatewayUrl { get; set; } = "https://secure.euplatesc.ro/tdsprocess/tranzactd.php";
}
