namespace PhotoPrint.API.DTOs.Admin;

public record UpdateOrderStatusRequest(
    string Status,
    string? AwbNumber,
    string? TrackingUrl);

public record UpdateOrderNotesRequest(string? Notes);

public record CancelOrderRequest(string? Reason);
