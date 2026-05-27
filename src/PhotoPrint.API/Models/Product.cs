namespace PhotoPrint.API.Models;

public class Product
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "";
    public string ProductType { get; set; } = "PhotoPrint";
    public string? ImageUrl { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public ICollection<ProductSize> Sizes { get; set; } = [];
    public ICollection<ProductFinish> Finishes { get; set; } = [];
}
