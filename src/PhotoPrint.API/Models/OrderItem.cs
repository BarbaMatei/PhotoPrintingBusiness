namespace PhotoPrint.API.Models;

public class OrderItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid OrderId { get; set; }
    public Guid UploadId { get; set; }
    public Guid ProductId { get; set; }

    public int Quantity { get; set; }
    public decimal UnitPriceRon { get; set; }
    public decimal LineTotalRon { get; set; }

    public ProductSnapshot ProductSnapshot { get; set; } = null!;

    // Navigation
    public Order Order { get; set; } = null!;
    public Upload Upload { get; set; } = null!;
    public Product Product { get; set; } = null!;
}
