namespace PhotoPrint.API.Models;

public class PricingTier
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ProductSizeId { get; set; }
    public int MinQuantity { get; set; }
    public int? MaxQuantity { get; set; }
    public decimal UnitPrice { get; set; }

    public ProductSize ProductSize { get; set; } = null!;
}
