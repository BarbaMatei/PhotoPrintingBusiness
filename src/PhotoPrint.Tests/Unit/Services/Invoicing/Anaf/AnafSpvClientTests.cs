using System.Net;
using System.Text;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PhotoPrint.API.Configuration;
using PhotoPrint.API.Models;
using PhotoPrint.API.Services.Invoicing.Anaf;
using PhotoPrint.Tests.Helpers;
using PhotoPrint.Tests.Unit.Services.Sameday;     // reuse ScriptedHttpMessageHandler

namespace PhotoPrint.Tests.Unit.Services.Invoicing.Anaf;

/// <summary>
/// Wire-protocol tests for <see cref="AnafSpvClient"/>. The ANAF SPV API
/// uses XML response bodies; this class verifies our parser correctly
/// maps each wire shape to the domain types.
/// </summary>
public class AnafSpvClientTests
{
    private static AnafSpvClient Build(ScriptedHttpMessageHandler script, DateTimeOffset now, LogCapture? logCapture = null)
    {
        var http = new HttpClient(script)
        {
            BaseAddress = new Uri("https://anaf-test/api/"),
        };
        return new AnafSpvClient(
            http,
            Options.Create(new AnafSettings { Enabled = true, BaseUrl = "https://anaf-test/api/" }),
            new FakeClock(now),
            logCapture is null
                ? new LoggerFactory().CreateLogger<AnafSpvClient>()
                : logCapture.LoggerFor<AnafSpvClient>());
    }

    [Fact]
    public async Task Upload_returns_upload_id_from_index_incarcare_attribute()
    {
        var now = new DateTimeOffset(2026, 6, 3, 12, 0, 0, TimeSpan.Zero);
        var script = new ScriptedHttpMessageHandler(_ => ScriptedHttpMessageHandler.Json(
            HttpStatusCode.OK,
            "<header index_incarcare=\"index-42\" data_incarcare=\"2026-06-03 11:30:00\" />"));

        var sut = Build(script, now);
        var result = await sut.UploadAsync(Encoding.UTF8.GetBytes("<Invoice />"));

        result.UploadId.Should().Be("index-42");
        result.SubmittedAt.Should().Be(now);
    }

    // A wrong base URL or a token without the scope is our misconfiguration, not ANAF judging the
    // document: parking every invoice for it costs an admin retry per row.
    [Theory]
    [InlineData(HttpStatusCode.NotFound)]
    [InlineData(HttpStatusCode.Forbidden)]
    [InlineData(HttpStatusCode.MethodNotAllowed)]
    public async Task Upload_treats_a_misconfiguration_status_as_unreachable_not_content_rejected(
        HttpStatusCode status)
    {
        var script = new ScriptedHttpMessageHandler(_ => new HttpResponseMessage(status));
        var sut = Build(script, DateTimeOffset.UtcNow);

        var act = () => sut.UploadAsync(Encoding.UTF8.GetBytes("<Invoice />"));

        await act.Should().ThrowAsync<AnafUnreachableException>();
    }

    [Theory]
    [InlineData(HttpStatusCode.BadRequest)]
    [InlineData(HttpStatusCode.UnprocessableEntity)]
    public async Task Upload_treats_a_document_refusal_as_content_rejected(HttpStatusCode status)
    {
        var script = new ScriptedHttpMessageHandler(_ => new HttpResponseMessage(status));
        var sut = Build(script, DateTimeOffset.UtcNow);

        var act = () => sut.UploadAsync(Encoding.UTF8.GetBytes("<Invoice />"));

        await act.Should().ThrowAsync<AnafContentRejectedException>();
    }

    // A proxy error page can parse as XML; storing an over-long id fails the status write, not the upload.
    [Fact]
    public async Task Upload_throws_when_index_incarcare_is_wider_than_the_column_that_stores_it()
    {
        var tooLong = new string('9', Invoice.AnafUploadIdMaxLength + 1);
        var script = new ScriptedHttpMessageHandler(_ => ScriptedHttpMessageHandler.Json(
            HttpStatusCode.OK,
            $"<header index_incarcare=\"{tooLong}\" data_incarcare=\"2026-06-03 11:30:00\" />"));

        var sut = Build(script, DateTimeOffset.UtcNow);

        var act = () => sut.UploadAsync(Encoding.UTF8.GetBytes("<Invoice />"));
        await act.Should().ThrowAsync<AnafUploadException>();
    }

    [Fact]
    public async Task Upload_throws_when_response_contains_errors_element()
    {
        var script = new ScriptedHttpMessageHandler(_ => ScriptedHttpMessageHandler.Json(
            HttpStatusCode.OK,
            "<header><Errors errorMessage=\"CUI invalid\" /></header>"));

        var sut = Build(script, DateTimeOffset.UtcNow);

        var act = () => sut.UploadAsync(Encoding.UTF8.GetBytes("<Invoice />"));
        await act.Should().ThrowAsync<AnafUploadException>()
            .WithMessage("*CUI invalid*");
    }

    [Fact]
    public async Task Upload_throws_unreachable_on_5xx()
    {
        var script = new ScriptedHttpMessageHandler(
            _ => ScriptedHttpMessageHandler.Empty(HttpStatusCode.InternalServerError));

        var sut = Build(script, DateTimeOffset.UtcNow);

        var act = () => sut.UploadAsync(Encoding.UTF8.GetBytes("<Invoice />"));
        var ex = await act.Should().ThrowAsync<AnafUnreachableException>();
        ex.Which.HttpStatus.Should().Be(500);
    }

    [Theory]
    [InlineData("ok",            AnafExternalStatus.Validated)]
    [InlineData("nok",           AnafExternalStatus.Rejected)]
    [InlineData("in prelucrare", AnafExternalStatus.InProgress)]
    [InlineData("garbled",       AnafExternalStatus.Unknown)]
    public async Task GetStatus_maps_stare_attribute_to_external_status(string wire, AnafExternalStatus expected)
    {
        var script = new ScriptedHttpMessageHandler(_ => ScriptedHttpMessageHandler.Json(
            HttpStatusCode.OK,
            $"<header stare=\"{wire}\" />"));

        var sut = Build(script, DateTimeOffset.UtcNow);
        var result = await sut.GetStatusAsync("upload-99");

        result.Status.Should().Be(expected);
    }

    [Fact]
    public async Task GetStatus_unrecognized_stare_logs_the_raw_value_at_warning()
    {
        var logs = new LogCapture();
        var script = new ScriptedHttpMessageHandler(_ => ScriptedHttpMessageHandler.Json(
            HttpStatusCode.OK,
            "<header stare=\"garbled\" />"));

        var sut = Build(script, DateTimeOffset.UtcNow, logs);
        await sut.GetStatusAsync("upload-99");

        logs.Records.Should().ContainSingle(
            r => r.Level == LogLevel.Warning &&
                 r.Message.StartsWith("anaf.spv.status-unrecognized", StringComparison.Ordinal) &&
                 r.Message.Contains("garbled"));
    }

    [Fact]
    public async Task GetStatus_rejected_extracts_error_message_from_errors_element()
    {
        var script = new ScriptedHttpMessageHandler(_ => ScriptedHttpMessageHandler.Json(
            HttpStatusCode.OK,
            "<header stare=\"nok\"><Errors errorMessage=\"date incorecte\" /></header>"));

        var sut = Build(script, DateTimeOffset.UtcNow);
        var result = await sut.GetStatusAsync("upload-99");

        result.Status.Should().Be(AnafExternalStatus.Rejected);
        result.ErrorMessage.Should().Be("date incorecte");
    }

    [Fact]
    public async Task GetStatus_url_includes_upload_id_in_query_string()
    {
        var script = new ScriptedHttpMessageHandler(
            _ => ScriptedHttpMessageHandler.Json(HttpStatusCode.OK, "<header stare=\"in prelucrare\" />"));

        var sut = Build(script, DateTimeOffset.UtcNow);
        await sut.GetStatusAsync("complex/id 42");   // url-encode required

        script.Recorded.Should().HaveCount(1);
        script.Recorded[0].Uri!.Query.Should().Contain("id_incarcare=complex%2Fid%2042");
    }

    // ── Timeout versus shutdown: the classifier the worker's claim handling reads ──

    private static AnafSpvClient BuildWithHandler(HttpMessageHandler handler, TimeSpan? timeout = null)
    {
        var http = new HttpClient(handler) { BaseAddress = new Uri("https://anaf-test/api/") };
        if (timeout is not null) http.Timeout = timeout.Value;
        return new AnafSpvClient(
            http,
            Options.Create(new AnafSettings { Enabled = true, BaseUrl = "https://anaf-test/api/" }),
            new FakeClock(DateTimeOffset.UtcNow),
            new LoggerFactory().CreateLogger<AnafSpvClient>());
    }

    [Fact]
    public async Task Upload_when_the_client_timeout_fires_is_classified_as_an_unknown_outcome()
    {
        var sut = BuildWithHandler(new BlockingHandler(), TimeSpan.FromMilliseconds(200));

        var act = () => sut.UploadAsync(Encoding.UTF8.GetBytes("<Invoice />"));

        await act.Should().ThrowAsync<AnafUploadTimeoutException>(
            "ANAF may have taken the invoice and merely answered too slowly");
    }

    [Fact]
    public async Task Upload_when_the_caller_cancels_propagates_the_shutdown_instead()
    {
        using var cts = new CancellationTokenSource();
        var sut = BuildWithHandler(new BlockingHandler(), TimeSpan.FromMinutes(5));

        var pending = sut.UploadAsync(Encoding.UTF8.GetBytes("<Invoice />"), cts.Token);
        await cts.CancelAsync();

        var act = async () => await pending;
        await act.Should().ThrowAsync<OperationCanceledException>(
            "a deploy must stop the worker, not leave every in-flight invoice recorded as outcome-unknown");
    }

    [Fact]
    public async Task GetStatus_when_the_client_timeout_fires_is_classified_as_unreachable()
    {
        var sut = BuildWithHandler(new BlockingHandler(), TimeSpan.FromMilliseconds(200));

        var act = () => sut.GetStatusAsync("upload-1");

        await act.Should().ThrowAsync<AnafUnreachableException>(
            "polling changes nothing at ANAF, so a slow poll is an outage with no ambiguity to preserve");
    }

    [Fact]
    public async Task GetStatus_when_the_caller_cancels_propagates_the_shutdown_instead()
    {
        using var cts = new CancellationTokenSource();
        var sut = BuildWithHandler(new BlockingHandler(), TimeSpan.FromMinutes(5));

        var pending = sut.GetStatusAsync("upload-1", cts.Token);
        await cts.CancelAsync();

        var act = async () => await pending;
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    public enum WireOutcome { Transport, NotXml, EmptyBody, Forbidden, ServerError, Unauthorized, MissingIndex, BodyErrors }

    // Anything outside these four lands in the batch loop's generic catch, which writes no LastError and releases no claim.
    [Theory]
    [InlineData(WireOutcome.Transport)]
    [InlineData(WireOutcome.NotXml)]
    [InlineData(WireOutcome.EmptyBody)]
    [InlineData(WireOutcome.Forbidden)]
    [InlineData(WireOutcome.ServerError)]
    [InlineData(WireOutcome.Unauthorized)]
    [InlineData(WireOutcome.MissingIndex)]
    [InlineData(WireOutcome.BodyErrors)]
    public async Task Upload_never_leaks_an_unclassified_exception(WireOutcome outcome)
    {
        var sut = BuildWithHandler(HandlerFor(outcome));

        Exception? thrown = null;
        try { await sut.UploadAsync(Encoding.UTF8.GetBytes("<Invoice />")); }
        catch (Exception ex) { thrown = ex; }

        thrown.Should().NotBeNull();
        new[]
        {
            typeof(AnafUploadException), typeof(AnafUnreachableException),
            typeof(AnafAuthException), typeof(AnafUploadTimeoutException),
            typeof(AnafContentRejectedException),
        }.Should().Contain(thrown!.GetType());
    }

    private static HttpMessageHandler HandlerFor(WireOutcome outcome) => outcome switch
    {
        WireOutcome.Transport => new ScriptedHttpMessageHandler(
            _ => throw new HttpRequestException("connection reset")),
        WireOutcome.NotXml => new ScriptedHttpMessageHandler(
            _ => ScriptedHttpMessageHandler.Json(HttpStatusCode.OK, "definitely not xml")),
        WireOutcome.EmptyBody => new ScriptedHttpMessageHandler(
            _ => ScriptedHttpMessageHandler.Json(HttpStatusCode.OK, "")),
        WireOutcome.Forbidden => new ScriptedHttpMessageHandler(
            _ => ScriptedHttpMessageHandler.Empty(HttpStatusCode.Forbidden)),
        WireOutcome.ServerError => new ScriptedHttpMessageHandler(
            _ => ScriptedHttpMessageHandler.Empty(HttpStatusCode.ServiceUnavailable)),
        WireOutcome.Unauthorized => new ScriptedHttpMessageHandler(
            _ => ScriptedHttpMessageHandler.Empty(HttpStatusCode.Unauthorized)),
        WireOutcome.MissingIndex => new ScriptedHttpMessageHandler(
            _ => ScriptedHttpMessageHandler.Json(HttpStatusCode.OK, "<header data_incarcare=\"2026-06-03 11:30:00\" />")),
        WireOutcome.BodyErrors => new ScriptedHttpMessageHandler(
            _ => ScriptedHttpMessageHandler.Json(HttpStatusCode.OK, "<header><Errors errorMessage=\"CUI invalid\" /></header>")),
        _ => throw new ArgumentOutOfRangeException(nameof(outcome)),
    };

    private sealed class BlockingHandler : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            return new HttpResponseMessage(HttpStatusCode.OK);
        }
    }

    private sealed class FakeClock : TimeProvider
    {
        private readonly DateTimeOffset _now;
        public FakeClock(DateTimeOffset now) { _now = now; }
        public override DateTimeOffset GetUtcNow() => _now;
    }
}
