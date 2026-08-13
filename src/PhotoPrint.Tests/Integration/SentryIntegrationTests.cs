using FluentAssertions;

namespace PhotoPrint.Tests.Integration;

/// <summary>
/// End-to-end: a synthetic 500 endpoint exists only in the Testing environment.
/// We hit it with a known correlation id and assert that the production
/// <c>ExceptionHandlerMiddleware</c> captured the exception through the
/// Sentry IHub seam, and that <c>SentryScopeEnricherMiddleware</c> stamped
/// the <c>correlation_id</c> tag on the request scope.
/// </summary>
[Collection(ObservabilityHostCollection.Name)]
public class SentryIntegrationTests : IClassFixture<SentryIntegrationFactory>
{
    private readonly SentryIntegrationFactory _factory;

    public SentryIntegrationTests(SentryIntegrationFactory factory) => _factory = factory;

    [Fact]
    public async Task Synthetic_500_captures_exception_through_sentry_hub()
    {
        _factory.CapturedEvents.Clear();
        _factory.CapturedTags.Clear();

        var client = _factory.CreateClient();
        var correlationId = Guid.NewGuid().ToString();
        client.DefaultRequestHeaders.Add("X-Correlation-Id", correlationId);

        var response = await client.GetAsync("/__test/throw");

        response.StatusCode.Should().Be(System.Net.HttpStatusCode.InternalServerError);

        _factory.CapturedEvents.Should().NotBeEmpty(
            because: "ExceptionHandlerMiddleware should have called IHub.CaptureException → CaptureEvent");

        _factory.CapturedEvents.Should().Contain(e =>
            e.Exception != null && e.Exception.Message.Contains("synthetic-test-exception"));

        _factory.CapturedTags.Should().ContainKey("correlation_id")
            .WhoseValue.Should().Be(correlationId);
    }
}
