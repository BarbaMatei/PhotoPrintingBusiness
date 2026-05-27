namespace PhotoPrint.API.DTOs.Auth;

public record GoogleLoginResponse(string AccessToken, int ExpiresIn, bool AccountLinked);
