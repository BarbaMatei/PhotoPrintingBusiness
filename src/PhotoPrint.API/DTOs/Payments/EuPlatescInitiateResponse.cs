namespace PhotoPrint.API.DTOs.Payments;

/// <summary>
/// Returned after successfully initiating an EuPlatesc payment.
/// The client should redirect the browser to <see cref="RedirectUrl"/>.
/// </summary>
public record EuPlatescInitiateResponse(string RedirectUrl, Guid OrderId);
