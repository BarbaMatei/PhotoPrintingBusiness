using System.Net;
using FluentAssertions;

namespace PhotoPrint.Tests.Integration;

/// <summary>
/// A mapped status code is not proof of an expected outcome: a mapped 5xx is a dependency
/// failure that burns the availability SLO, so it has to reach the error tracker the same way
/// an unmapped 500 does. The 4xx leg is asserted alongside it — capturing business outcomes
/// would drown the quota that the SLO alerting depends on.
/// </summary>
[Collection(ObservabilityHostCollection.Name)]
public class MappedServerErrorSentryTests : IClassFixture<SentryIntegrationFactory>
{
    private readonly SentryIntegrationFactory _factory;

    public MappedServerErrorSentryTests(SentryIntegrationFactory factory) => _factory = factory;

    [Fact]
    public async Task A_mapped_502_is_captured_to_sentry()
    {
        _factory.CapturedEvents.Clear();
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/__test/throw-mapped-502");

        response.StatusCode.Should().Be(HttpStatusCode.BadGateway);
        _factory.CapturedEvents.Should().Contain(e =>
            e.Exception != null && e.Exception.Message.Contains("synthetic-mapped-502"));
    }

    [Fact]
    public async Task A_mapped_404_is_not_captured_to_sentry()
    {
        _factory.CapturedEvents.Clear();
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/__test/throw-mapped-404");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        _factory.CapturedEvents.Should().NotContain(e =>
            e.Exception != null && e.Exception.Message.Contains("synthetic-mapped-404"));
    }
}
