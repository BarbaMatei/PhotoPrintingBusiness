namespace PhotoPrint.API.DTOs.Products;

public class ProductDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public string ProductType { get; set; } = "";
    public string? ImageUrl { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; }
    public List<ProductSizeDto> Sizes { get; set; } = [];
    public List<string> Finishes { get; set; } = [];
}

public class ProductSizeDto
{
    public Guid Id { get; set; }
    public string Label { get; set; } = "";
    public int WidthMm { get; set; }
    public int HeightMm { get; set; }
    public bool IsActive { get; set; }
    public List<PricingTierDto> PricingTiers { get; set; } = [];
}

public class PricingTierDto
{
    public int MinQuantity { get; set; }
    public int? MaxQuantity { get; set; }
    public decimal UnitPrice { get; set; }
}
