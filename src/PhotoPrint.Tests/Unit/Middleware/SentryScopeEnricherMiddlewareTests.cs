using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using PhotoPrint.API.Middleware;
using Sentry;

namespace PhotoPrint.Tests.Unit.Middleware;

public class SentryScopeEnricherMiddlewareTests
{
    private static HttpContext NewContextWithoutSentry()
    {
        var services = new ServiceCollection().BuildServiceProvider();
        return new DefaultHttpContext { RequestServices = services };
    }

    // A real Sentry Scope behind a hub stub: the enrichment body only runs when IHub resolves
    // and reports enabled, and what it writes is SDK state, not a test double's dictionary.
    private static (HttpContext Context, Scope Scope) NewContextWithSentry()
    {
        var scope = new Scope(new SentryOptions { Dsn = "https://dummy@sentry.invalid/0" });

        var hub = new Mock<IHub>();
        hub.SetupGet(h => h.IsEnabled).Returns(true);
        hub.Setup(h => h.ConfigureScope(It.IsAny<Action<Scope>>()))
           .Callback<Action<Scope>>(configure => configure(scope));

        var services = new ServiceCollection()
            .AddSingleton(hub.Object)
            .BuildServiceProvider();

        return (new DefaultHttpContext { RequestServices = services }, scope);
    }

    private static async Task<bool> Invoke(HttpContext context)
    {
        var nextCalled = false;
        await new SentryScopeEnricherMiddleware().InvokeAsync(context, _ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });
        return nextCalled;
    }

    [Fact]
    public async Task Authenticated_request_stamps_correlation_id_and_user_id_on_the_scope()
    {
        var (context, scope) = NewContextWithSentry();
        var correlationId = Guid.NewGuid();
        var userId = Guid.NewGuid().ToString();

        context.Items["CorrelationId"] = correlationId;
        context.User = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(ClaimTypes.NameIdentifier, userId)], authenticationType: "Test"));

        (await Invoke(context)).Should().BeTrue();

        scope.Tags.Should().ContainKey("correlation_id")
             .WhoseValue.Should().Be(correlationId.ToString());
        scope.User.Id.Should().Be(userId);
    }

    [Fact]
    public async Task Anonymous_request_stamps_the_correlation_id_but_no_user()
    {
        var (context, scope) = NewContextWithSentry();
        var correlationId = Guid.NewGuid();

        context.Items["CorrelationId"] = correlationId;
        context.User = new ClaimsPrincipal(new ClaimsIdentity());

        (await Invoke(context)).Should().BeTrue();

        scope.Tags.Should().ContainKey("correlation_id")
             .WhoseValue.Should().Be(correlationId.ToString());
        scope.User.Id.Should().BeNull();
    }

    [Fact]
    public async Task A_request_without_a_correlation_id_stamps_no_tag()
    {
        var (context, scope) = NewContextWithSentry();

        (await Invoke(context)).Should().BeTrue();

        scope.Tags.Should().NotContainKey("correlation_id");
    }

    [Fact]
    public async Task A_disabled_hub_is_never_asked_to_configure_a_scope()
    {
        var hub = new Mock<IHub>(MockBehavior.Strict);
        hub.SetupGet(h => h.IsEnabled).Returns(false);
        var services = new ServiceCollection().AddSingleton(hub.Object).BuildServiceProvider();
        var context = new DefaultHttpContext { RequestServices = services };
        context.Items["CorrelationId"] = Guid.NewGuid();

        (await Invoke(context)).Should().BeTrue();

        hub.Verify(h => h.ConfigureScope(It.IsAny<Action<Scope>>()), Times.Never);
    }

    [Fact]
    public async Task When_IHub_is_absent_middleware_is_a_no_op()
    {
        var context = NewContextWithoutSentry();
        context.Items["CorrelationId"] = Guid.NewGuid();

        (await Invoke(context)).Should().BeTrue();
    }

    [Fact]
    public async Task Middleware_passes_through_when_no_correlation_id_in_context()
    {
        (await Invoke(NewContextWithoutSentry())).Should().BeTrue();
    }

    [Fact]
    public async Task Middleware_passes_through_with_unauthenticated_user()
    {
        var context = NewContextWithoutSentry();
        context.User = new ClaimsPrincipal(new ClaimsIdentity());

        (await Invoke(context)).Should().BeTrue();
    }
}
