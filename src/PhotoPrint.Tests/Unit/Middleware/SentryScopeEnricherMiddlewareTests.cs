using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using PhotoPrint.API.Middleware;

namespace PhotoPrint.Tests.Unit.Middleware;

/// <summary>
/// Smoke tests for the scope enricher. The positive case (tags actually reach
/// a captured event) is covered by <c>SentryIntegrationTests</c>; here we just
/// pin the "no IHub in DI → no-op" contract and the no-throw guarantees for
/// missing context bits.
/// </summary>
public class SentryScopeEnricherMiddlewareTests
{
    private static HttpContext NewContextWithoutSentry()
    {
        var services = new ServiceCollection().BuildServiceProvider();
        return new DefaultHttpContext { RequestServices = services };
    }

    [Fact]
    public async Task When_IHub_is_absent_middleware_is_a_no_op()
    {
        // No Sentry IHub registered in the DI scope → middleware skips
        // enrichment but must still pass control through.
        var sut = new SentryScopeEnricherMiddleware();
        var ctx = NewContextWithoutSentry();
        ctx.Items["CorrelationId"] = Guid.NewGuid();

        var nextCalled = false;
        await sut.InvokeAsync(ctx, _ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        nextCalled.Should().BeTrue();
    }

    [Fact]
    public async Task Middleware_passes_through_when_no_correlation_id_in_context()
    {
        var sut = new SentryScopeEnricherMiddleware();
        var ctx = NewContextWithoutSentry();   // no CorrelationId in Items

        var nextCalled = false;
        await sut.InvokeAsync(ctx, _ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        nextCalled.Should().BeTrue();
    }

    [Fact]
    public async Task Middleware_passes_through_with_unauthenticated_user()
    {
        var sut = new SentryScopeEnricherMiddleware();
        var ctx = NewContextWithoutSentry();
        ctx.User = new ClaimsPrincipal(new ClaimsIdentity());

        var nextCalled = false;
        await sut.InvokeAsync(ctx, _ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        nextCalled.Should().BeTrue();
    }
}
