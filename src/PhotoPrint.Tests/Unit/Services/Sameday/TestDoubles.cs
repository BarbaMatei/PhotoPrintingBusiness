using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using PhotoPrint.API.Services.Sameday;

namespace PhotoPrint.Tests.Unit.Services.Sameday;

/// <summary>
/// Minimal <see cref="TimeProvider"/> implementation for tests. Avoids taking
/// a NuGet dependency on <c>Microsoft.Extensions.TimeProvider.Testing</c> just
/// to step time forward in a handful of token-expiry tests.
/// </summary>
internal sealed class FakeTimeProvider : TimeProvider
{
    private DateTimeOffset _now;

    public FakeTimeProvider(DateTimeOffset start)
    {
        _now = start;
    }

    public override DateTimeOffset GetUtcNow() => _now;

    public void Advance(TimeSpan delta) => _now = _now.Add(delta);
}

/// <summary>
/// Scripted <see cref="HttpMessageHandler"/> for transport-level tests.
/// Each invocation pops the next response from the queue and records the
/// request that produced it (cloned headers + body so the test can inspect
/// what the auth handler / client sent on the wire).
/// </summary>
internal sealed class ScriptedHttpMessageHandler : HttpMessageHandler
{
    private readonly Queue<Func<HttpRequestMessage, HttpResponseMessage>> _responses;
    private readonly List<RecordedRequest> _recorded = new();
    private readonly object _gate = new();

    public ScriptedHttpMessageHandler(params Func<HttpRequestMessage, HttpResponseMessage>[] responses)
    {
        _responses = new Queue<Func<HttpRequestMessage, HttpResponseMessage>>(responses);
    }

    public IReadOnlyList<RecordedRequest> Recorded => _recorded;
    public int CallCount => _recorded.Count;

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        // Buffer body BEFORE the script may dispose anything.
        var bodyBytes = request.Content is null
            ? Array.Empty<byte>()
            : await request.Content.ReadAsByteArrayAsync(cancellationToken);

        var authHeader = request.Headers.Authorization is null
            ? null
            : new AuthenticationHeaderValue(
                request.Headers.Authorization.Scheme,
                request.Headers.Authorization.Parameter);

        var record = new RecordedRequest(
            request.Method,
            request.RequestUri,
            authHeader,
            bodyBytes);

        lock (_gate)
            _recorded.Add(record);

        Func<HttpRequestMessage, HttpResponseMessage> next;
        lock (_gate)
        {
            if (_responses.Count == 0)
                throw new InvalidOperationException(
                    $"ScriptedHttpMessageHandler ran out of canned responses (call #{_recorded.Count}).");
            next = _responses.Dequeue();
        }

        return next(request);
    }

    public static HttpResponseMessage Json(HttpStatusCode status, string json)
        => new(status)
        {
            Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json"),
        };

    public static HttpResponseMessage Empty(HttpStatusCode status) => new(status);
}

internal sealed record RecordedRequest(
    HttpMethod Method,
    Uri? Uri,
    AuthenticationHeaderValue? Authorization,
    byte[] Body)
{
    public string BodyText() => System.Text.Encoding.UTF8.GetString(Body);
}

/// <summary>
/// In-memory <see cref="ISamedayAuthenticator"/> for tests that exercise the
/// token provider without involving HTTP. Records every call so tests can
/// assert "called exactly once across N concurrent requests".
/// </summary>
internal sealed class FakeAuthenticator : ISamedayAuthenticator
{
    private readonly Func<SamedayCredentials, SamedayToken> _factory;
    private int _callCount;

    public FakeAuthenticator(Func<SamedayCredentials, SamedayToken> factory)
    {
        _factory = factory;
    }

    public int CallCount => Volatile.Read(ref _callCount);

    public Task<SamedayToken> AuthenticateAsync(SamedayCredentials credentials, CancellationToken ct = default)
    {
        Interlocked.Increment(ref _callCount);
        return Task.FromResult(_factory(credentials));
    }
}
