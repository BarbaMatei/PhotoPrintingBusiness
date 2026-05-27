namespace PhotoPrint.API.DTOs.Auth;

public record RegisterRequest(
    string FirstName,
    string LastName,
    string Email,
    string Password,
    string ConfirmPassword,
    string? Phone,
    bool GdprConsentAccepted);
