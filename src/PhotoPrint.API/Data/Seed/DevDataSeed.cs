using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PhotoPrint.API.Models;

namespace PhotoPrint.API.Data.Seed;

/// <summary>
/// Seeds realistic-looking development data: two regular users + six orders
/// spanning every status. Idempotent — skips if already applied.
/// Run with: dotnet run --seed-dev
/// </summary>
public static class DevDataSeed
{
    // ── Fixed IDs ────────────────────────────────────────────────────────────
    private static readonly Guid User1Id = new("f1000000-0000-0000-0000-000000000001");
    private static readonly Guid User2Id = new("f1000000-0000-0000-0000-000000000002");

    private static readonly Guid Upload1Id = new("a2000000-0000-0000-0000-000000000001");
    private static readonly Guid Upload2Id = new("a2000000-0000-0000-0000-000000000002");
    private static readonly Guid Upload3Id = new("a2000000-0000-0000-0000-000000000003");
    private static readonly Guid Upload4Id = new("a2000000-0000-0000-0000-000000000004");
    private static readonly Guid Upload5Id = new("a2000000-0000-0000-0000-000000000005");
    private static readonly Guid Upload6Id = new("a2000000-0000-0000-0000-000000000006");

    private static readonly Guid Order1Id = new("c2000000-0000-0000-0000-000000000001");
    private static readonly Guid Order2Id = new("c2000000-0000-0000-0000-000000000002");
    private static readonly Guid Order3Id = new("c2000000-0000-0000-0000-000000000003");
    private static readonly Guid Order4Id = new("c2000000-0000-0000-0000-000000000004");
    private static readonly Guid Order5Id = new("c2000000-0000-0000-0000-000000000005");
    private static readonly Guid Order6Id = new("c2000000-0000-0000-0000-000000000006");

    public static async Task ApplyAsync(PhotoPrintDbContext db, CancellationToken ct = default)
    {
        if (await db.Orders.AnyAsync(o => o.Id == Order1Id, ct))
        {
            Console.WriteLine("Dev seed already applied — skipping.");
            return;
        }

        // ── Easybox Lockers ───────────────────────────────────────────────────
        if (!await db.EasyboxLockers.AnyAsync(ct))
        {
            db.EasyboxLockers.AddRange(
                Locker("SMD-001", "easybox Iulius Mall Cluj", "Str. Alexandru Vaida Voevod 53-55", "Cluj-Napoca", "Cluj", 46.7496, 23.5900),
                Locker("SMD-002", "easybox Polus Center",    "Str. Avram Iancu 492-500",          "Cluj-Napoca", "Cluj", 46.7917, 23.6319),
                Locker("SMD-003", "easybox Mega Image Unirii","Piața Unirii 1",                   "București",   "Ilfov", 44.4306, 26.0996),
                Locker("SMD-004", "easybox AFI Cotroceni",   "Bd. Vasile Milea 4",                "București",   "Ilfov", 44.4255, 26.0489),
                Locker("SMD-005", "easybox Sun Plaza",       "Calea Văcărești 391",               "București",   "Ilfov", 44.3982, 26.1152),
                Locker("SMD-006", "easybox Iulius Mall Iași","Str. Palas 7A",                     "Iași",        "Iași",  47.1574, 27.5849),
                Locker("SMD-007", "easybox Bega Mall",       "Str. Torontalului 2",               "Timișoara",   "Timiș", 45.7562, 21.2342),
                Locker("SMD-008", "easybox City Park Mall",  "Bd. Alexandru Lăpușneanu 116C",     "Constanța",   "Constanța", 44.1649, 28.6293)
            );
        }

        // ── Users ─────────────────────────────────────────────────────────────
        if (!await db.Users.AnyAsync(u => u.Id == User1Id, ct))
        {
            var user1 = new User
            {
                Id                  = User1Id,
                Email               = "ion.popescu@example.com",
                NormalizedEmail     = "ION.POPESCU@EXAMPLE.COM",
                FirstName           = "Ion",
                LastName            = "Popescu",
                Role                = UserRole.Customer,
                IsEmailConfirmed    = true,
                GdprConsentAccepted = true,
                CreatedAt           = new DateTimeOffset(2026, 3, 1, 0, 0, 0, TimeSpan.Zero),
            };
            user1.PasswordHash = new PasswordHasher<User>().HashPassword(user1, "Test1234!");
            db.Users.Add(user1);
        }

        if (!await db.Users.AnyAsync(u => u.Id == User2Id, ct))
        {
            var user2 = new User
            {
                Id                  = User2Id,
                Email               = "ana.ionescu@example.com",
                NormalizedEmail     = "ANA.IONESCU@EXAMPLE.COM",
                FirstName           = "Ana",
                LastName            = "Ionescu",
                Role                = UserRole.Customer,
                IsEmailConfirmed    = true,
                GdprConsentAccepted = true,
                CreatedAt           = new DateTimeOffset(2026, 3, 5, 0, 0, 0, TimeSpan.Zero),
            };
            user2.PasswordHash = new PasswordHasher<User>().HashPassword(user2, "Test1234!");
            db.Users.Add(user2);
        }

        // ── Fake uploads (no actual file on disk — for demo only) ─────────────
        db.Uploads.AddRange(
            FakeUpload(Upload1Id, User1Id, "vacanta_2026_001.jpg", 3024, 4032),
            FakeUpload(Upload2Id, User1Id, "vacanta_2026_002.jpg", 4032, 3024),
            FakeUpload(Upload3Id, User1Id, "portret_ana.jpg",      3000, 4000),
            FakeUpload(Upload4Id, User2Id, "familie_craciun.jpg",  5000, 3333),
            FakeUpload(Upload5Id, User2Id, "nunta_2025_001.jpg",   4500, 3000),
            FakeUpload(Upload6Id, User2Id, "nunta_2025_002.jpg",   4500, 3000)
        );

        // ── Shipping address helpers ───────────────────────────────────────────
        var addr1 = new ShippingAddressSnapshot
        {
            RecipientName = "Ion Popescu",
            Phone         = "0722111222",
            Street        = "Strada Florilor",
            Number        = "12",
            City          = "București",
            County        = "Ilfov",
            PostalCode    = "077190",
        };
        var addr2 = new ShippingAddressSnapshot
        {
            RecipientName = "Ana Ionescu",
            Phone         = "0733222333",
            Street        = "Bulevardul Unirii",
            Number        = "45",
            Block         = "Bl. A2, Sc. 1, Ap. 8",
            City          = "Cluj-Napoca",
            County        = "Cluj",
            PostalCode    = "400001",
        };

        // ── Orders ────────────────────────────────────────────────────────────
        var productId = ProductCatalogSeed.ProductId;

        // 1. Paid → being printed
        var o1 = new Order
        {
            Id               = Order1Id,
            OrderNumber      = "FT-2026-0001",
            UserId           = User1Id,
            Status           = OrderStatus.Printing,
            PaymentIntentId  = "pi_dev_0001",
            ShippingAddress  = addr1,
            DeliveryType     = DeliveryType.Courier,
            ShippingCostRon  = 25.00m,
            SubtotalRon      = 36.00m,
            TotalRon         = 61.00m,
            PaidAt           = new DateTimeOffset(2026, 5, 10, 9, 0, 0, TimeSpan.Zero),
            CreatedAt        = new DateTimeOffset(2026, 5, 10, 8, 45, 0, TimeSpan.Zero),
        };
        o1.Items.Add(new OrderItem
        {
            Id              = NewItemId(1),
            OrderId         = Order1Id,
            UploadId        = Upload1Id,
            ProductId       = productId,
            Quantity        = 30,
            UnitPriceRon    = 0.90m,
            LineTotalRon    = 27.00m,
            ProductSnapshot = Snap("Poze foto", "10×15", "Lucioasă"),
        });
        o1.Items.Add(new OrderItem
        {
            Id              = NewItemId(2),
            OrderId         = Order1Id,
            UploadId        = Upload2Id,
            ProductId       = productId,
            Quantity        = 5,
            UnitPriceRon    = 1.80m,
            LineTotalRon    = 9.00m,
            ProductSnapshot = Snap("Poze foto", "13×18", "Mată"),
        });
        db.Orders.Add(o1);

        // 2. Shipped
        var o2 = new Order
        {
            Id               = Order2Id,
            OrderNumber      = "FT-2026-0002",
            UserId           = User2Id,
            Status           = OrderStatus.Shipped,
            PaymentIntentId  = "pi_dev_0002",
            ShippingAddress  = addr2,
            DeliveryType     = DeliveryType.Courier,
            ShippingCostRon  = 25.00m,
            SubtotalRon      = 90.00m,
            TotalRon         = 115.00m,
            AwbNumber        = "RO123456789",
            TrackingUrl      = "https://sameday.ro/tracking/RO123456789",
            PaidAt           = new DateTimeOffset(2026, 5, 8, 14, 0, 0, TimeSpan.Zero),
            CreatedAt        = new DateTimeOffset(2026, 5, 8, 13, 30, 0, TimeSpan.Zero),
        };
        o2.Items.Add(new OrderItem
        {
            Id              = NewItemId(3),
            OrderId         = Order2Id,
            UploadId        = Upload4Id,
            ProductId       = productId,
            Quantity        = 50,
            UnitPriceRon    = 1.80m,
            LineTotalRon    = 90.00m,
            ProductSnapshot = Snap("Poze foto", "13×18", "Lucioasă"),
        });
        db.Orders.Add(o2);

        // 3. Delivered
        var o3 = new Order
        {
            Id               = Order3Id,
            OrderNumber      = "FT-2026-0003",
            UserId           = User1Id,
            Status           = OrderStatus.Delivered,
            PaymentIntentId  = "pi_dev_0003",
            ShippingAddress  = addr1,
            DeliveryType     = DeliveryType.Easybox,
            ShippingCostRon  = 20.00m,
            SubtotalRon      = 22.00m,
            TotalRon         = 42.00m,
            PaidAt           = new DateTimeOffset(2026, 4, 20, 10, 0, 0, TimeSpan.Zero),
            CreatedAt        = new DateTimeOffset(2026, 4, 20, 9, 50, 0, TimeSpan.Zero),
        };
        o3.Items.Add(new OrderItem
        {
            Id              = NewItemId(4),
            OrderId         = Order3Id,
            UploadId        = Upload3Id,
            ProductId       = productId,
            Quantity        = 10,
            UnitPriceRon    = 2.20m,
            LineTotalRon    = 22.00m,
            ProductSnapshot = Snap("Poze foto", "15×21", "Lucioasă"),
        });
        db.Orders.Add(o3);

        // 4. Awaiting payment (abandoned)
        var o4 = new Order
        {
            Id               = Order4Id,
            OrderNumber      = "FT-2026-0004",
            UserId           = User2Id,
            Status           = OrderStatus.AwaitingPayment,
            ShippingAddress  = addr2,
            DeliveryType     = DeliveryType.Courier,
            ShippingCostRon  = 25.00m,
            SubtotalRon      = 55.00m,
            TotalRon         = 80.00m,
            CreatedAt        = new DateTimeOffset(2026, 5, 20, 18, 0, 0, TimeSpan.Zero),
        };
        o4.Items.Add(new OrderItem
        {
            Id              = NewItemId(5),
            OrderId         = Order4Id,
            UploadId        = Upload5Id,
            ProductId       = productId,
            Quantity        = 10,
            UnitPriceRon    = 5.50m,
            LineTotalRon    = 55.00m,
            ProductSnapshot = Snap("Poze foto", "A3", "Mată"),
        });
        db.Orders.Add(o4);

        // 5. Cancelled
        var o5 = new Order
        {
            Id               = Order5Id,
            OrderNumber      = "FT-2026-0005",
            UserId           = User1Id,
            Status           = OrderStatus.Cancelled,
            ShippingAddress  = addr1,
            DeliveryType     = DeliveryType.Courier,
            ShippingCostRon  = 25.00m,
            SubtotalRon      = 12.00m,
            TotalRon         = 37.00m,
            InternalNotes    = "Client a solicitat anularea comanda.",
            CreatedAt        = new DateTimeOffset(2026, 5, 1, 12, 0, 0, TimeSpan.Zero),
        };
        o5.Items.Add(new OrderItem
        {
            Id              = NewItemId(6),
            OrderId         = Order5Id,
            UploadId        = Upload2Id,
            ProductId       = productId,
            Quantity        = 4,
            UnitPriceRon    = 3.00m,
            LineTotalRon    = 12.00m,
            ProductSnapshot = Snap("Poze foto", "A4", "Lucioasă"),
        });
        db.Orders.Add(o5);

        // 6. Recently paid
        var o6 = new Order
        {
            Id               = Order6Id,
            OrderNumber      = "FT-2026-0006",
            UserId           = User2Id,
            Status           = OrderStatus.Paid,
            PaymentIntentId  = "pi_dev_0006",
            ShippingAddress  = addr2,
            DeliveryType     = DeliveryType.Easybox,
            ShippingCostRon  = 20.00m,
            SubtotalRon      = 168.00m,
            TotalRon         = 188.00m,
            PaidAt           = new DateTimeOffset(2026, 5, 22, 8, 0, 0, TimeSpan.Zero),
            CreatedAt        = new DateTimeOffset(2026, 5, 22, 7, 55, 0, TimeSpan.Zero),
        };
        o6.Items.Add(new OrderItem
        {
            Id              = NewItemId(7),
            OrderId         = Order6Id,
            UploadId        = Upload6Id,
            ProductId       = productId,
            Quantity        = 60,
            UnitPriceRon    = 2.80m,
            LineTotalRon    = 168.00m,
            ProductSnapshot = Snap("Poze foto", "20×30", "Lucioasă"),
        });
        db.Orders.Add(o6);

        await db.SaveChangesAsync(ct);
        Console.WriteLine("Dev seed applied successfully — 2 users, 8 lockers, 6 uploads, 6 orders.");
    }

    // ── Helpers ──────────────────────────────────────────────────────────────
    private static EasyboxLocker Locker(string samedayId, string name, string address, string city, string county, double lat, double lng) =>
        new() { Id = Guid.NewGuid(), SamedayId = samedayId, Name = name, Address = address, City = city, County = county, Lat = lat, Lng = lng, IsActive = true };

    private static Upload FakeUpload(Guid id, Guid userId, string fileName, int w, int h) =>
        new()
        {
            Id               = id,
            UserId           = userId,
            FilePath         = $"{userId}/{id:N}.jpg",
            OriginalFileName = fileName,
            ContentType      = "image/jpeg",
            WidthPx          = w,
            HeightPx         = h,
            FileSizeBytes    = w * h / 4, // rough estimate
            UploadedAt       = new DateTimeOffset(2026, 5, 1, 10, 0, 0, TimeSpan.Zero),
        };

    private static ProductSnapshot Snap(string product, string size, string finish) =>
        new() { ProductName = product, Size = size, Finish = finish };

    private static Guid NewItemId(int n) =>
        new($"e2000000-0000-0000-0000-{n:D12}");
}
