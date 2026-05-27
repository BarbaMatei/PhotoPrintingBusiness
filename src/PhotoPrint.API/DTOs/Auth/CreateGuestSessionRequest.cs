namespace PhotoPrint.API.DTOs.Auth;

public record CreateGuestSessionRequest(
    string FirstName,
    string LastName,
    string Email,
    string Phone);
