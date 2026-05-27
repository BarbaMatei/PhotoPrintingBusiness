namespace PhotoPrint.API.DTOs.Auth;

public record UpdateGuestContactRequest(
    string FirstName,
    string LastName,
    string Email,
    string Phone);
