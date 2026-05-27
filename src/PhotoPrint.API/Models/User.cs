namespace PhotoPrint.API.Models;

public class User
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Email { get; set; } = "";
    public string NormalizedEmail { get; set; } = "";
    public string? PasswordHash { get; set; }
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string? Phone { get; set; }
    public UserRole Role { get; set; } = UserRole.Customer;
    public bool IsEmailConfirmed { get; set; }
    public bool GdprConsentAccepted { get; set; }
    public int FailedLoginCount { get; set; }
    public DateTimeOffset? LockoutEnd { get; set; }
    public DateTimeOffset? DeletionRequestedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public ICollection<SavedAddress> SavedAddresses { get; set; } = new List<SavedAddress>();
}

public enum UserRole
{
    Customer,
    Admin,
}
