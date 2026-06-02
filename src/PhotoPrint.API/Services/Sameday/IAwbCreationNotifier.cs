namespace PhotoPrint.API.Services.Sameday;

/// <summary>
/// Notified by the payment-webhook handlers when an order transitions to
/// <c>Paid</c>. The bolt-036/037 Sameday integration owns the only real
/// implementation; with <c>Sameday:Jobs:Enabled = false</c> the
/// <c>NullAwbCreationNotifier</c> registration silently no-ops so the webhook
/// is decoupled from Sameday-specific lifecycle decisions.
/// </summary>
public interface IAwbCreationNotifier
{
    Task NotifyPaidAsync(Guid orderId, CancellationToken ct = default);
}
