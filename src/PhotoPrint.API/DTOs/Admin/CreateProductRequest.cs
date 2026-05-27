namespace PhotoPrint.API.DTOs.Admin;

public class CreateProductRequest
{
    public string Name { get; set; } = "";
    public string ProductType { get; set; } = "PhotoPrint";
    public string? ImageUrl { get; set; }
    public int SortOrder { get; set; }
    public List<CreateProductSizeRequest> Sizes { get; set; } = [];
}
