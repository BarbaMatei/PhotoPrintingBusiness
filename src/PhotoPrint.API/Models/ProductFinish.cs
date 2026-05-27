namespace PhotoPrint.API.Models;

public class ProductFinish
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ProductId { get; set; }
    public string Name { get; set; } = "";

    public Product Product { get; set; } = null!;
}
