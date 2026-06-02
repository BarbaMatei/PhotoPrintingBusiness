namespace PhotoPrint.API.DTOs.Orders;

/// <summary>
/// One photo from an order's archive (intent 024, bolt 053). Carried under
/// <see cref="OrderPhotosDto.Photos"/>. The two URLs are presigned cloud URLs with a
/// short TTL (see <see cref="Configuration.StorageSettings.PresignTtlMinutes"/>).
/// </summary>
public record OrderPhotoDto(
    Guid UploadId,
    string FileName,
    string ThumbnailUrl,
    string LargeUrl);

/// <summary>
/// Envelope returned from <c>GET /api/orders/{id}/photos</c>. Empty <see cref="Photos"/>
/// list = the order has no viewable photos (pre-promotion, post-retention, or
/// cloud-tier-off in this deployment).
/// </summary>
public record OrderPhotosDto(IReadOnlyList<OrderPhotoDto> Photos);
