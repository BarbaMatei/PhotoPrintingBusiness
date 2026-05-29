using System.Threading.Channels;

namespace PhotoPrint.API.Services;

/// <summary>
/// Default <see cref="IPromotionQueue"/> implementation — wraps an unbounded
/// <see cref="Channel{T}"/> (single reader, multi-writer). Crash-safety lives in
/// <see cref="PromotionRecoveryScanner"/>, not here (ADR-010).
/// </summary>
public class PromotionQueue : IPromotionQueue
{
    private readonly Channel<PromotionJob> _channel;

    public PromotionQueue()
    {
        _channel = Channel.CreateUnbounded<PromotionJob>(new UnboundedChannelOptions
        {
            // Single consumer = the worker; enables minor SDK optimisations.
            SingleReader = true,
            SingleWriter = false,
        });
    }

    public ValueTask EnqueueAsync(PromotionJob job, CancellationToken ct = default)
        => _channel.Writer.WriteAsync(job, ct);

    public ChannelReader<PromotionJob> Reader => _channel.Reader;
}
