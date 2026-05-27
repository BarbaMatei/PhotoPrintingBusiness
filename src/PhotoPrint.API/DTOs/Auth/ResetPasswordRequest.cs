namespace PhotoPrint.API.DTOs.Auth;

public record ResetPasswordRequest(Guid UserId, string Token, string NewPassword);
