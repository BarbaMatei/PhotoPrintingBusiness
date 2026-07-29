using System.Net;
using System.Threading.RateLimiting;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Polly;
using Polly.RateLimiting;
using Polly.Retry;
using PhotoPrint.API.Configuration;
using PhotoPrint.API.Services.Sameday;

namespace PhotoPrint.Tests.Unit.Services.Sameday;

/// <summary>
/// Regression suite for the retry resilience pipeline. The critical invariant
/// (ADR-014) is that 401 is NOT retried by Polly — session-expiry handling
/// lives in <c>SamedayAuthHandler</c>. If a future PR tries to "fix" 401
/// handling by adding it to Polly's retryable set, these tests fail.
///
/// We assert behaviour through a scripted handler wrapped in a
/// <c>SamedayResilienceHandler</c> rather than reaching into the pipeline
/// directly — the contract is "what does the wire see", not "what does the
/// pipeline DSL contain".
/// </summary>
public class SamedayPoliciesTests
{
    private static (ScriptedHttpMessageHandler script, HttpClient client) Build(
        params Func<HttpRequestMessage, HttpResponseMessage>[] responses)
    {
        var script = new ScriptedHttpMessageHandler(responses);
        // Bolt 036 contract: tests exercise retry semantics without the bolt-037
        // rate-limit interfering. SamedayResilienceHandler reads
        // MaxConcurrentSamedayCalls; the special sentinel int.MaxValue disables
        // the limiter via SamedayPolicies.BuildRetryPipeline.
        var settings = Options.Create(new SamedaySettings
        {
            Jobs = new SamedayJobsSettings { MaxConcurrentSamedayCalls = int.MaxValue },
        });
        var resilience = new SamedayResilienceHandler(settings, NullLogger<SamedayResilienceHandler>.Instance) { InnerHandler = script };
        var client = new HttpClient(resilience)
        {
            BaseAddress = new Uri("https://sameday-test/"),
            Timeout = TimeSpan.FromSeconds(30),
        };
        return (script, client);
    }

    [Fact]
    public async Task Retries_on_500_and_eventually_succeeds()
    {
        var (script, client) = Build(
            _ => ScriptedHttpMessageHandler.Empty(HttpStatusCode.InternalServerError),
            _ => ScriptedHttpMessageHandler.Json(HttpStatusCode.OK, "{}"));

        // Single retry → 1s backoff — keep below the 30s xunit default by using a
        // tiny operation. (The retry strategy's Delay isn't easily injectable; this
        // test tolerates the 1s sleep.)
        var response = await client.GetAsync("/test");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        script.CallCount.Should().Be(2);
    }

    [Fact]
    public async Task Does_not_retry_on_401()
    {
        // ADR-014 regression: 401 must NOT be in the retryable set; session
        // expiry is owned by SamedayAuthHandler at a higher layer.
        var (script, client) = Build(
            _ => ScriptedHttpMessageHandler.Empty(HttpStatusCode.Unauthorized));

        var response = await client.GetAsync("/test");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        script.CallCount.Should().Be(1);
    }

    [Fact]
    public async Task Does_not_retry_on_400()
    {
        // Validation errors (our request is malformed) must not be retried —
        // the bug is on our side.
        var (script, client) = Build(
            _ => ScriptedHttpMessageHandler.Empty(HttpStatusCode.BadRequest));

        var response = await client.GetAsync("/test");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        script.CallCount.Should().Be(1);
    }

    [Fact]
    public async Task Retries_on_429_too_many_requests()
    {
        var (script, client) = Build(
            _ => ScriptedHttpMessageHandler.Empty(HttpStatusCode.TooManyRequests),
            _ => ScriptedHttpMessageHandler.Json(HttpStatusCode.OK, "{}"));

        var response = await client.GetAsync("/test");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        script.CallCount.Should().Be(2);
    }

    // ── Rate limiter (bolt 037): one shared limiter, actually throttling ─────────

    [Fact]
    public async Task Shared_rate_limiter_throttles_when_its_only_permit_is_held()
    {
        using var limiter = new SlidingWindowRateLimiter(new SlidingWindowRateLimiterOptions
        {
            PermitLimit = 1,
            Window = TimeSpan.FromMinutes(1),
            SegmentsPerWindow = 1,
            QueueLimit = 0,
        });
        var pipeline = SamedayPolicies.BuildPipeline(limiter);

        // Hold the only permit. The pipeline shares THIS limiter, so an execution is
        // rejected — a per-call `new` limiter (the bug) would hand it a fresh permit.
        using var held = limiter.AttemptAcquire(1);
        held.IsAcquired.Should().BeTrue();

        var act = async () => await pipeline.ExecuteAsync(
            async _ => { await Task.Yield(); return new HttpResponseMessage(HttpStatusCode.OK); });

        await act.Should().ThrowAsync<RateLimiterRejectedException>();
    }

    [Theory]
    [InlineData(int.MaxValue, false)]
    [InlineData(5, true)]
    public void CreateRateLimiter_opts_out_only_on_the_sentinel(int permits, bool expectLimiter)
    {
        var limiter = SamedayPolicies.CreateRateLimiter(permits);
        (limiter is not null).Should().Be(expectLimiter);
        limiter?.Dispose();
    }

    [Fact]
    public async Task Retry_backoff_is_1_4_16_seconds_not_Polly_default_base_2()
    {
        // The delays are asserted on the strategy the pipeline is built from, so the schedule is
        // pinned without waiting 21 s of real backoff. Polly's DelayBackoffType.Exponential yields
        // 1/2/4; reverting to it drops DelayGenerator to null and reddens this test.
        var options = SamedayPolicies.BuildRetryOptions();

        options.MaxRetryAttempts.Should().Be(3);
        options.UseJitter.Should().BeFalse("a jittered delay could not be asserted exactly");
        options.DelayGenerator.Should().NotBeNull();

        var delays = new List<TimeSpan?>();
        for (var attemptNumber = 0; attemptNumber < 3; attemptNumber++)
        {
            var args = new RetryDelayGeneratorArguments<HttpResponseMessage>(
                ResilienceContextPool.Shared.Get(),
                Outcome.FromResult(new HttpResponseMessage(HttpStatusCode.InternalServerError)),
                attemptNumber);
            delays.Add(await options.DelayGenerator!(args));
        }

        delays.Should().Equal(
            TimeSpan.FromSeconds(1),
            TimeSpan.FromSeconds(4),
            TimeSpan.FromSeconds(16));
    }
}
