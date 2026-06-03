using System.Net;
using System.Text;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PhotoPrint.API.Configuration;
using PhotoPrint.API.Services.Invoicing.Anaf;
using PhotoPrint.Tests.Unit.Services.Sameday;     // reuse ScriptedHttpMessageHandler

namespace PhotoPrint.Tests.Unit.Services.Invoicing.Anaf;

/// <summary>
/// Wire-protocol tests for <see cref="AnafSpvClient"/>. The ANAF SPV API
/// uses XML response bodies; this class verifies our parser correctly
/// maps each wire shape to the domain types.
/// </summary>
public class AnafSpvClientTests
{
    private static AnafSpvClient Build(ScriptedHttpMessageHandler script, DateTimeOffset now)
    {
        var http = new HttpClient(script)
        {
            BaseAddress = new Uri("https://anaf-test/api/"),
        };
        return new AnafSpvClient(
            http,
            Options.Create(new AnafSettings { Enabled = true, BaseUrl = "https://anaf-test/api/" }),
            new FakeClock(now),
            new LoggerFactory().CreateLogger<AnafSpvClient>());
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

    private sealed class FakeClock : TimeProvider
    {
        private readonly DateTimeOffset _now;
        public FakeClock(DateTimeOffset now) { _now = now; }
        public override DateTimeOffset GetUtcNow() => _now;
    }
}
