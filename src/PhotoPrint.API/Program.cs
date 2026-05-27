using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using PhotoPrint.API.Configuration;
using PhotoPrint.API.Data;
using PhotoPrint.API.Extensions;
using PhotoPrint.API.Filters;
using PhotoPrint.API.HealthChecks;
using PhotoPrint.API.Hubs;
using PhotoPrint.API.Middleware;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// ── Logging ──────────────────────────────────────────────────────────────────
builder.AddSerilogLogging();

// ── Database ─────────────────────────────────────────────────────────────────
var dbProvider = builder.Configuration["DatabaseProvider"] ?? "Postgres";
builder.Services.AddDbContext<PhotoPrintDbContext>(options =>
{
    var connStr = builder.Configuration.GetConnectionString("Default");
    if (dbProvider.Equals("Sqlite", StringComparison.OrdinalIgnoreCase))
        options.UseSqlite(connStr);
    else
        options.UseNpgsql(connStr);
});

// ── Middleware ────────────────────────────────────────────────────────────────
builder.Services.AddScoped<CorrelationIdMiddleware>();
builder.Services.AddScoped<ExceptionHandlerMiddleware>();
builder.Services.AddSingleton<PhotoPrint.API.Filters.DetectLegacyShippingCostFilter>();

// ── Controllers + Validation ─────────────────────────────────────────────────
builder.Services.AddControllers(options =>
    options.Filters.Add<ValidationFilter>());

builder.Services.Configure<Microsoft.AspNetCore.Mvc.ApiBehaviorOptions>(options =>
    options.SuppressModelStateInvalidFilter = true);

builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<Program>();

// ── Health Checks ─────────────────────────────────────────────────────────────
builder.Services.Configure<HealthCheckSettings>(
    builder.Configuration.GetSection("HealthCheck"));

builder.Services.AddHealthChecks()
    .AddCheck<DbHealthCheck>("database")
    .AddCheck<DiskHealthCheck>("disk");

// ── Security Baselines ───────────────────────────────────────────────────────
builder.Services.AddSecurityBaselines(builder.Configuration);

// ── Email Infrastructure ──────────────────────────────────────────────────────
builder.Services.AddEmailInfrastructure(builder.Configuration);

// ── Auth Core ────────────────────────────────────────────────────────────────
builder.Services.AddAuthCore(builder.Configuration);

// ── Social Auth ───────────────────────────────────────────────────────────────
builder.Services.AddSocialAuth(builder.Configuration);

// ── Guest Sessions ────────────────────────────────────────────────────────────
builder.Services.AddGuestSessions();

// ── Product Catalog ───────────────────────────────────────────────────────────
builder.Services.AddScoped<PhotoPrint.API.Services.PricingService>();
builder.Services.AddScoped<PhotoPrint.API.Services.IProductService, PhotoPrint.API.Services.ProductService>();
builder.Services.AddScoped<PhotoPrint.API.Services.IAdminProductService, PhotoPrint.API.Services.AdminProductService>();

// ── Photo Upload ──────────────────────────────────────────────────────────────
builder.Services.Configure<PhotoPrint.API.Configuration.StorageSettings>(
    builder.Configuration.GetSection(PhotoPrint.API.Configuration.StorageSettings.SectionName));
builder.Services.AddSingleton<PhotoPrint.API.Services.IMimeValidator, PhotoPrint.API.Services.MimeValidator>();
builder.Services.AddScoped<PhotoPrint.API.Services.IStorageService, PhotoPrint.API.Services.LocalStorageService>();
builder.Services.AddScoped<PhotoPrint.API.Services.IImageProcessor, PhotoPrint.API.Services.ImageProcessor>();
builder.Services.AddScoped<PhotoPrint.API.Services.IUploadService, PhotoPrint.API.Services.UploadService>();

builder.Services
    .AddOptions<PhotoPrint.API.Configuration.UploadCleanupSettings>()
    .Bind(builder.Configuration.GetSection(PhotoPrint.API.Configuration.UploadCleanupSettings.SectionName))
    .Validate(s => s.OrphanRetentionHours    > 0, "UploadCleanup:OrphanRetentionHours must be > 0")
    .Validate(s => s.ReferencedRetentionDays > 0, "UploadCleanup:ReferencedRetentionDays must be > 0")
    .ValidateOnStart();

builder.Services.AddHostedService<PhotoPrint.API.BackgroundJobs.UploadCleanupJob>();

// ── Cart ──────────────────────────────────────────────────────────────────────
builder.Services.AddScoped<PhotoPrint.API.Services.ICartService, PhotoPrint.API.Services.CartService>();

// ── Shipping ──────────────────────────────────────────────────────────────────
builder.Services.AddScoped<PhotoPrint.API.Services.IShippingService, PhotoPrint.API.Services.StaticShippingService>();
builder.Services.AddScoped<PhotoPrint.API.Services.IOrderNumberService, PhotoPrint.API.Services.OrderNumberService>();

// ── Payments ──────────────────────────────────────────────────────────────────
builder.Services.Configure<PhotoPrint.API.Configuration.StripeSettings>(
    builder.Configuration.GetSection(PhotoPrint.API.Configuration.StripeSettings.SectionName));
builder.Services.Configure<PhotoPrint.API.Configuration.EuPlatescSettings>(
    builder.Configuration.GetSection(PhotoPrint.API.Configuration.EuPlatescSettings.SectionName));
builder.Services.AddSingleton<Stripe.IStripeClient>(sp =>
{
    var settings = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<PhotoPrint.API.Configuration.StripeSettings>>().Value;
    return new Stripe.StripeClient(settings.SecretKey);
});
builder.Services.AddScoped<PhotoPrint.API.Services.IStripePaymentGateway, PhotoPrint.API.Services.StripePaymentGateway>();
builder.Services.AddScoped<PhotoPrint.API.Services.IStripeSignatureVerifier, PhotoPrint.API.Services.StripeSignatureVerifier>();
builder.Services.AddScoped<PhotoPrint.API.Services.IEuPlatescService, PhotoPrint.API.Services.EuPlatescService>();
builder.Services.AddScoped<PhotoPrint.API.Services.IOrderService, PhotoPrint.API.Services.OrderService>();

// ── Admin ────────────────────────────────────────────────────────────────────────────

builder.Services.AddHttpClient();
builder.Services.AddMemoryCache();
builder.Services.AddSignalR();
builder.Services.AddScoped<PhotoPrint.API.Services.IAdminOrderService, PhotoPrint.API.Services.AdminOrderService>();
builder.Services.AddScoped<PhotoPrint.API.Services.IAdminStatsService, PhotoPrint.API.Services.AdminStatsService>();

// ── Account ───────────────────────────────────────────────────────────────────────────
builder.Services.AddScoped<PhotoPrint.API.Services.IAccountService, PhotoPrint.API.Services.AccountService>();
builder.Services.AddHostedService<PhotoPrint.API.BackgroundJobs.AccountDeletionJob>();

// ── Response Caching ──────────────────────────────────────────────────────────
builder.Services.AddResponseCaching();

var app = builder.Build();

// ── SQLite: auto-create schema (bypasses Postgres-specific migrations) ────────
if (dbProvider.Equals("Sqlite", StringComparison.OrdinalIgnoreCase))
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<PhotoPrintDbContext>();
    var schemaLog = scope.ServiceProvider.GetRequiredService<ILogger<PhotoPrintDbContext>>();

    bool created = db.Database.EnsureCreated();

    if (!created)
    {
        // DB already existed — verify the schema is complete.
        // A previous startup may have created only a subset of tables if EnsureCreated
        // was interrupted; detect this by checking for tables added after the 11 early ones.
        var conn = db.Database.GetDbConnection();
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText =
            "SELECT COUNT(*) FROM sqlite_master WHERE type='table'" +
            " AND name IN ('Uploads','CartItems','Orders','OrderItems','EasyboxLockers')";
        var present = Convert.ToInt64(cmd.ExecuteScalar());
        conn.Close();

        if (present < 5)
        {
            schemaLog.LogWarning(
                "SQLite schema is incomplete ({Present}/5 core tables). " +
                "Dropping and recreating the dev database — all local data will be lost.",
                present);
            db.Database.EnsureDeleted();
            db.Database.EnsureCreated();
        }
    }
}

// ── Seed-only mode: dotnet run --seed  /  dotnet run --seed-dev ──────────────
if (args.Contains("--seed") || args.Contains("--seed-dev"))
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<PhotoPrintDbContext>();
    await PhotoPrint.API.Data.Seed.ProductCatalogSeed.ApplyAsync(db);
    if (args.Contains("--seed-dev"))
        await PhotoPrint.API.Data.Seed.DevDataSeed.ApplyAsync(db);
    return;
}

// ── Promote admin mode: dotnet run -- --promote-admin <email> ─────────────────
var promoteIdx = Array.IndexOf(args, "--promote-admin");
if (promoteIdx >= 0)
{
    var email = promoteIdx + 1 < args.Length ? args[promoteIdx + 1] : null;
    if (string.IsNullOrWhiteSpace(email))
    {
        Console.Error.WriteLine("Usage: dotnet run -- --promote-admin <email>");
        return;
    }
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<PhotoPrintDbContext>();
    var user = await db.Users.FirstOrDefaultAsync(u => u.NormalizedEmail == email.ToUpperInvariant());
    if (user is null)
    {
        Console.Error.WriteLine($"User '{email}' not found.");
        return;
    }
    user.Role = PhotoPrint.API.Models.UserRole.Admin;
    await db.SaveChangesAsync();
    Console.WriteLine($"'{email}' promoted to Admin.");
    return;
}

// ── Middleware Pipeline (ORDER MATTERS) ───────────────────────────────────────
app.UseCorrelationId();          // 1st: stamp correlation ID on every request
app.UseGlobalExceptionHandler(); // 2nd: catch all unhandled exceptions
app.UseSerilogRequestLogging();  // 3rd: structured request log per request

app.UseSecurityBaselines();      // 4th: HSTS, HTTPS, security headers, CORS, rate limiting

app.UseResponseCaching();        // 5th: serve cached responses for catalog endpoints

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<AdminOrderHub>("/hubs/admin-orders");
app.MapHealthEndpoint();

app.Run();

// Required for WebApplicationFactory<Program> in integration tests
public partial class Program { }
