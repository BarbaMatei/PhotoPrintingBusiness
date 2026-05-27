using System.Text;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Moq;
using PhotoPrint.API.Filters;
using Xunit;

namespace PhotoPrint.Tests.Unit.Filters;

public class DetectLegacyShippingCostFilterTests
{
    private static ResourceExecutingContext BuildContext(string body, string path = "/api/payments/stripe/intent")
    {
        var http = new DefaultHttpContext();
        http.Request.Path = path;
        http.Request.Method = "POST";
        http.Request.ContentType = "application/json";
        http.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(body));

        var actionContext = new ActionContext(http, new RouteData(), new ActionDescriptor());
        return new ResourceExecutingContext(actionContext, new List<IFilterMetadata>(), new List<IValueProviderFactory>());
    }

    private static (DetectLegacyShippingCostFilter filter, Mock<ILogger<DetectLegacyShippingCostFilter>> log) BuildSut()
    {
        var log = new Mock<ILogger<DetectLegacyShippingCostFilter>>();
        return (new DetectLegacyShippingCostFilter(log.Object), log);
    }

    private static int CountWarnings(Mock<ILogger<DetectLegacyShippingCostFilter>> log) =>
        log.Invocations
            .Where(i => i.Method.Name == nameof(ILogger.Log))
            .Count(i => (LogLevel)i.Arguments[0] == LogLevel.Warning);

    [Fact]
    public async Task BodyContainingShippingCostRon_LogsWarning()
    {
        var (sut, log) = BuildSut();
        var ctx = BuildContext("""{"paymentProcessor":"Stripe","shippingCostRon":-100}""");

        await sut.OnResourceExecutionAsync(ctx, () =>
            Task.FromResult<ResourceExecutedContext>(new ResourceExecutedContext(ctx, new List<IFilterMetadata>())));

        CountWarnings(log).Should().Be(1);
    }

    [Fact]
    public async Task BodyContainingShippingCostRon_CaseInsensitive_LogsWarning()
    {
        var (sut, log) = BuildSut();
        var ctx = BuildContext("""{"ShippingCostRon":42}""");

        await sut.OnResourceExecutionAsync(ctx, () =>
            Task.FromResult<ResourceExecutedContext>(new ResourceExecutedContext(ctx, new List<IFilterMetadata>())));

        CountWarnings(log).Should().Be(1);
    }

    [Fact]
    public async Task BodyWithoutShippingCostRon_DoesNotLog()
    {
        var (sut, log) = BuildSut();
        var ctx = BuildContext("""{"paymentProcessor":"Stripe","deliveryType":"Easybox"}""");

        await sut.OnResourceExecutionAsync(ctx, () =>
            Task.FromResult<ResourceExecutedContext>(new ResourceExecutedContext(ctx, new List<IFilterMetadata>())));

        CountWarnings(log).Should().Be(0);
    }

    [Fact]
    public async Task EmptyBody_DoesNotThrowAndDoesNotLog()
    {
        var (sut, log) = BuildSut();
        var ctx = BuildContext("");

        var act = async () =>
            await sut.OnResourceExecutionAsync(ctx, () =>
                Task.FromResult<ResourceExecutedContext>(new ResourceExecutedContext(ctx, new List<IFilterMetadata>())));

        await act.Should().NotThrowAsync();
        CountWarnings(log).Should().Be(0);
    }

    [Fact]
    public async Task MalformedJson_DoesNotThrowAndDoesNotLog()
    {
        var (sut, log) = BuildSut();
        var ctx = BuildContext("not json{");

        var act = async () =>
            await sut.OnResourceExecutionAsync(ctx, () =>
                Task.FromResult<ResourceExecutedContext>(new ResourceExecutedContext(ctx, new List<IFilterMetadata>())));

        await act.Should().NotThrowAsync();
        CountWarnings(log).Should().Be(0);
    }

    [Fact]
    public async Task Filter_RewindsBodyStream_ForDownstreamModelBinder()
    {
        var (sut, _) = BuildSut();
        var ctx = BuildContext("""{"shippingCostRon":-100,"deliveryType":"Easybox"}""");

        await sut.OnResourceExecutionAsync(ctx, () =>
            Task.FromResult<ResourceExecutedContext>(new ResourceExecutedContext(ctx, new List<IFilterMetadata>())));

        ctx.HttpContext.Request.Body.Position.Should().Be(0,
            "the model binder reads the body next and needs it positioned at the start");
    }
}
