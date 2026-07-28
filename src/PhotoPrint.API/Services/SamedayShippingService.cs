using Microsoft.EntityFrameworkCore;
using PhotoPrint.API.Data;
using PhotoPrint.API.DTOs.Shipping;
using PhotoPrint.API.Exceptions;
using PhotoPrint.API.Services.Sameday;

namespace PhotoPrint.API.Services;

/// <summary>
/// <see cref="IShippingService"/> implementation that delegates to the
/// Sameday HTTP client. Registered only when <c>Sameday:Enabled=true</c>;
/// otherwise <see cref="StaticShippingService"/> remains the default
/// (intent goal: byte-identical fallback).
///
/// In bolt 036, the AWB-creation workflow is NOT wired up — that lives in
/// bolt 037. The locker and shipping-cost paths reuse the existing
/// <see cref="StaticShippingService"/> behaviour via a delegated instance,
/// because the data they rely on (the <c>EasyboxLockers</c> table and
/// <c>Shipping:*</c> config keys) is the same regardless of which courier
/// is wired up.
/// </summary>
public sealed class SamedayShippingService : IShippingService
{
    private readonly ISamedayClient _client;
    private readonly StaticShippingService _staticFallback;
    private readonly ILogger<SamedayShippingService> _logger;

    public SamedayShippingService(
        ISamedayClient client,
        PhotoPrintDbContext db,
        IConfiguration config,
        ILogger<SamedayShippingService> logger)
    {
        _client = client;
        _staticFallback = new StaticShippingService(db, config);
        _logger = logger;
    }

    public Task<IEnumerable<LockerDto>> GetLockersAsync(string city, CancellationToken ct = default)
        => _staticFallback.GetLockersAsync(city, ct);

    public Task<ShippingCostDto> GetShippingCostAsync(string type, CancellationToken ct = default)
        => _staticFallback.GetShippingCostAsync(type, ct);

    public Task<AwbResultDto> GenerateAwbAsync(Guid orderId, CancellationToken ct = default)
    {
        // AWB creation is event-driven — the background jobs create it automatically on the
        // Paid transition. This synchronous endpoint must NOT tell the admin to create one in
        // the Sameday portal, which would double-book alongside the job.
        _logger.LogInformation(
            "sameday.awb.manual-endpoint order_id={OrderId} — AWB is created automatically after payment", orderId);
        return Task.FromResult(new AwbResultDto(
            Manual: false,
            Message: "AWB-ul se generează automat după confirmarea plății."));
    }
}
