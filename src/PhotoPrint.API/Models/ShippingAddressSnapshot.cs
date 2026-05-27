namespace PhotoPrint.API.Models;

public class ShippingAddressSnapshot
{
    public string Street { get; set; } = null!;
    public string Number { get; set; } = null!;
    public string? Block { get; set; }
    public string City { get; set; } = null!;
    public string County { get; set; } = null!;
    public string PostalCode { get; set; } = null!;
    public string RecipientName { get; set; } = null!;
    public string Phone { get; set; } = null!;
}
