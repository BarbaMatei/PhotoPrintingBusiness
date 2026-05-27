namespace PhotoPrint.API.DTOs.Products;

public class PriceCalculationResponse
{
    public Guid SizeId { get; set; }
    public string SizeLabel { get; set; } = "";
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalPrice { get; set; }
    public string TierLabel { get; set; } = "";
    public string Currency { get; set; } = "RON";
}
