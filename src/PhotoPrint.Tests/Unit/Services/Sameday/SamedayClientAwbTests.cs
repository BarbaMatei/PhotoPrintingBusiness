using System.Net;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using PhotoPrint.API.Exceptions;
using PhotoPrint.API.Services.Sameday;

namespace PhotoPrint.Tests.Unit.Services.Sameday;

/// <summary>
/// Full error-matrix for <see cref="SamedayClient.CreateAwbAsync"/>.
/// Mirrors <see cref="SamedayClientTests"/>' coverage of
/// <c>AuthenticateAsync</c> from bolt 036.
/// </summary>
public class SamedayClientAwbTests
{
    private static SamedayClient Build(ScriptedHttpMessageHandler script)
    {
        var http = new HttpClient(script) { BaseAddress = new Uri("https://sameday-test/") };
        return new SamedayClient(http, new LoggerFactory().CreateLogger<SamedayClient>());
    }

    private static readonly AwbCreationRequest Request = new(
        PickupPointId: "PP1",
        OrderNumber: "FT-1", ServiceId: 7, LockerSamedayId: null,
        RecipientName: "Alice", RecipientPhone: "+40712345678",
        RecipientAddress: "Str. Test 10", RecipientCity: "Cluj",
        RecipientCounty: "Cluj", RecipientPostalCode: "400000",
        ParcelWeightKg: 0.2m, ParcelCount: 1, CodAmountRon: 0m,
        Observations: "Order #FT-1");

    [Fact]
    public async Task Posts_request_body_with_recipient_and_weight()
    {
        var script = new ScriptedHttpMessageHandler(
            _ => ScriptedHttpMessageHandler.Json(
                HttpStatusCode.OK,
                "{\"awbNumber\":\"RO123\",\"awbCost\":18.50,\"pdfLink\":\"https://x/y.pdf\"}"));

        var sut = Build(script);
        await sut.CreateAwbAsync(Request);

        script.Recorded.Should().HaveCount(1);
        var body = script.Recorded[0].BodyText();
        body.Should().Contain("Alice");
        // JSON escapes '+' as +, so check both forms.
        (body.Contains("+40712345678") || body.Contains("\\u002B40712345678"))
            .Should().BeTrue();
        body.Should().Contain("PP1");
        body.Should().Contain("\"packageWeight\":0.2");
    }

    [Fact]
    public async Task Uses_the_order_number_as_the_vendor_reference_not_the_pickup_point()
    {
        var script = new ScriptedHttpMessageHandler(
            _ => ScriptedHttpMessageHandler.Json(
                HttpStatusCode.OK,
                "{\"awbNumber\":\"RO123\",\"awbCost\":1,\"pdfLink\":\"https://x/y.pdf\"}"));
        var sut = Build(script);

        var req = Request with { OrderNumber = "FT-20260001", ServiceId = 7, LockerSamedayId = "LCK-42" };
        await sut.CreateAwbAsync(req);

        var body = script.Recorded[0].BodyText();
        // D1: the idempotency key must be the per-order number, never the shop-wide pickup point.
        body.Should().Contain("\"clientInternalReference\":\"FT-20260001\"");
        body.Should().NotContain("\"clientInternalReference\":\"PP1\"");
        // D5: the delivery-type service id and the locker OOH id are on the wire.
        body.Should().Contain("\"service\":7");
        body.Should().Contain("\"lockerLastMile\":\"LCK-42\"");
    }

    [Fact]
    public async Task Returns_AwbCreationResult_on_200()
    {
        var script = new ScriptedHttpMessageHandler(
            _ => ScriptedHttpMessageHandler.Json(
                HttpStatusCode.OK,
                "{\"awbNumber\":\"RO12345678\",\"awbCost\":18.50,\"pdfLink\":\"https://sameday/labels/x.pdf\"}"));

        var sut = Build(script);
        var result = await sut.CreateAwbAsync(Request);

        result.AwbNumber.Should().Be("RO12345678");
        result.LabelUrl.Should().Be("https://sameday/labels/x.pdf");
        result.CalculatedPrice.Should().Be(18.50m);
    }

    [Fact]
    public async Task Throws_SamedayAuthException_on_401()
    {
        var script = new ScriptedHttpMessageHandler(
            _ => ScriptedHttpMessageHandler.Empty(HttpStatusCode.Unauthorized));

        var sut = Build(script);
        var act = () => sut.CreateAwbAsync(Request);
        var ex = await act.Should().ThrowAsync<SamedayAuthException>();
        ex.Which.Endpoint.Should().Contain("/api/awb");
    }

    [Theory]
    [InlineData(HttpStatusCode.InternalServerError)]
    [InlineData(HttpStatusCode.BadGateway)]
    [InlineData(HttpStatusCode.RequestTimeout)]
    [InlineData(HttpStatusCode.TooManyRequests)] // 429 surviving retries is transient, not a permanent give-up
    public async Task Throws_SamedayUnreachableException_on_5xx_408_and_429(HttpStatusCode status)
    {
        var script = new ScriptedHttpMessageHandler(
            _ => ScriptedHttpMessageHandler.Empty(status));

        var sut = Build(script);
        var act = () => sut.CreateAwbAsync(Request);
        var ex = await act.Should().ThrowAsync<SamedayUnreachableException>();
        ex.Which.HttpStatus.Should().Be((int)status);
    }

    [Theory]
    [InlineData(HttpStatusCode.BadRequest)]
    [InlineData(HttpStatusCode.UnprocessableEntity)]
    public async Task Throws_SamedayValidationException_on_other_4xx(HttpStatusCode status)
    {
        var script = new ScriptedHttpMessageHandler(
            _ => ScriptedHttpMessageHandler.Json(status, "{\"error\":\"weight\"}"));

        var sut = Build(script);
        var act = () => sut.CreateAwbAsync(Request);
        var ex = await act.Should().ThrowAsync<SamedayValidationException>();
        ex.Which.HttpStatus.Should().Be((int)status);
    }

    [Fact]
    public async Task Throws_SamedayProtocolException_when_response_missing_awbNumber()
    {
        var script = new ScriptedHttpMessageHandler(
            _ => ScriptedHttpMessageHandler.Json(HttpStatusCode.OK, "{\"pdfLink\":\"https://x/y.pdf\"}"));

        var sut = Build(script);
        var act = () => sut.CreateAwbAsync(Request);
        await act.Should().ThrowAsync<SamedayProtocolException>();
    }

    [Fact]
    public async Task Throws_SamedayProtocolException_when_response_missing_pdfLink()
    {
        var script = new ScriptedHttpMessageHandler(
            _ => ScriptedHttpMessageHandler.Json(HttpStatusCode.OK, "{\"awbNumber\":\"RO123\"}"));

        var sut = Build(script);
        var act = () => sut.CreateAwbAsync(Request);
        await act.Should().ThrowAsync<SamedayProtocolException>();
    }

    [Fact]
    public async Task Throws_SamedayUnreachableException_on_HttpRequestException()
    {
        var script = new ScriptedHttpMessageHandler(
            _ => throw new HttpRequestException("DNS failed"));

        var sut = Build(script);
        var act = () => sut.CreateAwbAsync(Request);
        await act.Should().ThrowAsync<SamedayUnreachableException>();
    }
}
