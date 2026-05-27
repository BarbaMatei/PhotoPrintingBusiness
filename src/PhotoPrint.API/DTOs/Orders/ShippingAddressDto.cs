namespace PhotoPrint.API.DTOs.Orders;

public record ShippingAddressDto(
    string RecipientName,
    string Street,
    string Number,
    string? Block,
    string City,
    string County,
    string PostalCode,
    string Phone);
