namespace PhotoPrint.API.Models;

public class ProductSize
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ProductId { get; set; }
    public string Label { get; set; } = "";
    public int WidthMm { get; set; }
    public int HeightMm { get; set; }
    public bool IsActive { get; set; } = true;

    public Product Product { get; set; } = null!;
    public ICollection<PricingTier> PricingTiers { get; set; } = [];
}
