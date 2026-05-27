using PhotoPrint.API.Models;

namespace PhotoPrint.API.Services;

public interface IEuPlatescService
{
    /// <summary>
    /// Builds the EuPlatesc redirect URL (with HMAC fingerprint) for the given order.
    /// </summary>
    string BuildInitiateUrl(Order order);

    /// <summary>
    /// Issues a full refund for the given EuPlatesc transaction.
    /// </summary>
    Task RefundAsync(string transactionId, decimal amount, CancellationToken ct = default);
}
