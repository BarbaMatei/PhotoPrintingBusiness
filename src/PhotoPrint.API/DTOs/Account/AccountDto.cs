namespace PhotoPrint.API.DTOs.Account;

public record AccountDto(
    string FirstName,
    string LastName,
    string Email,
    string? Phone,
    bool HasPassword,
    List<string> LinkedProviders,
    bool DeletionRequested
);
