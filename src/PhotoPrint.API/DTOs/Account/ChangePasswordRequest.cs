namespace PhotoPrint.API.DTOs.Account;

public record ChangePasswordRequest(
    string CurrentPassword,
    string NewPassword,
    string ConfirmNewPassword
);
