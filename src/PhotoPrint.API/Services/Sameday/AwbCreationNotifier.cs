using Microsoft.Extensions.Options;
using PhotoPrint.API.Configuration;

namespace PhotoPrint.API.Services.Sameday;

/// <summary>
/// Live <see cref="IAwbCreationNotifier"/> that enqueues an <see cref="AwbJob"/>
/// onto the in-process channel. Registered only when
/// <c>Sameday:Jobs:Enabled = true</c>; otherwise <see cref="NullAwbCreationNotifier"/>
/// is the bound implementation.
/// </summary>
public sealed class AwbCreationNotifier : IAwbCreationNotifier
{
    private readonly IAwbJobQueue _queue;
    private readonly TimeProvider _clock;
    private readonly ILogger<AwbCreationNotifier> _logger;

    public AwbCreationNotifier(
        IAwbJobQueue queue,
        TimeProvider clock,
        ILogger<AwbCreationNotifier> logger)
    {
        _queue = queue;
        _clock = clock;
        _logger = logger;
    }

    public async Task NotifyPaidAsync(Guid orderId, CancellationToken ct = default)
    {
        var job = new AwbJob(orderId, Attempt: 1, EnqueuedAt: _clock.GetUtcNow());
        await _queue.EnqueueAsync(job, ct);
        _logger.LogInformation("sameday.awb.enqueued order_id={OrderId}", orderId);
    }
}
