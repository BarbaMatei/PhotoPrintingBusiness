using System.Reflection;
using System.Text;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Moq;
using PhotoPrint.API.Configuration;
using PhotoPrint.API.Controllers;
using PhotoPrint.API.Data;
using PhotoPrint.API.Exceptions;
using PhotoPrint.API.Hubs;
using PhotoPrint.API.Observability;
using PhotoPrint.API.Services;
using PhotoPrint.API.Services.Invoicing;
using PhotoPrint.API.Services.Sameday;
using PhotoPrint.Tests.Helpers;
using Stripe;
using Xunit;

namespace PhotoPrint.Tests.Unit.Controllers;

// The Stripe webhook is anonymous, so whatever it buffers before the signature check is memory an unauthenticated caller controls.
public class WebhooksControllerBodyLimitTests
{
    private readonly Mock<IStripeSignatureVerifier> _stripeVerifier = new();
    private readonly LogCapture _logs = new();
    private readonly WebhooksController _sut;

    public WebhooksControllerBodyLimitTests()
    {
        var db = new PhotoPrintDbContext(
            new DbContextOptionsBuilder<PhotoPrintDbContext>()
                .UseInMemoryDatabase($"WebhookBody_{Guid.NewGuid():N}")
                .Options);

        _sut = new WebhooksController(
            new Mock<IOrderService>().Object,
            _stripeVerifier.Object,
            new Mock<IEuPlatescService>().Object,
            db,
            new Mock<IOrderEmailService>().Object,
            new Mock<IOrderPhotoPromoter>().Object,
            new Mock<IAwbCreationNotifier>().Object,
            new Mock<IInvoiceCreationService>().Object,
            new Mock<IHubContext<AdminOrderHub>>().Object,
            Options.Create(new StripeSettings { WebhookSecret = "whsec_test" }),
            Options.Create(new EuPlatescSettings { SecretKey = "00112233445566778899aabbccddeeff", MerchantId = "M1" }),
            _logs.LoggerFor<WebhooksController>());
    }

    private void GivenBody(Stream body, long? contentLength = null)
    {
        var http = new DefaultHttpContext();
        http.Request.Body = body;
        http.Request.ContentLength = contentLength;
        _sut.ControllerContext = new ControllerContext { HttpContext = http };
    }

    private static string PaddedEvent(int totalBytes)
    {
        const string head = "{\"data\":{\"object\":{\"id\":\"pi_1\"}},\"pad\":\"";
        const string tail = "\"}";
        return head + new string('x', totalBytes - head.Length - tail.Length) + tail;
    }

    [Fact]
    public async Task Stripe_body_over_the_cap_is_rejected_before_the_signature_is_verified()
    {
        var oversized = Encoding.UTF8.GetBytes(PaddedEvent(WebhooksController.StripeMaxBodyBytes + 1));
        GivenBody(new MemoryStream(oversized), oversized.Length);

        var act = () => _sut.StripeWebhookAsync(default);

        await act.Should().ThrowAsync<RequestEntityTooLargeException>();
        _stripeVerifier.Verify(
            v => v.ConstructEvent(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()),
            Times.Never);
    }

    [Fact]
    public async Task Stripe_body_with_no_content_length_stops_reading_at_the_cap()
    {
        var endless = new CountingEndlessStream();
        GivenBody(endless, contentLength: null);

        var act = () => _sut.StripeWebhookAsync(default);

        await act.Should().ThrowAsync<RequestEntityTooLargeException>();
        endless.BytesRead.Should().BeLessThan(
            WebhooksController.StripeMaxBodyBytes + 4096,
            "a chunked body with no declared length must not be buffered past the cap");
    }

    [Fact]
    public async Task Stripe_body_at_the_cap_is_still_processed()
    {
        var atCap = Encoding.UTF8.GetBytes(PaddedEvent(WebhooksController.StripeMaxBodyBytes));
        GivenBody(new MemoryStream(atCap), atCap.Length);
        _stripeVerifier
            .Setup(v => v.ConstructEvent(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(new Event { Type = "customer.created" });

        var result = await _sut.StripeWebhookAsync(default);

        result.Should().BeOfType<OkResult>(
            "rejecting a legitimate event buys a three-day Stripe retry cycle");
    }

    [Fact]
    public async Task Stripe_body_arriving_one_byte_at_a_time_is_still_assembled_whole()
    {
        var json = "{\"data\":{\"object\":{\"id\":\"pi_drip\"}}}";
        GivenBody(new DripStream(Encoding.UTF8.GetBytes(json)), contentLength: null);
        _stripeVerifier
            .Setup(v => v.ConstructEvent(json, It.IsAny<string>(), It.IsAny<string>()))
            .Returns(new Event { Type = "customer.created" });

        var result = await _sut.StripeWebhookAsync(default);

        result.Should().BeOfType<OkResult>(
            "a socket hands over whatever it has, so the read loop has to accumulate short reads");
    }

    [Fact]
    public void The_attribute_limit_sits_above_the_cap_the_action_enforces()
    {
        var declared = (long)typeof(WebhooksController)
            .GetMethod(nameof(WebhooksController.StripeWebhookAsync))!
            .GetCustomAttributesData()
            .First(a => a.AttributeType == typeof(RequestSizeLimitAttribute))
            .ConstructorArguments[0].Value!;

        declared.Should().BeGreaterThan(WebhooksController.StripeMaxBodyBytes,
            "the action owns the clean 413; the attribute is only the byte backstop under it");
    }

    [Fact]
    public async Task Stripe_oversized_body_is_counted_under_its_own_result_label()
    {
        using var capture = new MetricCapture(MetricNames.Instruments.PaymentWebhookTotal);
        var oversized = Encoding.UTF8.GetBytes(PaddedEvent(WebhooksController.StripeMaxBodyBytes + 1));
        GivenBody(new MemoryStream(oversized), oversized.Length);

        try { await _sut.StripeWebhookAsync(default); } catch (RequestEntityTooLargeException) { }

        capture.For(MetricNames.Instruments.PaymentWebhookTotal,
                (MetricNames.Labels.Processor, MetricNames.ProcessorValues.Stripe),
                (MetricNames.Labels.Result, MetricNames.WebhookResultValues.BodyTooLarge))
            .Should().HaveCount(1);
        _logs.Records.Should().ContainSingle(r =>
            r.Message.StartsWith("payments.webhook.body-too-large", StringComparison.Ordinal));
    }

    [Fact]
    public void No_action_in_the_api_disables_the_request_size_limit()
    {
        var offenders = typeof(WebhooksController).Assembly.GetTypes()
            .Where(t => typeof(ControllerBase).IsAssignableFrom(t))
            .SelectMany(t => t.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .Cast<MemberInfo>()
                .Append(t))
            .Where(m => m.GetCustomAttribute<DisableRequestSizeLimitAttribute>() is not null)
            .Select(m => $"{m.DeclaringType?.Name ?? "?"}.{m.Name}")
            .ToList();

        offenders.Should().BeEmpty(
            "an endpoint with no byte ceiling is a memory-exhaustion vector for whoever can reach it");
    }

    [Theory]
    [InlineData(nameof(WebhooksController.StripeWebhookAsync))]
    [InlineData(nameof(WebhooksController.EuPlatescIpnAsync))]
    public void Every_anonymous_webhook_action_caps_its_request_body(string action)
    {
        var method = typeof(WebhooksController).GetMethod(action)!;

        method.GetCustomAttribute<RequestSizeLimitAttribute>().Should().NotBeNull(
            "the action is anonymous, so its body is attacker-sized until something caps it");
    }

    [Fact]
    public void The_payment_endpoints_cap_the_body_their_filter_buffers()
    {
        typeof(PaymentsController).GetCustomAttribute<RequestSizeLimitAttribute>().Should().NotBeNull(
            "DetectLegacyShippingCostFilter buffers the whole body for a caller holding a free guest token");
    }

    private sealed class DripStream : MemoryStream
    {
        public DripStream(byte[] buffer) : base(buffer, writable: false) { }

        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
            => ValueTask.FromResult(base.Read(buffer.Length == 0 ? buffer.Span : buffer.Span[..1]));
    }

    private sealed class CountingEndlessStream : Stream
    {
        public int BytesRead { get; private set; }

        public override int Read(byte[] buffer, int offset, int count)
        {
            BytesRead += count;
            Array.Fill(buffer, (byte)'x', offset, count);
            return count;
        }

        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            BytesRead += buffer.Length;
            buffer.Span.Fill((byte)'x');
            return ValueTask.FromResult(buffer.Length);
        }

        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => 0; set => throw new NotSupportedException(); }
        public override void Flush() { }
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }
}
