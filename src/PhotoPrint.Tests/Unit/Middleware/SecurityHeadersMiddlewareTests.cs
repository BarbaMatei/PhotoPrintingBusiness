using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Options;
using PhotoPrint.API.Configuration;
using PhotoPrint.API.Middleware;
using Xunit;

namespace PhotoPrint.Tests.Unit.Middleware;

public class SecurityHeadersMiddlewareTests
{
    // ── Custom feature that captures OnStarting callbacks for manual firing ──────

    /// <summary>
    /// DefaultHttpContext has no real HTTP transport, so OnStarting callbacks
    /// registered via context.Response.OnStarting() are never fired automatically.
    /// This feature implementation stores the callbacks so tests can fire them
    /// explicitly via FireAsync().
    /// </summary>
    private sealed class FireableResponseFeature : IHttpResponseFeature
    {
        private readonly List<(Func<object, Task> Callback, object State)> _callbacks = new();

        public int StatusCode { get; set; } = 200;
        public string? ReasonPhrase { get; set; }
        public IHeaderDictionary Headers { get; set; } = new HeaderDictionary();
        public Stream Body { get; set; } = Stream.Null;
        public bool HasStarted { get; private set; }

        public void OnStarting(Func<object, Task> callback, object state)
            => _callbacks.Add((callback, state));

        public void OnCompleted(Func<object, Task> callback, object state) { }

        public async Task FireAsync()
        {
            HasStarted = true;
            foreach (var (cb, state) in _callbacks)
                await cb(state);
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────────

    private static SecurityHeadersMiddleware CreateSut(
        RequestDelegate next,
        string csp = "default-src 'self'")
    {
        var options = Options.Create(new SecurityHeadersOptions { ContentSecurityPolicy = csp });
        return new SecurityHeadersMiddleware(next, options);
    }

    private static (DefaultHttpContext Context, FireableResponseFeature Feature) CreateContext()
    {
        var feature = new FireableResponseFeature();
        var context = new DefaultHttpContext();
        // Replace the default IHttpResponseFeature so OnStarting callbacks flow to ours
        context.Features.Set<IHttpResponseFeature>(feature);
        return (context, feature);
    }

    private static async Task InvokeAndFireAsync(
        SecurityHeadersMiddleware sut,
        DefaultHttpContext context,
        FireableResponseFeature feature)
    {
        await sut.InvokeAsync(context);
        await feature.FireAsync();
    }

    // ── Tests ─────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task InvokeAsync_AddsXContentTypeOptionsNosniff()
    {
        var (context, feature) = CreateContext();
        await InvokeAndFireAsync(CreateSut(_ => Task.CompletedTask), context, feature);
        context.Response.Headers["X-Content-Type-Options"].ToString().Should().Be("nosniff");
    }

    [Fact]
    public async Task InvokeAsync_AddsXFrameOptionsDeny()
    {
        var (context, feature) = CreateContext();
        await InvokeAndFireAsync(CreateSut(_ => Task.CompletedTask), context, feature);
        context.Response.Headers["X-Frame-Options"].ToString().Should().Be("DENY");
    }

    [Fact]
    public async Task InvokeAsync_AddsReferrerPolicy()
    {
        var (context, feature) = CreateContext();
        await InvokeAndFireAsync(CreateSut(_ => Task.CompletedTask), context, feature);
        context.Response.Headers["Referrer-Policy"].ToString()
            .Should().Be("strict-origin-when-cross-origin");
    }

    [Fact]
    public async Task InvokeAsync_AddsContentSecurityPolicyFromOptions()
    {
        const string csp = "default-src 'self'; frame-ancestors 'none'";
        var (context, feature) = CreateContext();
        await InvokeAndFireAsync(CreateSut(_ => Task.CompletedTask, csp), context, feature);
        context.Response.Headers["Content-Security-Policy"].ToString().Should().Be(csp);
    }

    [Fact]
    public async Task InvokeAsync_StillCallsNextMiddleware()
    {
        var nextCalled = false;
        RequestDelegate next = _ => { nextCalled = true; return Task.CompletedTask; };
        var (context, _) = CreateContext();

        await CreateSut(next).InvokeAsync(context);

        nextCalled.Should().BeTrue();
    }
}
