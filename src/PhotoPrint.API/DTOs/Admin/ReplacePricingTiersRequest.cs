namespace PhotoPrint.API.DTOs.Admin;

public class ReplacePricingTiersRequest
{
    public List<CreatePricingTierRequest> Tiers { get; set; } = [];
}

public class CreatePricingTierRequest
{
    public int MinQuantity { get; set; }
    public int? MaxQuantity { get; set; }
    public decimal UnitPrice { get; set; }
}
