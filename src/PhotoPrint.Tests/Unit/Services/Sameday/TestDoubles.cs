using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using PhotoPrint.API.Services.Sameday;

namespace PhotoPrint.Tests.Unit.Services.Sameday;

/// <summary>
/// Minimal <see cref="TimeProvider"/> implementation for tests. Avoids taking
/// a NuGet dependency on <c>Microsoft.Extensions.TimeProvider.Testing</c> just
/// to step time forward in a handful of token-expiry tests.
///
/// Timers are faked too, so <c>Task.Delay(delay, thisProvider, ct)</c> completes when
/// <see cref="Advance"/> passes the due time instead of sleeping for real. Without that a
/// "deterministic" delay test silently waits out the whole wall-clock delay.
/// </summary>
internal sealed class FakeTimeProvider : TimeProvider
{
    private readonly object _gate = new();
    private readonly List<FakeTimer> _timers = new();
    private DateTimeOffset _now;

    public FakeTimeProvider(DateTimeOffset start)
    {
        _now = start;
    }

    public override DateTimeOffset GetUtcNow()
    {
        lock (_gate) return _now;
    }

    public void Advance(TimeSpan delta)
    {
        List<FakeTimer> due;
        lock (_gate)
        {
            _now = _now.Add(delta);
            due = _timers.Where(t => t.IsDueAt(_now)).ToList();
        }

        // Fire outside the lock: a callback may schedule or dispose timers.
        foreach (var timer in due)
            timer.Fire();
    }

    public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
    {
        var timer = new FakeTimer(this, callback, state);
        lock (_gate) _timers.Add(timer);
        timer.Schedule(GetUtcNow(), dueTime, period);
        return timer;
    }

    private void Remove(FakeTimer timer)
    {
        lock (_gate) _timers.Remove(timer);
    }

    private sealed class FakeTimer : ITimer
    {
        private readonly FakeTimeProvider _owner;
        private readonly TimerCallback _callback;
        private readonly object? _state;
        private DateTimeOffset? _dueAt;
        private TimeSpan _period = Timeout.InfiniteTimeSpan;

        public FakeTimer(FakeTimeProvider owner, TimerCallback callback, object? state)
        {
            _owner = owner;
            _callback = callback;
            _state = state;
        }

        public void Schedule(DateTimeOffset now, TimeSpan dueTime, TimeSpan period)
        {
            _period = period;
            _dueAt = dueTime == Timeout.InfiniteTimeSpan ? null : now.Add(dueTime);
        }

        public bool IsDueAt(DateTimeOffset now) => _dueAt is not null && now >= _dueAt;

        public void Fire()
        {
            if (_dueAt is null) return;
            _dueAt = _period == Timeout.InfiniteTimeSpan ? null : _dueAt.Value.Add(_period);
            _callback(_state);
        }

        public bool Change(TimeSpan dueTime, TimeSpan period)
        {
            Schedule(_owner.GetUtcNow(), dueTime, period);
            return true;
        }

        public void Dispose()
        {
            _dueAt = null;
            _owner.Remove(this);
        }

        public ValueTask DisposeAsync()
        {
            Dispose();
            return ValueTask.CompletedTask;
        }
    }
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
