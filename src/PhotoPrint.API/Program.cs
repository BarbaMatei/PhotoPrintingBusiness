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

// ── Local developer overrides (untracked; holds the dev JWT signing key) ──────
// appsettings.{Environment}.Local.json is gitignored. Loaded last so it wins.
builder.Configuration.AddJsonFile(
    $"appsettings.{builder.Environment.EnvironmentName}.Local.json",
    optional: true, reloadOnChange: true);

// ── Logging ──────────────────────────────────────────────────────────────────
builder.AddSerilogLogging();

// ── Error tracking (Sentry, intent 020 bolt 045) ─────────────────────────────
// Master flag mirrors the Sameday two-stage rollout: Enabled=false → SDK never
// constructed, boot is byte-identical to baseline. The DSN never lives in
// appsettings.json — provide via user-secrets/env vars.
builder.Services.Configure<PhotoPrint.API.Configuration.SentrySettings>(
    builder.Configuration.GetSection(PhotoPrint.API.Configuration.SentrySettings.SectionName));
builder.Services.AddSingleton<
    Microsoft.Extensions.Options.IValidateOptions<PhotoPrint.API.Configuration.SentrySettings>,
    PhotoPrint.API.Validators.SentrySettingsValidator>();
builder.Services
    .AddOptions<PhotoPrint.API.Configuration.SentrySettings>()
    .ValidateOnStart();

var sentryEnabled = builder.Configuration
    .GetSection(PhotoPrint.API.Configuration.SentrySettings.SectionName)
    .GetValue<bool>("Enabled");

if (sentryEnabled)
{
    var sentryConfig = builder.Configuration
        .GetSection(PhotoPrint.API.Configuration.SentrySettings.SectionName)
        .Get<PhotoPrint.API.Configuration.SentrySettings>()!;

    builder.WebHost.UseSentry(o =>
    {
        o.Dsn              = sentryConfig.Dsn;
        o.Environment      = sentryConfig.Environment ?? builder.Environment.EnvironmentName;
        o.Release          = sentryConfig.Release ?? Environment.GetEnvironmentVariable("GIT_COMMIT_SHA");
        o.SampleRate       = (float)sentryConfig.SampleRate;
        o.TracesSampleRate = sentryConfig.TracesSampleRate;
        o.SendDefaultPii   = false;
        o.Debug            = sentryConfig.Debug;
        PhotoPrint.API.Configuration.SentryDataScrubbers.Register(o);
    });

    builder.Services.AddScoped<PhotoPrint.API.Middleware.SentryScopeEnricherMiddleware>();
}

// ── Observability (OTel traces + Prometheus metrics) ─────────────────────────
// Enabled=false wires nothing. When on, /metrics is gated by an IP allow-list and
// traces go to OTLP; without an endpoint they go nowhere outside Development.
builder.Services.AddObservability(builder.Configuration, builder.Environment);

var observabilityEnabled = builder.Configuration
    .GetSection(PhotoPrint.API.Configuration.ObservabilitySettings.SectionName)
    .GetValue<bool>("Enabled");
var metricsPath = builder.Configuration
    .GetSection(PhotoPrint.API.Configuration.ObservabilitySettings.SectionName)
    .GetValue<string>("Metrics:PrometheusEndpoint") ?? "/metrics";

// ── Database ─────────────────────────────────────────────────────────────────
var dbProvider = builder.Configuration["DatabaseProvider"] ?? "Postgres";
builder.Services.AddDbContext<PhotoPrintDbContext>(options =>
{
    var connStr = builder.Configuration.GetConnectionString("Default");
    // Default to split queries so multi-collection Includes don't trigger a cartesian
    // explosion (and silence the MultipleCollectionInclude warning). No effect on the
    // InMemory provider used in tests.
    // The split-query option is intentionally repeated in both
    // arms — the UseSqlite/UseNpgsql calls differ, so a shared helper would save only the
    // one option line and obscure the provider branch. Not worth extracting.
    if (dbProvider.Equals("Sqlite", StringComparison.OrdinalIgnoreCase))
        options.UseSqlite(connStr, o => o.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery));
    else
        options.UseNpgsql(connStr, o => o.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery));
});

// ── Middleware ────────────────────────────────────────────────────────────────
builder.Services.AddScoped<CorrelationIdMiddleware>();
builder.Services.AddScoped<ExceptionHandlerMiddleware>();
builder.Services.AddSingleton<PhotoPrint.API.Filters.DetectLegacyShippingCostFilter>();
builder.Services.AddScoped<PhotoPrint.API.Filters.IdempotencyKeyFilter>();

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

// ── Photo Upload + Storage (bolt 043: two-tier router + S3 adapter) ───────────
// Cap ImageSharp's largest single allocation as defence-in-depth against decompression
// bombs (bolt 042, story 003 AC#1 /). The per-image pixel-area guard
// (ImageProcessor.ExceedsDecodeLimits) is the primary control; this bounds any decode that
// slips past it — a 2.5 GB bomb allocation throws InvalidMemoryOperationException instead of
// OOM-ing the process. 512 MB sits just above a legitimate max-size (100 MP ≈ 400 MB) decode.
SixLabors.ImageSharp.Configuration.Default.MemoryAllocator =
    SixLabors.ImageSharp.Memory.MemoryAllocator.Create(
        new SixLabors.ImageSharp.Memory.MemoryAllocatorOptions { AllocationLimitMegabytes = 512 });

// Bound concurrent image decodes process-wide (bolt 042, M3/F1). Each
// ~100 MP decode is ~400 MB, so an unbounded burst of concurrent first previews can OOM the
// box even under the per-image caps. Derive the default from both CPU and host memory; ops can
// override via ImageProcessing:MaxConcurrentDecodes.
var maxConcurrentDecodes = builder.Configuration.GetValue<int?>("ImageProcessing:MaxConcurrentDecodes")
    ?? PhotoPrint.API.Services.ImageDecodeLimiter.RecommendedMaxConcurrentDecodes(
           GC.GetGCMemoryInfo().TotalAvailableMemoryBytes,
           Environment.ProcessorCount);
builder.Services.AddSingleton(new PhotoPrint.API.Services.ImageDecodeLimiter(Math.Max(1, maxConcurrentDecodes)));

builder.Services.AddPhotoStorage(builder.Configuration);
builder.Services.AddSingleton<PhotoPrint.API.Services.IMimeValidator, PhotoPrint.API.Services.MimeValidator>();
builder.Services.AddScoped<PhotoPrint.API.Services.IImageProcessor, PhotoPrint.API.Services.ImageProcessor>();
builder.Services.AddScoped<PhotoPrint.API.Services.IUploadService, PhotoPrint.API.Services.UploadService>();

builder.Services
    .AddOptions<PhotoPrint.API.Configuration.UploadCleanupSettings>()
    .Bind(builder.Configuration.GetSection(PhotoPrint.API.Configuration.UploadCleanupSettings.SectionName))
    .Validate(s => s.OrphanRetentionHours    > 0, "UploadCleanup:OrphanRetentionHours must be > 0")
    .Validate(s => s.ReferencedRetentionDays > 0, "UploadCleanup:ReferencedRetentionDays must be > 0")
    .ValidateOnStart();

builder.Services.AddHostedService<PhotoPrint.API.BackgroundJobs.UploadCleanupJob>();

// ── Order Photo Archive (bolt 051: promote-on-paid + recovery scanner) ────────
builder.Services.AddPhotoArchive(builder.Configuration);

// ── Cart ──────────────────────────────────────────────────────────────────────
builder.Services.AddScoped<PhotoPrint.API.Services.ICartService, PhotoPrint.API.Services.CartService>();

// ── Shipping ──────────────────────────────────────────────────────────────────
// Sameday integration (intent 015, bolt 036). The flag is read once at boot:
//   - Sameday:Enabled = false → StaticShippingService (today's behaviour, default).
//   - Sameday:Enabled = true → SamedayShippingService + typed HttpClient + auth
//                                handler. Flipping back to false produces a
//                                byte-identical fallback (intent goal).
builder.Services.AddSingleton<TimeProvider>(_ => TimeProvider.System);

builder.Services.AddSamedayIntegration(builder.Configuration);

builder.Services.AddScoped<PhotoPrint.API.Services.IOrderNumberService, PhotoPrint.API.Services.OrderNumberService>();

// ── Payments ──────────────────────────────────────────────────────────────────
// Payment secrets fail fast in Production (story 006 env-matrix). Validation is
// Production-gated so the Testing host and local dev — which don't configure live
// payment keys — start normally.
var paymentsRequired = builder.Environment.IsProduction();
builder.Services
    .AddOptions<PhotoPrint.API.Configuration.StripeSettings>()
    .Bind(builder.Configuration.GetSection(PhotoPrint.API.Configuration.StripeSettings.SectionName))
    .Validate(s => !paymentsRequired || !string.IsNullOrWhiteSpace(s.SecretKey),
        "Stripe:SecretKey is required in Production.")
    .ValidateOnStart();
builder.Services
    .AddOptions<PhotoPrint.API.Configuration.EuPlatescSettings>()
    .Bind(builder.Configuration.GetSection(PhotoPrint.API.Configuration.EuPlatescSettings.SectionName))
    .Validate(s => !paymentsRequired || (!string.IsNullOrWhiteSpace(s.MerchantId) && !string.IsNullOrWhiteSpace(s.SecretKey)),
        "EuPlatesc:MerchantId and EuPlatesc:SecretKey are required in Production.")
    .ValidateOnStart();
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
else
{
    // ── Postgres (production): apply EF migrations at boot ────────────────────
    // Guarded by IsNpgsql so the Testing host (InMemory) and any non-relational
    // provider are a no-op; only a real PostgreSQL connection triggers migration.
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<PhotoPrintDbContext>();
    if (db.Database.IsNpgsql())
    {
        var migrateLog = scope.ServiceProvider.GetRequiredService<ILogger<PhotoPrintDbContext>>();
        migrateLog.LogInformation("Applying pending EF migrations to PostgreSQL (if any)...");
        db.Database.Migrate();
    }
}

// ── Seed-only mode: dotnet run --seed / dotnet run --seed-dev ──────────────
if (args.Contains("--seed") || args.Contains("--seed-dev"))
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<PhotoPrintDbContext>();
    await PhotoPrint.API.Data.Seed.ProductCatalogSeed.ApplyAsync(db);
    if (args.Contains("--seed-dev"))
        await PhotoPrint.API.Data.Seed.DevDataSeed.ApplyAsync(db);
    return;
}

// ── Backfill archive mode (bolt 051 story 004): one-off ops verb ──────────────
//    dotnet run --project src/PhotoPrint.API -- backfill-archive [--dry-run]
if (args.Contains(PhotoPrint.API.Cli.BackfillCommand.Verb))
{
    var exitCode = await PhotoPrint.API.Cli.BackfillCommand.RunAsync(
        app.Services, args, CancellationToken.None);
    Environment.Exit(exitCode);
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

// ── Static SPA assets (the combined image serves the built Angular app) ───────
// Registered only when wwwroot exists (the production image bundles the SPA there);
// skipped in API-only local dev / tests so StaticFileMiddleware doesn't warn.
if (Directory.Exists(Path.Combine(builder.Environment.ContentRootPath, "wwwroot")))
{
    app.UseDefaultFiles();
    app.UseStaticFiles();
}

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

// Sentry scope enrichment: stamps every event captured during the request with
// correlation_id + user_id. Registered after auth so the user claim is populated;
// the middleware is a no-op when the SDK isn't initialized.
if (sentryEnabled)
    app.UseSentryScopeEnricher();

// ── /metrics endpoint — gated by scrape port + IP allow-list ──────
// Registered conditionally so the endpoint is absent (not just 403) when
// Observability:Enabled=false. The gate middleware runs before the Prometheus
// exporter; wrong listener sees 404, non-allowed IPs see 403 + empty body.
if (observabilityEnabled)
{
    var observabilitySettings = app.Services
        .GetRequiredService<Microsoft.Extensions.Options.IOptions<PhotoPrint.API.Configuration.ObservabilitySettings>>()
        .Value;
    var metricsSettings = observabilitySettings.Metrics;

    if (!ObservabilityExtensions.TracingWired(observabilitySettings, app.Environment))
    {
        app.Logger.LogWarning(
            "observability.tracing.disabled environment={Environment} — no Observability:Otlp:Endpoint, "
            + "so metrics are exported and traces are not; console spans are a Development-only fallback",
            app.Environment.EnvironmentName);
    }

    if (metricsSettings.ScrapePort == 0)
    {
        app.Logger.LogWarning(
            "observability.metrics.scrape_port_unset path={Path} — served on every listener; behind a "
            + "reverse proxy set Observability:Metrics:ScrapePort to a port the edge does not route",
            metricsPath);
    }

    app.UseWhen(
        ctx => ctx.Request.Path.StartsWithSegments(metricsPath, StringComparison.OrdinalIgnoreCase),
        branch => branch.UseMiddleware<PhotoPrint.API.Middleware.MetricsEndpointIpAllowListMiddleware>());
    app.UseOpenTelemetryPrometheusScrapingEndpoint(metricsPath);
}

// Synthetic-throw endpoint — exists only in the "Testing" environment for
// SentryIntegrationTests. Never reachable in Development or Production.
if (app.Environment.IsEnvironment("Testing"))
{
    app.MapGet("/__test/throw",
        () => { throw new InvalidOperationException("synthetic-test-exception"); });
    app.MapGet("/__test/throw-mapped-502",
        () => { throw new PhotoPrint.API.Exceptions.BadGatewayException("synthetic-mapped-502"); });
    app.MapGet("/__test/throw-mapped-404",
        () => { throw new PhotoPrint.API.Exceptions.NotFoundException("synthetic-mapped-404"); });
}

app.MapControllers();
app.MapHub<AdminOrderHub>("/hubs/admin-orders");
app.MapHealthEndpoint();

// SPA fallback — only when the UI was built into wwwroot (production combined
// image). Absent in API-only dev/test, preserving 404s for unknown routes.
var spaIndex = Path.Combine(builder.Environment.ContentRootPath, "wwwroot", "index.html");
if (File.Exists(spaIndex))
    app.MapFallbackToFile("index.html");

app.Run();

// Required for WebApplicationFactory<Program> in integration tests
public partial class Program { }
