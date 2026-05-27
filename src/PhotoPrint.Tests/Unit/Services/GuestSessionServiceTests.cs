using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using PhotoPrint.API.Data;
using PhotoPrint.API.DTOs.Auth;
using PhotoPrint.API.Exceptions;
using PhotoPrint.API.Models;
using PhotoPrint.API.Services;

namespace PhotoPrint.Tests.Unit.Services;

public class GuestSessionServiceTests
{
    private readonly PhotoPrintDbContext _db;
    private readonly IGuestSessionService _sut;

    public GuestSessionServiceTests()
    {
        var options = new DbContextOptionsBuilder<PhotoPrintDbContext>()
            .UseInMemoryDatabase($"GuestSession_{Guid.NewGuid():N}")
            .Options;
        _db = new PhotoPrintDbContext(options);
        _sut = new GuestSessionService(_db, Mock.Of<ILogger<GuestSessionService>>());
    }

    private static CreateGuestSessionRequest ValidRequest(string email = "guest@test.com") =>
        new("Ion", "Popescu", email, "0712345678");

    // ── CreateAsync ────────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateAsync_ValidRequest_ReturnsGuestToken()
    {
        var response = await _sut.CreateAsync(ValidRequest(), default);

        response.GuestToken.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public async Task CreateAsync_ValidRequest_PersistsSessionToDb()
    {
        var response = await _sut.CreateAsync(ValidRequest("test@example.com"), default);

        var session = await _db.GuestSessions.SingleAsync();
        session.Id.Should().Be(response.GuestToken);
        session.Email.Should().Be("test@example.com");
        session.FirstName.Should().Be("Ion");
        session.LastName.Should().Be("Popescu");
        session.Phone.Should().Be("0712345678");
    }

    [Fact]
    public async Task CreateAsync_ValidRequest_SetsTtlOfSevenDays()
    {
        var before = DateTimeOffset.UtcNow;
        await _sut.CreateAsync(ValidRequest(), default);
        var after = DateTimeOffset.UtcNow;

        var session = await _db.GuestSessions.SingleAsync();
        session.ExpiresAt.Should().BeOnOrAfter(before.AddDays(7));
        session.ExpiresAt.Should().BeOnOrBefore(after.AddDays(7).AddSeconds(1));
    }

    [Fact]
    public async Task CreateAsync_ValidRequest_SessionIsValid()
    {
        await _sut.CreateAsync(ValidRequest(), default);

        var session = await _db.GuestSessions.SingleAsync();
        session.IsValid.Should().BeTrue();
        session.IsExpired.Should().BeFalse();
        session.IsClaimed.Should().BeFalse();
    }

    // ── ClaimAsync ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task ClaimAsync_ValidToken_SetsClaimedByUserId()
    {
        var session = new GuestSession
        {
            Email = "g@test.com",
            FirstName = "Ion",
            LastName = "Popescu",
            Phone = "0712345678",
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(7),
        };
        _db.GuestSessions.Add(session);
        await _db.SaveChangesAsync();

        var userId = Guid.NewGuid();
        await _sut.ClaimAsync(session.Id, userId, default);

        var updated = await _db.GuestSessions.SingleAsync();
        updated.ClaimedByUserId.Should().Be(userId);
        updated.IsClaimed.Should().BeTrue();
        updated.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task ClaimAsync_UnknownToken_ThrowsBadRequestException()
    {
        var act = async () => await _sut.ClaimAsync(Guid.NewGuid(), Guid.NewGuid(), default);

        await act.Should().ThrowAsync<BadRequestException>();
    }

    [Fact]
    public async Task ClaimAsync_ExpiredSession_ThrowsBadRequestException()
    {
        var session = new GuestSession
        {
            Email = "g@test.com",
            FirstName = "Ion",
            LastName = "Popescu",
            Phone = "0712345678",
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(-1),   // already expired
        };
        _db.GuestSessions.Add(session);
        await _db.SaveChangesAsync();

        var act = async () => await _sut.ClaimAsync(session.Id, Guid.NewGuid(), default);

        await act.Should().ThrowAsync<BadRequestException>();
    }

    [Fact]
    public async Task ClaimAsync_AlreadyClaimedSession_ThrowsBadRequestException()
    {
        var session = new GuestSession
        {
            Email = "g@test.com",
            FirstName = "Ion",
            LastName = "Popescu",
            Phone = "0712345678",
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(7),
            ClaimedByUserId = Guid.NewGuid(),
        };
        _db.GuestSessions.Add(session);
        await _db.SaveChangesAsync();

        var act = async () => await _sut.ClaimAsync(session.Id, Guid.NewGuid(), default);

        await act.Should().ThrowAsync<BadRequestException>();
    }
}
