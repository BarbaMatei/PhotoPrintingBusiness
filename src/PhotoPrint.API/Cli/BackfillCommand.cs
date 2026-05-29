using Microsoft.EntityFrameworkCore;
using PhotoPrint.API.Data;
using PhotoPrint.API.Models;
using PhotoPrint.API.Services;

namespace PhotoPrint.API.Cli;

/// <summary>
/// One-off ops verb: promote pre-existing paid orders whose photos are still on local disk
/// (story 004; supersedes intent-019 story 003). Same code path as the live worker — reuses
/// <see cref="IOrderPhotoPromoter"/> directly, no parallel implementation.
/// </summary>
/// <remarks>
/// Usage: <c>dotnet run --project src/PhotoPrint.API -- backfill-archive [--dry-run]</c>
/// </remarks>
public static class BackfillCommand
{
    public const string Verb = "backfill-archive";

    /// <summary>
    /// Runs the backfill against the already-built host's services. Returns a process
    /// exit code (0 = ok, 1 = any per-order failure, 2 = cloud tier off).
    /// </summary>
    public static async Task<int> RunAsync(IServiceProvider services, string[] args, CancellationToken ct)
    {
        var dryRun = args.Contains("--dry-run", StringComparer.OrdinalIgnoreCase);

        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PhotoPrintDbContext>();
        var router = scope.ServiceProvider.GetRequiredService<IStorageRouter>();
        var promoter = scope.ServiceProvider.GetRequiredService<IOrderPhotoPromoter>();

        if (!router.CloudEnabled)
        {
            Console.Error.WriteLine(
                "backfill-archive: cloud tier is disabled (Storage:Provider=Local). " +
                "Configure Storage:Provider=S3 with credentials before running.");
            return 2;
        }

        // Same filter as PromotionRecoveryScanner — single source of truth for "needs promoting."
        var orderIds = await db.Orders
            .Where(o => o.Status == OrderStatus.Paid ||
                        o.Status == OrderStatus.Printing ||
                        o.Status == OrderStatus.Shipped ||
                        o.Status == OrderStatus.Delivered)
            .Where(o => o.Items.Any(i => i.Upload.StorageLocation == StorageLocation.Local))
            .OrderBy(o => o.PaidAt)
            .Select(o => new { o.Id, o.OrderNumber, UploadCount = o.Items.Count })
            .ToListAsync(ct);

        Console.WriteLine($"backfill-archive: found {orderIds.Count} order(s) with Local uploads " +
                          $"(mode={(dryRun ? "dry-run" : "live")})");

        if (orderIds.Count == 0)
            return 0;

        if (dryRun)
        {
            foreach (var o in orderIds)
                Console.WriteLine($"  would promote: {o.OrderNumber} ({o.Id}) — {o.UploadCount} item(s)");
            Console.WriteLine("backfill-archive: dry run complete — no changes made.");
            return 0;
        }

        var totalPromoted = 0;
        var totalSkipped = 0;
        var totalFailed = 0;
        long totalBytes = 0;

        foreach (var o in orderIds)
        {
            if (ct.IsCancellationRequested)
            {
                Console.WriteLine("backfill-archive: cancellation requested — stopping cleanly.");
                break;
            }

            try
            {
                // Each order gets its own scope so the DbContext is fresh — long-running
                // change tracking across many orders would balloon memory.
                using var orderScope = services.CreateScope();
                var orderPromoter = orderScope.ServiceProvider.GetRequiredService<IOrderPhotoPromoter>();
                var outcome = await orderPromoter.PromoteOrderAsync(o.Id, ct);

                totalPromoted += outcome.Promoted;
                totalSkipped += outcome.Skipped;
                totalFailed += outcome.Failed;
                totalBytes += outcome.TotalBytes;

                Console.WriteLine(
                    $"  {o.OrderNumber}: promoted={outcome.Promoted} " +
                    $"skipped={outcome.Skipped} failed={outcome.Failed} bytes={outcome.TotalBytes}");
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"  {o.OrderNumber}: ERROR — {ex.GetType().Name}: {ex.Message}");
                totalFailed += 1;
            }
        }

        var totalMb = totalBytes / 1_048_576.0;
        Console.WriteLine(
            $"backfill-archive: promoted={totalPromoted} skipped={totalSkipped} " +
            $"failed={totalFailed} total_mb={totalMb:F2}");

        return totalFailed > 0 ? 1 : 0;
    }
}
