namespace PhotoPrint.API.Configuration;

public sealed class UploadCleanupSettings
{
    public const string SectionName = "UploadCleanup";

    public int OrphanRetentionHours { get; init; } = 24;

    public int ReferencedRetentionDays { get; init; } = 365;
}
