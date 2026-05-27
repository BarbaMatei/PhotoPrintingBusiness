using PhotoPrint.API.Data;
using PhotoPrint.API.DTOs.Auth;
using PhotoPrint.API.Exceptions;
using PhotoPrint.API.Models;

namespace PhotoPrint.API.Services;

public class GuestSessionService : IGuestSessionService
{
    private static readonly TimeSpan SessionTtl = TimeSpan.FromDays(7);

    private readonly PhotoPrintDbContext _db;
    private readonly ILogger<GuestSessionService> _logger;

    public GuestSessionService(PhotoPrintDbContext db, ILogger<GuestSessionService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<CreateGuestSessionResponse> CreateAsync(
        CreateGuestSessionRequest request,
        CancellationToken ct = default)
    {
        var session = new GuestSession
        {
            Email = request.Email.ToLowerInvariant(),
            FirstName = request.FirstName,
            LastName = request.LastName,
            Phone = request.Phone,
            ExpiresAt = DateTimeOffset.UtcNow.Add(SessionTtl),
        };

        _db.GuestSessions.Add(session);
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Guest session created: {SessionId} {Email}", session.Id, session.Email);

        return new CreateGuestSessionResponse(session.Id);
    }

    public async Task ClaimAsync(Guid guestToken, Guid userId, CancellationToken ct = default)
    {
        var session = await _db.GuestSessions.FindAsync(new object[] { guestToken }, ct);

        if (session is null || !session.IsValid)
        {
            throw new BadRequestException(
                "Sesiunea de oaspete este invalidă sau a fost deja revendicată.");
        }

        session.ClaimedByUserId = userId;

        // Order transfer: deferred — Orders table added in a future bolt

        await _db.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Guest session claimed: {SessionId} by User {UserId}", guestToken, userId);
    }

    public async Task<CreateGuestSessionResponse> InitAsync(CancellationToken ct = default)
    {
        var session = new GuestSession
        {
            ExpiresAt = DateTimeOffset.UtcNow.Add(SessionTtl),
        };

        _db.GuestSessions.Add(session);
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("Anonymous guest session initialised: {SessionId}", session.Id);

        return new CreateGuestSessionResponse(session.Id);
    }

    public async Task UpdateContactAsync(
        Guid sessionId,
        UpdateGuestContactRequest request,
        CancellationToken ct = default)
    {
        var session = await _db.GuestSessions.FindAsync(new object[] { sessionId }, ct);

        if (session is null || !session.IsValid)
        {
            throw new BadRequestException(
                "Sesiunea de oaspete este invalidă sau a expirat.");
        }

        session.FirstName = request.FirstName;
        session.LastName  = request.LastName;
        session.Email     = request.Email.ToLowerInvariant();
        session.Phone     = request.Phone;

        await _db.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Guest session contact updated: {SessionId} {Email}", session.Id, session.Email);
    }
}
