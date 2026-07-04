using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using PhotoPrint.API.Data.Configurations;
using PhotoPrint.API.Models;
using System.Text.Json;

namespace PhotoPrint.API.Data;

public class PhotoPrintDbContext : DbContext
{
    /// <summary>
    /// Name of the unique index enforcing at-most-one order per non-null Idempotency-Key.
    /// Shared with <c>OrderService.IsIdempotencyKeyViolation</c> (the Postgres
    /// <c>ConstraintName</c> match) so a rename here is a compile break there, not a silent
    /// detection regression that degrades the canonical double-submit to a 500 (BUG-1, v8).
    /// </summary>
    public const string IdempotencyKeyIndexName = "ix_orders_idempotency_key";

    public PhotoPrintDbContext(DbContextOptions<PhotoPrintDbContext> options)
        : base(options)
    {
    }

    public DbSet<EmailQueue> EmailQueue { get; set; } = null!;
    public DbSet<User> Users { get; set; } = null!;
    public DbSet<RefreshToken> RefreshTokens { get; set; } = null!;
    public DbSet<EmailConfirmationToken> EmailConfirmationTokens { get; set; } = null!;
    public DbSet<PasswordResetToken> PasswordResetTokens { get; set; } = null!;
    public DbSet<ExternalLogin> ExternalLogins { get; set; } = null!;
    public DbSet<GuestSession> GuestSessions { get; set; } = null!;
    public DbSet<Product> Products { get; set; } = null!;
    public DbSet<ProductSize> ProductSizes { get; set; } = null!;
    public DbSet<ProductFinish> ProductFinishes { get; set; } = null!;
    public DbSet<PricingTier> PricingTiers { get; set; } = null!;
    public DbSet<Upload> Uploads { get; set; } = null!;
    public DbSet<CartItem> CartItems { get; set; } = null!;
    public DbSet<EasyboxLocker> EasyboxLockers { get; set; } = null!;
    public DbSet<Order> Orders { get; set; } = null!;
    public DbSet<OrderItem> OrderItems { get; set; } = null!;
    public DbSet<SavedAddress> SavedAddresses { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // SQLite doesn't support DateTimeOffset natively — store as Unix ms (long)
        // so that range comparisons (<=, >=) translate correctly in LINQ queries.
        if (Database.ProviderName == DbProviders.Sqlite)
        {
            var dtConverter = new Microsoft.EntityFrameworkCore.Storage.ValueConversion.ValueConverter<DateTimeOffset, long>(
                v => v.ToUnixTimeMilliseconds(),
                v => DateTimeOffset.FromUnixTimeMilliseconds(v));
            var dtNullConverter = new Microsoft.EntityFrameworkCore.Storage.ValueConversion.ValueConverter<DateTimeOffset?, long?>(
                v => v == null ? (long?)null : v.Value.ToUnixTimeMilliseconds(),
                v => v == null ? (DateTimeOffset?)null : DateTimeOffset.FromUnixTimeMilliseconds(v.Value));

            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                foreach (var property in entityType.GetProperties())
                {
                    if (property.ClrType == typeof(DateTimeOffset))
                        property.SetValueConverter(dtConverter);
                    else if (property.ClrType == typeof(DateTimeOffset?))
                        property.SetValueConverter(dtNullConverter);
                }
            }
        }
        modelBuilder.Entity<EmailQueue>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.HtmlBody).HasColumnType("text");
            entity.Property(e => e.LastError).HasColumnType("text");
            entity.Property(e => e.Status).HasConversion<string>();
            entity.HasIndex(e => new { e.Status, e.NextRetryAt })
                  .HasDatabaseName("ix_email_queue_status_next_retry");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(u => u.Id);
            entity.HasIndex(u => u.NormalizedEmail)
                  .IsUnique()
                  .HasDatabaseName("ix_users_normalized_email");
            entity.Property(u => u.Email).HasMaxLength(256);
            entity.Property(u => u.NormalizedEmail).HasMaxLength(256);
            entity.Property(u => u.FirstName).HasMaxLength(100);
            entity.Property(u => u.LastName).HasMaxLength(100);
            entity.Property(u => u.Phone).HasMaxLength(20);
            entity.Property(u => u.Role).HasConversion<string>();
        });

        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.HasKey(rt => rt.Id);
            entity.HasIndex(rt => rt.TokenHash)
                  .IsUnique()
                  .HasDatabaseName("ix_refresh_tokens_hash");
            entity.HasOne(rt => rt.User)
                  .WithMany()
                  .HasForeignKey(rt => rt.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.Property(rt => rt.TokenHash).HasMaxLength(128);
        });

        modelBuilder.Entity<EmailConfirmationToken>(entity =>
        {
            entity.HasKey(t => t.Id);
            entity.HasIndex(t => t.UserId)
                  .HasDatabaseName("ix_email_confirmation_tokens_user_id");
            entity.Property(t => t.TokenHash).HasMaxLength(128);
        });

        modelBuilder.Entity<PasswordResetToken>(entity =>
        {
            entity.HasKey(t => t.Id);
            entity.HasIndex(t => t.UserId)
                  .HasDatabaseName("ix_password_reset_tokens_user_id");
            entity.Property(t => t.TokenHash).HasMaxLength(128);
        });

        modelBuilder.Entity<ExternalLogin>(entity =>
        {
            entity.HasKey(el => el.Id);
            entity.HasIndex(el => new { el.Provider, el.ProviderKey })
                  .IsUnique()
                  .HasDatabaseName("ix_external_logins_provider_key");
            entity.HasIndex(el => new { el.UserId, el.Provider })
                  .IsUnique()
                  .HasDatabaseName("ix_external_logins_user_provider");
            entity.Property(el => el.Provider).HasMaxLength(50);
            entity.Property(el => el.ProviderKey).HasMaxLength(256);
            entity.HasOne(el => el.User)
                  .WithMany()
                  .HasForeignKey(el => el.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<GuestSession>(entity =>
        {
            entity.HasKey(gs => gs.Id);
            entity.Property(gs => gs.Email).HasMaxLength(256);
            entity.Property(gs => gs.FirstName).HasMaxLength(100);
            entity.Property(gs => gs.LastName).HasMaxLength(100);
            entity.Property(gs => gs.Phone).HasMaxLength(20);
            entity.HasIndex(gs => gs.ExpiresAt)
                  .HasDatabaseName("ix_guest_sessions_expires_at");
            entity.HasIndex(gs => gs.ClaimedByUserId)
                  .HasDatabaseName("ix_guest_sessions_claimed_by_user");
            entity.HasOne<User>()
                  .WithMany()
                  .HasForeignKey(gs => gs.ClaimedByUserId)
                  .OnDelete(DeleteBehavior.SetNull)
                  .IsRequired(false);
        });

        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(p => p.Id);
            entity.Property(p => p.Name).HasMaxLength(200);
            entity.Property(p => p.ProductType).HasMaxLength(50);
            entity.Property(p => p.ImageUrl).HasMaxLength(500);
            entity.HasIndex(p => new { p.IsActive, p.SortOrder })
                  .HasDatabaseName("ix_products_is_active_sort_order");
        });

        modelBuilder.Entity<ProductSize>(entity =>
        {
            entity.HasKey(ps => ps.Id);
            entity.Property(ps => ps.Label).HasMaxLength(50);
            entity.HasIndex(ps => ps.ProductId)
                  .HasDatabaseName("ix_product_sizes_product_id");
            entity.HasIndex(ps => new { ps.ProductId, ps.Label })
                  .IsUnique()
                  .HasDatabaseName("ix_product_sizes_product_id_label");
            entity.HasOne(ps => ps.Product)
                  .WithMany(p => p.Sizes)
                  .HasForeignKey(ps => ps.ProductId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ProductFinish>(entity =>
        {
            entity.HasKey(pf => pf.Id);
            entity.Property(pf => pf.Name).HasMaxLength(50);
            entity.HasIndex(pf => pf.ProductId)
                  .HasDatabaseName("ix_product_finishes_product_id");
            entity.HasOne(pf => pf.Product)
                  .WithMany(p => p.Finishes)
                  .HasForeignKey(pf => pf.ProductId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PricingTier>(entity =>
        {
            entity.HasKey(pt => pt.Id);
            if (Database.ProviderName != DbProviders.Sqlite)
                entity.Property(pt => pt.UnitPrice).HasColumnType("decimal(10,2)");
            entity.HasIndex(pt => pt.ProductSizeId)
                  .HasDatabaseName("ix_pricing_tiers_product_size_id");
            entity.HasOne(pt => pt.ProductSize)
                  .WithMany(ps => ps.PricingTiers)
                  .HasForeignKey(pt => pt.ProductSizeId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.ApplyConfiguration(new UploadConfiguration());

        modelBuilder.Entity<CartItem>(entity =>
        {
            entity.HasKey(ci => ci.Id);
            entity.Property(ci => ci.Quantity);

            // Composite unique indexes prevent a user/guest adding the same photo twice.
            // PostgreSQL treats NULLs as distinct in unique indexes, so these are safe.
            entity.HasIndex(ci => new { ci.UserId, ci.UploadId })
                  .IsUnique()
                  .HasDatabaseName("ix_cart_items_user_upload");
            entity.HasIndex(ci => new { ci.GuestSessionId, ci.UploadId })
                  .IsUnique()
                  .HasDatabaseName("ix_cart_items_guest_upload");

            // Index for fast cart retrieval
            entity.HasIndex(ci => new { ci.UserId, ci.AddedAt })
                  .HasDatabaseName("ix_cart_items_user_added_at");
            entity.HasIndex(ci => new { ci.GuestSessionId, ci.AddedAt })
                  .HasDatabaseName("ix_cart_items_guest_added_at");

            entity.HasOne(ci => ci.User)
                  .WithMany()
                  .HasForeignKey(ci => ci.UserId)
                  .OnDelete(DeleteBehavior.Cascade)
                  .IsRequired(false);

            entity.HasOne(ci => ci.Upload)
                  .WithMany()
                  .HasForeignKey(ci => ci.UploadId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(ci => ci.Product)
                  .WithMany()
                  .HasForeignKey(ci => ci.ProductId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // Check constraint: exactly one of UserId / GuestSessionId must be set.
        // Skipped for InMemory provider (used in tests) — relational-only API.
        if (Database.ProviderName != DbProviders.InMemory)
        {
            modelBuilder.Entity<Upload>()
                .ToTable(t => t.HasCheckConstraint(
                    "CK_Uploads_OneOwner",
                    "(\"UserId\" IS NOT NULL AND \"GuestSessionId\" IS NULL) OR " +
                    "(\"UserId\" IS NULL AND \"GuestSessionId\" IS NOT NULL)"));

            modelBuilder.Entity<CartItem>()
                .ToTable(t => t.HasCheckConstraint(
                    "CK_CartItems_OneOwner",
                    "(\"UserId\" IS NOT NULL AND \"GuestSessionId\" IS NULL) OR " +
                    "(\"UserId\" IS NULL AND \"GuestSessionId\" IS NOT NULL)"));
        }

        // ── EasyboxLocker ──────────────────────────────────────────────────────────
        modelBuilder.Entity<EasyboxLocker>(entity =>
        {
            entity.HasKey(l => l.Id);
            entity.Property(l => l.SamedayId).HasMaxLength(50);
            entity.Property(l => l.Name).HasMaxLength(200);
            entity.Property(l => l.Address).HasMaxLength(400);
            entity.Property(l => l.City).HasMaxLength(100);
            entity.Property(l => l.County).HasMaxLength(100);
            entity.HasIndex(l => l.City)
                  .HasDatabaseName("ix_easybox_lockers_city");
        });

        // ── Order ──────────────────────────────────────────────────────────────────
        var jsonOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

        var shippingConverter = new ValueConverter<ShippingAddressSnapshot, string>(
            v => JsonSerializer.Serialize(v, jsonOptions),
            v => JsonSerializer.Deserialize<ShippingAddressSnapshot>(v, jsonOptions)!);

        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasKey(o => o.Id);
            entity.Property(o => o.OrderNumber).HasMaxLength(20);
            entity.HasIndex(o => o.OrderNumber)
                  .IsUnique()
                  .HasDatabaseName("ix_orders_order_number");
            entity.HasIndex(o => new { o.Status, o.CreatedAt })
                  .HasDatabaseName("ix_orders_status_created_at");
            entity.HasIndex(o => o.PaymentIntentId)
                  .HasDatabaseName("ix_orders_payment_intent_id");
            entity.Property(o => o.Status).HasConversion<string>();
            entity.Property(o => o.PaymentProcessor).HasConversion<string>();
            entity.Property(o => o.DeliveryType).HasConversion<string>();
            entity.Property(o => o.PaymentIntentId).HasMaxLength(200);
            entity.Property(o => o.EuPlatescTransactionId).HasMaxLength(200);
            entity.Property(o => o.OrderNumber).HasMaxLength(20);
            entity.Property(o => o.AwbNumber).HasMaxLength(100);
            entity.Property(o => o.TrackingUrl).HasMaxLength(500);

            // ── Idempotency (bolt 035) ──────────────────────────────────────
            entity.Property(o => o.IdempotencyKey).HasMaxLength(80);
            // DB-2 (review 035-v5): 512, not Stripe's exact 255-char ID ceiling. Today's
            // client secrets are ~60–90 chars, but a zero-headroom column throws "value
            // too long" on prod Postgres AFTER the Stripe charge exists if Stripe ever
            // lengthens IDs (SQLite/InMemory don't enforce it, so tests wouldn't catch it).
            entity.Property(o => o.StripeClientSecret).HasMaxLength(512);
            entity.Property(o => o.EuPlatescRedirectUrl).HasMaxLength(1000);

            // At most one order may carry any given non-null IdempotencyKey.
            // Both Postgres and SQLite permit multiple NULLs in a unique index, so
            // key-less orders coexist freely (DOC-2). The explicit HasFilter on Postgres
            // documents intent and keeps the index small; SQLite gets a plain unique
            // index (multiple NULLs still permitted).
            var idempotencyIndex = entity.HasIndex(o => o.IdempotencyKey)
                  .IsUnique()
                  .HasDatabaseName(IdempotencyKeyIndexName);
            if (Database.ProviderName == DbProviders.Postgres)
                idempotencyIndex.HasFilter("\"IdempotencyKey\" IS NOT NULL");
            if (Database.ProviderName != DbProviders.Sqlite)
            {
                entity.Property(o => o.ShippingCostRon).HasColumnType("decimal(10,2)");
                entity.Property(o => o.SubtotalRon).HasColumnType("decimal(10,2)");
                entity.Property(o => o.TotalRon).HasColumnType("decimal(10,2)");
            }

            entity.Property(o => o.ShippingAddress)
                  .HasConversion(shippingConverter);
            if (Database.ProviderName == DbProviders.Postgres)
                entity.Property(o => o.ShippingAddress).HasColumnType("jsonb");

            entity.HasOne(o => o.User)
                  .WithMany()
                  .HasForeignKey(o => o.UserId)
                  .OnDelete(DeleteBehavior.SetNull)
                  .IsRequired(false);

            entity.HasOne(o => o.EasyboxLocker)
                  .WithMany()
                  .HasForeignKey(o => o.EasyboxLockerId)
                  .OnDelete(DeleteBehavior.Restrict)
                  .IsRequired(false);
        });

        // ── OrderItem ──────────────────────────────────────────────────────────────
        var productSnapshotConverter = new ValueConverter<ProductSnapshot, string>(
            v => JsonSerializer.Serialize(v, jsonOptions),
            v => JsonSerializer.Deserialize<ProductSnapshot>(v, jsonOptions)!);

        modelBuilder.Entity<OrderItem>(entity =>
        {
            entity.HasKey(oi => oi.Id);
            entity.HasIndex(oi => oi.OrderId)
                  .HasDatabaseName("ix_order_items_order_id");
            if (Database.ProviderName != DbProviders.Sqlite)
            {
                entity.Property(oi => oi.UnitPriceRon).HasColumnType("decimal(10,2)");
                entity.Property(oi => oi.LineTotalRon).HasColumnType("decimal(10,2)");
            }

            entity.Property(oi => oi.ProductSnapshot)
                  .HasConversion(productSnapshotConverter);
            if (Database.ProviderName == DbProviders.Postgres)
                entity.Property(oi => oi.ProductSnapshot).HasColumnType("jsonb");

            entity.HasOne(oi => oi.Order)
                  .WithMany(o => o.Items)
                  .HasForeignKey(oi => oi.OrderId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(oi => oi.Upload)
                  .WithMany()
                  .HasForeignKey(oi => oi.UploadId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(oi => oi.Product)
                  .WithMany()
                  .HasForeignKey(oi => oi.ProductId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // ── SavedAddress ───────────────────────────────────────────────────────────
        modelBuilder.Entity<SavedAddress>(entity =>
        {
            entity.HasKey(sa => sa.Id);
            entity.Property(sa => sa.Label).HasMaxLength(100);
            entity.Property(sa => sa.FullName).HasMaxLength(200);
            entity.Property(sa => sa.Phone).HasMaxLength(20);
            entity.Property(sa => sa.AddressLine).HasMaxLength(400);
            entity.Property(sa => sa.City).HasMaxLength(100);
            entity.Property(sa => sa.County).HasMaxLength(100);
            entity.Property(sa => sa.PostalCode).HasMaxLength(20);
            entity.HasIndex(sa => sa.UserId)
                  .HasDatabaseName("ix_saved_addresses_user_id");
            entity.HasOne(sa => sa.User)
                  .WithMany(u => u.SavedAddresses)
                  .HasForeignKey(sa => sa.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
