namespace PhotoPrint.API.Models;

public class Upload
{
    public Guid Id { get; set; } = Guid.NewGuid();

    // Exactly one of UserId / GuestSessionId must be set (enforced by DB check constraint)
    public Guid? UserId { get; set; }
    public Guid? GuestSessionId { get; set; }

    /// <summary>UUID-based path on disk — never derived from OriginalFileName.</summary>
    public string FilePath { get; set; } = "";

    /// <summary>Original client filename stored for audit only — never used in storage path.</summary>
    public string OriginalFileName { get; set; } = "";

    /// <summary>MIME type as determined by magic bytes, not client Content-Type.</summary>
    public string ContentType { get; set; } = "";

    public int WidthPx { get; set; }
    public int HeightPx { get; set; }
    public long FileSizeBytes { get; set; }

    public DateTimeOffset UploadedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>Soft-delete timestamp — set only by UploadCleanupJob.</summary>
    public DateTimeOffset? DeletedAt { get; set; }

    // Navigation
    public User? User { get; set; }
}
