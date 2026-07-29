using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PhotoPrint.API.Configuration;
using PhotoPrint.API.Services.Sameday;

namespace PhotoPrint.Tests.Unit.Services.Sameday;

/// <summary>
/// Token provider behaviour (ADR-013). Exercises cache-hit / cache-miss /
/// expiry / invalidate / thundering-herd. <see cref="FakeTimeProvider"/> is
/// used to step time forward deterministically.
/// </summary>
public class SamedayTokenProviderTests
{
    private static readonly DateTimeOffset T0 = new(2026, 6, 2, 12, 0, 0, TimeSpan.Zero);

    private static SamedaySettings ValidSettings() => new()
    {
        Enabled = true,
        BaseUrl = "https://api.sameday.ro",
        Username = "user",
        Password = "pass",
        PickupPointId = "1",
        RequestTimeoutSeconds = 10,
    };

    private static SamedayTokenProvider CreateSut(
        FakeTimeProvider clock,
        FakeAuthenticator authenticator) =>
        new(authenticator,
            Options.Create(ValidSettings()),
            new LoggerFactory().CreateLogger<SamedayTokenProvider>(),
            clock);

    [Fact]
    public async Task First_call_fetches_a_fresh_token()
    {
        var clock = new FakeTimeProvider(T0);
        var auth = new FakeAuthenticator(_ => new SamedayToken("tok1", T0.AddHours(1)));
        var sut = CreateSut(clock, auth);

        var token = await sut.GetTokenAsync();

        token.Value.Should().Be("tok1");
        auth.CallCount.Should().Be(1);
    }

    [Fact]
    public async Task Second_call_within_validity_window_returns_cached_token()
    {
        var clock = new FakeTimeProvider(T0);
        var auth = new FakeAuthenticator(_ => new SamedayToken("tok1", T0.AddHours(1)));
        var sut = CreateSut(clock, auth);

        var first  = await sut.GetTokenAsync();
        var second = await sut.GetTokenAsync();

        first.Should().BeSameAs(second);
        auth.CallCount.Should().Be(1);
    }

    [Fact]
    public async Task Token_inside_safety_window_is_treated_as_expired_and_refreshed()
    {
        var clock = new FakeTimeProvider(T0);
        var sequence = 0;
        var auth = new FakeAuthenticator(_ =>
        {
            sequence++;
            return new SamedayToken($"tok{sequence}", clock.GetUtcNow().AddSeconds(70));
        });
        var sut = CreateSut(clock, auth);

        var first = await sut.GetTokenAsync();   // tok1, expires T0+70s
        clock.Advance(TimeSpan.FromSeconds(15)); // T0+15s, now within 60s safety window of T0+70s
        var second = await sut.GetTokenAsync();  // should fetch tok2

        first.Value.Should().Be("tok1");
        second.Value.Should().Be("tok2");
        auth.CallCount.Should().Be(2);
    }

    [Fact]
    public async Task Invalidate_drops_the_cache_and_forces_refresh_on_next_call()
    {
        var clock = new FakeTimeProvider(T0);
        var sequence = 0;
        var auth = new FakeAuthenticator(_ =>
        {
            sequence++;
            return new SamedayToken($"tok{sequence}", T0.AddHours(1));
        });
        var sut = CreateSut(clock, auth);

        await sut.GetTokenAsync(); // fetches tok1
        sut.Invalidate();
        var afterInvalidate = await sut.GetTokenAsync();

        afterInvalidate.Value.Should().Be("tok2");
        auth.CallCount.Should().Be(2);
    }

    [Fact]
    public async Task Concurrent_first_calls_result_in_exactly_one_fetch()
    {
        // Thundering-herd guard: 50 simultaneous first-time callers must share
        // a single AuthenticateAsync call (ADR-013).
        var clock = new FakeTimeProvider(T0);
        var slowGate = new TaskCompletionSource<SamedayToken>(TaskCreationOptions.RunContinuationsAsynchronously);
        var fetchedCount = 0;

        var auth = new SlowAuthenticator(async ct =>
        {
            Interlocked.Increment(ref fetchedCount);
            return await slowGate.Task;
        });
        var sut = new SamedayTokenProvider(
            auth,
            Options.Create(ValidSettings()),
            new LoggerFactory().CreateLogger<SamedayTokenProvider>(),
            clock);

        var calls = Enumerable.Range(0, 50).Select(_ => sut.GetTokenAsync()).ToArray();

        // Let all 50 contend for the gate, then release the single underlying fetch.
        await Task.Delay(50);
        slowGate.SetResult(new SamedayToken("the-one-token", T0.AddHours(1)));

        var tokens = await Task.WhenAll(calls);

        tokens.Should().AllSatisfy(t => t.Value.Should().Be("the-one-token"));
        fetchedCount.Should().Be(1);
    }

    private sealed class SlowAuthenticator : ISamedayAuthenticator
    {
        private readonly Func<CancellationToken, Task<SamedayToken>> _fetch;
        public SlowAuthenticator(Func<CancellationToken, Task<SamedayToken>> fetch) => _fetch = fetch;

        public Task<SamedayToken> AuthenticateAsync(SamedayCredentials credentials, CancellationToken ct = default)
            => _fetch(ct);
    }
}
