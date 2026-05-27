namespace PhotoPrint.API.DTOs.Admin;

public class CreateProductSizeRequest
{
    public string Label { get; set; } = "";
    public int WidthMm { get; set; }
    public int HeightMm { get; set; }
}
