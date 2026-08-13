using Microsoft.Extensions.Options;

namespace PhotoPrint.API.Configuration;

/// <summary>
/// Configuration for the intent-024 promote-on-paid lifecycle.
/// <para>The master switch <see cref="Enabled"/> can disable promotion independently of
/// the cloud tier — useful for incident response (drain payments without promoting) or
/// a deploy that intentionally skips the worker.</para>
/// </summary>
public class OrderPhotoArchiveSettings
{
    public const string SectionName = "OrderPhotoArchive";

    /// <summary>Master switch — if false, promotion is a no-op regardless of cloud tier state.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>How many orders the worker processes concurrently. Bounded by a SemaphoreSlim.</summary>
    public int MaxConcurrentOrders { get; set; } = 4;

    /// <summary>
    /// Retry ceiling per order. After <see cref="MaxAttempts"/> failures the worker logs
    /// <c>UploadPromotionFailed</c> at Error and stops re-enqueueing; the next deploy's
    /// recovery scan picks it up again.
    /// </summary>
    public int MaxAttempts { get; set; } = 5;

    /// <summary>
    /// Backoff before each re-enqueue, indexed by <c>Attempt - 1</c>. Overflow clamps to the
    /// last value, so a 7-element schedule extends naturally to attempts beyond the array.
    /// </summary>
    public int[] BackoffSeconds { get; set; } = [30, 120, 300, 900, 3600];

    /// <summary>
    /// Cadence of the promotion-recovery sweep (<see cref="BackgroundJobs.PromotionRecoveryScanner"/>).
    /// Default 6 hours: the per-order retry envelope (<see cref="MaxAttempts"/> × backoff) tops out
    /// in ~1.5h, so a 6h re-scan bounds how long a paid order that went terminal-Local waits before
    /// it is re-enqueued for another promotion attempt. Boot-only was insufficient on an always-on
    /// server — its original never reached the durable cloud tier until the next reboot (class sibling of the purge-sweep fix).
    /// </summary>
    public int PromotionRecoverySweepIntervalHours { get; set; } = 6;
}

/// <summary>
/// Fails fast at startup (via <c>.ValidateOnStart</c>) if the settings are malformed —
/// negative concurrency, zero attempts, or an empty backoff schedule.
/// </summary>
public class OrderPhotoArchiveSettingsValidator : IValidateOptions<OrderPhotoArchiveSettings>
{
    public ValidateOptionsResult Validate(string? name, OrderPhotoArchiveSettings options)
    {
        var failures = new List<string>();

        if (options.MaxConcurrentOrders <= 0)
            failures.Add("OrderPhotoArchive:MaxConcurrentOrders must be > 0.");
        if (options.MaxAttempts <= 0)
            failures.Add("OrderPhotoArchive:MaxAttempts must be > 0.");
        if (options.BackoffSeconds is null || options.BackoffSeconds.Length == 0)
            failures.Add("OrderPhotoArchive:BackoffSeconds must contain at least one entry.");
        else if (options.BackoffSeconds.Any(s => s < 0))
            failures.Add("OrderPhotoArchive:BackoffSeconds values must be ≥ 0.");
        if (options.PromotionRecoverySweepIntervalHours <= 0)
            failures.Add("OrderPhotoArchive:PromotionRecoverySweepIntervalHours must be > 0.");

        return failures.Count > 0
            ? ValidateOptionsResult.Fail(failures)
            : ValidateOptionsResult.Success;
    }
}
