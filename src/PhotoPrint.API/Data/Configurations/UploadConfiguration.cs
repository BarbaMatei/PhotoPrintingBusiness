using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PhotoPrint.API.Models;

namespace PhotoPrint.API.Data.Configurations;

public class UploadConfiguration : IEntityTypeConfiguration<Upload>
{
    public void Configure(EntityTypeBuilder<Upload> builder)
    {
        builder.ToTable("Uploads");

        builder.HasKey(u => u.Id);

        // FilePath was non-nullable through bolt 042; bolt 052 makes it nullable so the
        // original-purge can flip it to null without losing other Upload metadata. The
        // single-source-of-truth rule (mirror direction) treats FilePath == null
        // as "original blob no longer exists."
        builder.Property(u => u.FilePath)
            .HasMaxLength(512);

        // Nullable cached-thumbnail path — same length budget as FilePath.
        builder.Property(u => u.ThumbnailPath)
            .HasMaxLength(512);

        // Nullable large-preview path — populated post-promotion; same length budget.
        builder.Property(u => u.LargePreviewPath)
            .HasMaxLength(512);

        // Nullable purge timestamp (bolt 051; written by unit-002 purge job).
        builder.Property(u => u.OriginalPurgedAt);

        // Two-tier storage location. Stored as int; defaults to 0 (Local).
        builder.Property(u => u.StorageLocation)
            .IsRequired()
            .HasDefaultValue(StorageLocation.Local);

        builder.Property(u => u.OriginalFileName)
            .IsRequired()
            .HasMaxLength(260);

        builder.Property(u => u.ContentType)
            .IsRequired()
            .HasMaxLength(100);

        builder.HasIndex(u => u.UserId);
        builder.HasIndex(u => u.GuestSessionId);
        builder.HasIndex(u => u.UploadedAt);

        // Soft-delete: only active uploads are visible by default
        builder.HasIndex(u => u.DeletedAt);

        // FK to Users — nullable; cascade set-null if user is deleted
        builder.HasOne(u => u.User)
            .WithMany()
            .HasForeignKey(u => u.UserId)
            .OnDelete(DeleteBehavior.SetNull);

        // GuestSessionId is stored as a plain column — no FK to GuestSessions
        // (guest sessions can be cleaned up independently without cascading)
        builder.Property(u => u.GuestSessionId);

        // Note: the CK_Uploads_OneOwner check constraint is added conditionally
        // in PhotoPrintDbContext.OnModelCreating (not here) so that InMemory
        // provider used in tests does not encounter relational-only APIs.
    }
}
