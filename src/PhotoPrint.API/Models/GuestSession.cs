namespace PhotoPrint.API.Models;

public class GuestSession
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Email { get; set; } = "";
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string Phone { get; set; } = "";
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset ExpiresAt { get; set; }
    public Guid? ClaimedByUserId { get; set; }

    public bool IsExpired => ExpiresAt < DateTimeOffset.UtcNow;
    public bool IsClaimed => ClaimedByUserId.HasValue;
    public bool IsValid => !IsExpired && !IsClaimed;
}
