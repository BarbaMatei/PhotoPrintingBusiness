using System.Threading.Channels;

namespace PhotoPrint.API.Services.Sameday;

/// <summary>
/// Default <see cref="IAwbJobQueue"/> backed by an unbounded
/// <see cref="Channel{T}"/>. One reader (the dispatcher); multiple writers
/// (the webhook hook + the retry job).
/// </summary>
public sealed class AwbJobQueue : IAwbJobQueue
{
    private readonly Channel<AwbJob> _channel = Channel.CreateUnbounded<AwbJob>(
        new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false,
        });

    public ValueTask EnqueueAsync(AwbJob job, CancellationToken ct = default)
        => _channel.Writer.WriteAsync(job, ct);

    public IAsyncEnumerable<AwbJob> DequeueAllAsync(CancellationToken ct)
        => _channel.Reader.ReadAllAsync(ct);
}
