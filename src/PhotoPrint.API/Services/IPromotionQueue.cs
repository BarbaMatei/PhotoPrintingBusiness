using System.Threading.Channels;

namespace PhotoPrint.API.Services;

/// <summary>
/// In-process queue of pending promotion work. The producers are the two webhook
/// branches in <c>WebhooksController</c>, the <see cref="PromotionRecoveryScanner"/> (at startup
/// and on each periodic sweep), and any future re-enqueue path. The single consumer is
/// <see cref="OrderPhotoPromotionWorker"/>.
/// </summary>
public interface IPromotionQueue
{
    /// <summary>Writes a job to the queue. Throws only if the channel is closed (process shutdown).</summary>
    ValueTask EnqueueAsync(PromotionJob job, CancellationToken ct = default);

    /// <summary>Channel reader exposed to the single worker — never to producers.</summary>
    ChannelReader<PromotionJob> Reader { get; }
}
