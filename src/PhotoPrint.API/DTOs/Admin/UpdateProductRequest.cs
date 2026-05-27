namespace PhotoPrint.API.DTOs.Admin;

public class UpdateProductRequest
{
    public string Name { get; set; } = "";
    public string ProductType { get; set; } = "PhotoPrint";
    public string? ImageUrl { get; set; }
    public int SortOrder { get; set; }
}
