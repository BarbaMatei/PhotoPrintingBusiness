using System.Net;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using PhotoPrint.API.Services.Invoicing.Anaf;
using PhotoPrint.Tests.Unit.Services.Sameday;     // reuse ScriptedHttpMessageHandler

namespace PhotoPrint.Tests.Unit.Services.Invoicing.Anaf;

/// <summary>
/// 401 triggers token-invalidate + retry once,
/// a second 401 raises <see cref="AnafAuthException"/>. The retry uses the
/// FRESH token, not the original. Polly is NOT involved in the 401 path.
/// </summary>
public class AnafAuthHandlerTests
{
    private static HttpClient Build(
        Mock<IAnafTokenProvider> tokenProvider,
        ScriptedHttpMessageHandler script)
    {
        var sut = new AnafAuthHandler(
            tokenProvider.Object,
            new LoggerFactory().CreateLogger<AnafAuthHandler>())
        {
            InnerHandler = script,
        };
        return new HttpClient(sut)
        {
            BaseAddress = new Uri("https://anaf-test/"),
        };
    }

    [Fact]
    public async Task Bearer_attached_to_every_call()
    {
        var tokenProvider = new Mock<IAnafTokenProvider>();
        tokenProvider
            .Setup(t => t.GetAccessTokenAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync("TOKEN-ABC");

        var script = new ScriptedHttpMessageHandler(
            _ => ScriptedHttpMessageHandler.Json(HttpStatusCode.OK, "<header />"));

        var http = Build(tokenProvider, script);
        await http.PostAsync("upload?standard=UBL", new StringContent("<x/>"));

        script.Recorded.Should().HaveCount(1);
        script.Recorded[0].Authorization!.Scheme.Should().Be("Bearer");
        script.Recorded[0].Authorization!.Parameter.Should().Be("TOKEN-ABC");
    }

    [Fact]
    public async Task On_401_invalidates_then_retries_with_fresh_token()
    {
        var tokenProvider = new Mock<IAnafTokenProvider>();
        var seq = 0;
        tokenProvider
            .Setup(t => t.GetAccessTokenAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => { seq++; return $"TOK{seq}"; });

        var script = new ScriptedHttpMessageHandler(
            _ => ScriptedHttpMessageHandler.Empty(HttpStatusCode.Unauthorized),
            _ => ScriptedHttpMessageHandler.Json(HttpStatusCode.OK, "<header />"));

        var http = Build(tokenProvider, script);
        var response = await http.PostAsync("upload?standard=UBL", new StringContent("<x/>"));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        script.Recorded.Should().HaveCount(2);
        script.Recorded[0].Authorization!.Parameter.Should().Be("TOK1");
        script.Recorded[1].Authorization!.Parameter.Should().Be("TOK2");
        tokenProvider.Verify(t => t.Invalidate(), Times.Once);
    }

    [Fact]
    public async Task Second_401_throws_AnafAuthException()
    {
        var tokenProvider = new Mock<IAnafTokenProvider>();
        tokenProvider
            .Setup(t => t.GetAccessTokenAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync("TOKEN");

        var script = new ScriptedHttpMessageHandler(
            _ => ScriptedHttpMessageHandler.Empty(HttpStatusCode.Unauthorized),
            _ => ScriptedHttpMessageHandler.Empty(HttpStatusCode.Unauthorized));

        var http = Build(tokenProvider, script);
        var act = () => http.PostAsync("upload?standard=UBL", new StringContent("<x/>"));

        await act.Should().ThrowAsync<AnafAuthException>();
        script.Recorded.Should().HaveCount(2);
    }
}
