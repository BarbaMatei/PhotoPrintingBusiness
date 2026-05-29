namespace PhotoPrint.API.Models;

public class Upload
{
    public Guid Id { get; set; } = Guid.NewGuid();

    // Exactly one of UserId / GuestSessionId must be set (enforced by DB check constraint)
    public Guid? UserId { get; set; }
    public Guid? GuestSessionId { get; set; }

    /// <summary>UUID-based path on disk — never derived from OriginalFileName.</summary>
    public string FilePath { get; set; } = "";

    /// <summary>Cached thumbnail storage path; null until the first preview generates it (bolt 042).</summary>
    public string? ThumbnailPath { get; set; }

    /// <summary>
    /// ~2000 px large web preview key (bolt 051). Null while the upload is on the Local tier;
    /// populated by <see cref="Services.IOrderPhotoPromoter"/> when an order is paid and its
    /// photos are promoted to cloud. The customer-facing full-view representation in the
    /// order history (intent 024, unit 003).
    /// </summary>
    public string? LargePreviewPath { get; set; }

    /// <summary>
    /// Timestamp the original photo was purged from cloud storage post-printing (intent 024,
    /// unit 002). Null while the original is still retained; set when the unit-002 purge job
    /// has deleted the bytes at <see cref="FilePath"/>. Independent of retention cleanup —
    /// only the original is purged on shipped; the thumbnail + large preview stay until the
    /// retention window expires.
    /// </summary>
    public DateTimeOffset? OriginalPurgedAt { get; set; }

    /// <summary>
    /// Which storage tier currently holds this upload's bytes (bolt 043 — two-tier model).
    /// New uploads start <see cref="Models.StorageLocation.Local"/>; the intent-024 promoter
    /// flips this to <see cref="Models.StorageLocation.Cloud"/> after a paid order's photos
    /// are written to cloud and the cloud writes are confirmed.
    /// </summary>
    public StorageLocation StorageLocation { get; set; } = StorageLocation.Local;

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
