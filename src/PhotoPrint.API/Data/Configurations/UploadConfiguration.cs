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

        builder.Property(u => u.FilePath)
            .IsRequired()
            .HasMaxLength(512);

        // Nullable cached-thumbnail path (bolt 042) — same length budget as FilePath.
        builder.Property(u => u.ThumbnailPath)
            .HasMaxLength(512);

        // Two-tier storage location (bolt 043). Stored as int; defaults to 0 (Local).
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
