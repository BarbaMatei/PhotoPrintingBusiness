namespace PhotoPrint.API.DTOs.Account;

public record SavedAddressRequest(
    string Label,
    string FullName,
    string Phone,
    string AddressLine,
    string City,
    string County,
    string PostalCode,
    bool IsDefault
);
