using System.Net;
using FluentAssertions;
using Microsoft.Extensions.Options;
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
        var resilience = new SamedayResilienceHandler(settings) { InnerHandler = script };
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
}
