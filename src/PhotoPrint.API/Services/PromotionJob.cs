namespace PhotoPrint.API.Services;

/// <summary>
/// One unit of work for the <see cref="OrderPhotoPromotionWorker"/>: promote all
/// <c>StorageLocation = Local</c> uploads of a given order to the cloud tier.
/// <para><see cref="Attempt"/> starts at 1; the worker re-enqueues with <c>Attempt + 1</c>
/// after a failure, capped by <c>OrderPhotoArchive:MaxAttempts</c>. On the next deploy the
/// recovery scan re-enqueues with <see cref="Attempt"/> = 1, giving terminal failures one
/// fresh chance per process lifetime.</para>
/// </summary>
public sealed record PromotionJob(Guid OrderId, int Attempt = 1);
