namespace PhotoPrint.API.DTOs.Account;

public record SavedAddressDto(
    Guid Id,
    string Label,
    string FullName,
    string Phone,
    string AddressLine,
    string City,
    string County,
    string PostalCode,
    bool IsDefault
);
