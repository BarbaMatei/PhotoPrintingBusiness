namespace PhotoPrint.API.Services.Sameday;

/// <summary>
/// Default no-op <see cref="IAwbCreationNotifier"/> registered whenever
/// the Sameday lifecycle jobs are disabled. Keeps the webhook handlers
/// agnostic about whether the integration is wired up.
/// </summary>
public sealed class NullAwbCreationNotifier : IAwbCreationNotifier
{
    public Task NotifyPaidAsync(Guid orderId, CancellationToken ct = default)
        => Task.CompletedTask;
}
