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
        var ctx = BuildContext("""{"deliveryType":"Courier","shippingCostRon":-100}""");

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
        var ctx = BuildContext("""{"deliveryType":"Easybox"}""");

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

    // Buffering happens before the guest token is even looked at, so an unbounded peek is memory anyone can claim.
    [Fact]
    public async Task OversizeBody_IsNotBufferedWholeAndStillPassesThrough()
    {
        var (sut, _) = BuildSut();
        var payload = Encoding.UTF8.GetBytes(
            "{\"deliveryType\":\"Easybox\",\"pad\":\"" + new string('x', 1024 * 1024) + "\"}");
        var counting = new CountingStream(payload);
        var http = new DefaultHttpContext();
        http.Request.Path = "/api/payments/stripe/intent";
        http.Request.Method = "POST";
        http.Request.ContentType = "application/json";
        http.Request.Body = counting;
        var ctx = new ResourceExecutingContext(
            new ActionContext(http, new RouteData(), new ActionDescriptor()),
            new List<IFilterMetadata>(), new List<IValueProviderFactory>());

        var act = async () => await sut.OnResourceExecutionAsync(ctx, () =>
            Task.FromResult<ResourceExecutedContext>(new ResourceExecutedContext(ctx, new List<IFilterMetadata>())));

        await act.Should().NotThrowAsync();
        counting.BytesRead.Should().BeLessThan(
            256 * 1024, "the peek is capped, so a multi-megabyte body must not land in memory here");
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

    private sealed class CountingStream : MemoryStream
    {
        public CountingStream(byte[] buffer) : base(buffer, writable: false) { }

        public int BytesRead { get; private set; }

        public override int Read(byte[] buffer, int offset, int count)
        {
            var read = base.Read(buffer, offset, count);
            BytesRead += read;
            return read;
        }

        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            var read = base.Read(buffer.Span);
            BytesRead += read;
            return ValueTask.FromResult(read);
        }
    }
}
