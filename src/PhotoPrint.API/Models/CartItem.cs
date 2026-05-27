namespace PhotoPrint.API.Models;

public class CartItem
{
    public Guid Id { get; set; } = Guid.NewGuid();

    // Exactly one of UserId / GuestSessionId must be set
    public Guid? UserId { get; set; }
    public Guid? GuestSessionId { get; set; }

    public Guid UploadId { get; set; }
    public Guid ProductId { get; set; }
    public Guid SizeId { get; set; }
    /// <summary>Selected finish name (e.g. "Lucioasă"). Null when product has no finishes.</summary>
    public string? FinishName { get; set; }

    /// <summary>1–100 inclusive.</summary>
    public int Quantity { get; set; }

    public DateTimeOffset AddedAt { get; set; } = DateTimeOffset.UtcNow;

    // Navigation
    public User? User { get; set; }
    public Upload Upload { get; set; } = null!;
    public Product Product { get; set; } = null!;
    public ProductSize Size { get; set; } = null!;
}
