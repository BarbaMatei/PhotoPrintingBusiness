using Microsoft.Extensions.Options;
using PhotoPrint.API.Models;

namespace PhotoPrint.API.Configuration;

/// <summary>
/// Configuration for the intent-024 retention lifecycle.
/// <para>The master <see cref="Enabled"/> switch can disable both the original-purge
/// hook and the retention sweep independently of the cloud tier — useful for incident
/// response (drain status changes without firing destructive operations) or a deploy
/// that intentionally skips the retention worker.</para>
/// <para>Retention is measured from <c>Order.PaidAt</c>.</para>
/// </summary>
public class ArchiveSettings
{
    public const string SectionName = "Archive";

    /// <summary>Master switch — false disables purge + retention as a runtime no-op.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>Order status that triggers original-purge. Must be <c>Shipped</c> or <c>Delivered</c>.</summary>
    public string PurgeOriginalAtStatus { get; set; } = "Shipped";

    /// <summary>Retention window from <c>Order.PaidAt</c>. Default 12 months; configurable down to 1.</summary>
    public int RetentionMonths { get; set; } = 12;

    /// <summary>Retention job cadence. Default 6 hours.</summary>
    public int JobIntervalHours { get; set; } = 6;

    /// <summary>
    /// Cadence of the original-purge recovery sweep (<see cref="Services.IOriginalPurger"/> backstop).
    /// Default 6 hours: the retention/GDPR window is measured in months, so an hours-scale backstop
    /// bounds how long a late-completing promotion's original can linger to at most one interval.
    /// </summary>
    public int PurgeSweepIntervalHours { get; set; } = 6;

    /// <summary>Max rows the retention job processes per tick. Default 500.</summary>
    public int BatchSize { get; set; } = 500;

    /// <summary>True iff the given status equals the configured production-complete status.</summary>
    public bool IsProductionCompleteStatus(OrderStatus status)
        => Enum.TryParse<OrderStatus>(PurgeOriginalAtStatus, ignoreCase: true, out var target)
           && status == target;

    /// <summary>
    /// Statuses considered "at or past production-complete" — drives the recovery scanner.
    /// Order enum integers are NOT in lifecycle order (PaymentFailed=5, Cancelled=6 are after
    /// Delivered=4), so we enumerate explicitly rather than using <c>&gt;=</c>.
    /// </summary>
    public OrderStatus[] ProductionCompleteFloor()
        => PurgeOriginalAtStatus.Equals("Delivered", StringComparison.OrdinalIgnoreCase)
            ? [OrderStatus.Delivered]
            : [OrderStatus.Shipped, OrderStatus.Delivered];

    /// <summary>
    /// Statuses whose cloud original is no longer retained and is therefore eligible for the
    /// purge-recovery sweep: the production-complete floor (fulfilled) plus <c>Cancelled</c>
    /// (refunded/aborted — the original is purged on cancel). PaymentFailed
    /// never promoted, so it has no cloud original to sweep.
    /// </summary>
    public OrderStatus[] OriginalPurgeSweepStatuses()
        => [.. ProductionCompleteFloor(), OrderStatus.Cancelled];
}

/// <summary>
/// Fails fast at startup (via <c>.ValidateOnStart</c>) when the retention settings
/// are malformed. Bad values would otherwise lurk until the first sweep / first
/// production-complete transition.
/// </summary>
public class ArchiveSettingsValidator : IValidateOptions<ArchiveSettings>
{
    public ValidateOptionsResult Validate(string? name, ArchiveSettings options)
    {
        var failures = new List<string>();

        if (!Enum.TryParse<OrderStatus>(options.PurgeOriginalAtStatus, ignoreCase: true, out var parsed)
            || (parsed != OrderStatus.Shipped && parsed != OrderStatus.Delivered))
        {
            failures.Add("Archive:PurgeOriginalAtStatus must be 'Shipped' or 'Delivered'.");
        }

        if (options.RetentionMonths <= 0)
            failures.Add("Archive:RetentionMonths must be > 0.");
        if (options.JobIntervalHours <= 0)
            failures.Add("Archive:JobIntervalHours must be > 0.");
        if (options.PurgeSweepIntervalHours <= 0)
            failures.Add("Archive:PurgeSweepIntervalHours must be > 0.");
        if (options.BatchSize <= 0)
            failures.Add("Archive:BatchSize must be > 0.");

        return failures.Count > 0
            ? ValidateOptionsResult.Fail(failures)
            : ValidateOptionsResult.Success;
    }
}
