namespace PhotoPrint.API.DTOs.Account;

public record UpdateAccountRequest(
    string FirstName,
    string LastName,
    string? Phone
);
