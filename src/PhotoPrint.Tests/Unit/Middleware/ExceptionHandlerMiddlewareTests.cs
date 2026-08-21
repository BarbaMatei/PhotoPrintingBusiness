using System.Linq;
using System.Net;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moq;
using PhotoPrint.API.Exceptions;
using PhotoPrint.API.Middleware;
using Xunit;

namespace PhotoPrint.Tests.Unit.Middleware;

public class ExceptionHandlerMiddlewareTests
{
    private readonly Mock<ILogger<ExceptionHandlerMiddleware>> _loggerMock = new();
    private readonly Mock<IHostEnvironment> _envMock = new();

    private ExceptionHandlerMiddleware CreateSut()
        => new(_loggerMock.Object, _envMock.Object);

    // The middleware now uses its injected IHostEnvironment
    // (_envMock) for the dev/prod shape decision, not context.RequestServices — so the
    // context no longer needs a service-locator env stub. Each test sets _envMock directly.
    private static DefaultHttpContext CreateContext()
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        context.Items["CorrelationId"] = "test-correlation-id";
        return context;
    }

    private static async Task<JsonDocument> ReadResponseBodyAsync(HttpContext context)
    {
        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var body = await new StreamReader(context.Response.Body).ReadToEndAsync();
        return JsonDocument.Parse(body);
    }

    [Theory]
    [InlineData(typeof(NotFoundException), 404, "Not Found")]
    [InlineData(typeof(ConflictException), 409, "Conflict")]
    [InlineData(typeof(ForbiddenException), 403, "Forbidden")]
    [InlineData(typeof(UnauthorizedException), 401, "Unauthorized")]
    public async Task InvokeAsync_KnownException_ReturnsMappedStatusCodeAndProblemDetails(
        Type exceptionType, int expectedStatus, string expectedTitle)
    {
        // Arrange
        _envMock.Setup(e => e.EnvironmentName).Returns(Environments.Production);
        var sut = CreateSut();
        var context = CreateContext();
        var exception = (Exception)Activator.CreateInstance(exceptionType, "Test error message")!;
        RequestDelegate next = _ => throw exception;

        // Act
        await sut.InvokeAsync(context, next);

        // Assert
        context.Response.StatusCode.Should().Be(expectedStatus);
        context.Response.ContentType.Should().Be("application/problem+json");

        var body = await ReadResponseBodyAsync(context);
        body.RootElement.GetProperty("status").GetInt32().Should().Be(expectedStatus);
        body.RootElement.GetProperty("title").GetString().Should().Be(expectedTitle);
        body.RootElement.GetProperty("detail").GetString().Should().Be("Test error message");
        body.RootElement.GetProperty("correlationId").GetString().Should().Be("test-correlation-id");
    }

    [Fact]
    public async Task InvokeAsync_UnknownException_Returns500WithGenericMessageInProduction()
    {
        // Arrange
        _envMock.Setup(e => e.EnvironmentName).Returns(Environments.Production);
        var sut = CreateSut();
        var context = CreateContext();
        RequestDelegate next = _ => throw new InvalidOperationException("Secret internal detail");

        // Act
        await sut.InvokeAsync(context, next);

        // Assert
        context.Response.StatusCode.Should().Be(500);
        var body = await ReadResponseBodyAsync(context);
        body.RootElement.GetProperty("detail").GetString()
            .Should().Be("A apărut o eroare neașteptată. Încearcă din nou.");
        body.RootElement.GetProperty("detail").GetString()
            .Should().NotContain("Secret internal detail");
    }

    [Fact]
    public async Task InvokeAsync_ImageAllocationBackstopTripped_Returns422NotRaw500()
    {
        // L13: a bomb whose header understated its decode size slips the
        // pixel-area check, then the decode trips the 512 MB allocator backstop (Program.cs)
        // throwing ImageSharp's InvalidMemoryOperationException — not an ImageFormatException —
        // so it surfaced as a raw 500. Map it to 422.
        _envMock.Setup(e => e.EnvironmentName).Returns(Environments.Production);
        var sut = CreateSut();
        var context = CreateContext();
        RequestDelegate next = _ =>
            throw new SixLabors.ImageSharp.Memory.InvalidMemoryOperationException("allocation exceeded");

        await sut.InvokeAsync(context, next);

        context.Response.StatusCode.Should().Be(422);
        var body = await ReadResponseBodyAsync(context);
        body.RootElement.GetProperty("status").GetInt32().Should().Be(422);
    }

    [Fact]
    public async Task InvokeAsync_MappedServerError_LogsAtErrorWithTheException()
    {
        var sut = CreateSut();
        var context = CreateContext();
        var boom = new BadGatewayException("upstream is down");
        RequestDelegate next = _ => throw boom;

        await sut.InvokeAsync(context, next);

        _loggerMock.Verify(
            l => l.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains("BadGatewayException")),
                boom,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_UnmappedServerError_LogsAtErrorWithTheException()
    {
        _envMock.Setup(e => e.EnvironmentName).Returns(Environments.Production);
        var sut = CreateSut();
        var context = CreateContext();
        var boom = new InvalidOperationException("Secret internal detail");
        RequestDelegate next = _ => throw boom;

        await sut.InvokeAsync(context, next);

        _loggerMock.Verify(
            l => l.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains("InvalidOperationException")),
                boom,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once,
            "every unmapped exception becomes a 500, and §13.8 reconciles Error-level logs "
                + "against Sentry — this is the branch most 500s take");
    }

    [Fact]
    public async Task InvokeAsync_MappedClientError_StaysAtWarning()
    {
        var sut = CreateSut();
        var context = CreateContext();
        RequestDelegate next = _ => throw new NotFoundException("no such order");

        await sut.InvokeAsync(context, next);

        _loggerMock.Verify(
            l => l.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Never,
            "a 404 is an expected business outcome, not a server error");
    }

    [Fact]
    public async Task InvokeAsync_ImageAllocationBackstopTripped_EmitsReservedBombEvent()
    {
        // A bomb that under-reports its dimensions passes the pixel guard but
        // trips the 512 MB allocator backstop (InvalidMemoryOperationException). It must emit the
        // SAME reserved `uploads.decompression_bomb.rejected` event ops alert on — otherwise the
        // bombs that evade the primary guard show up only as a generic "Handled exception" warning.
        _envMock.Setup(e => e.EnvironmentName).Returns(Environments.Production);
        var sut = CreateSut();
        var context = CreateContext();
        RequestDelegate next = _ =>
            throw new SixLabors.ImageSharp.Memory.InvalidMemoryOperationException("allocation exceeded");

        await sut.InvokeAsync(context, next);

        _loggerMock.Verify(
            l => l.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, _) =>
                    v.ToString()!.Contains("uploads.decompression_bomb.rejected") &&
                    v.ToString()!.Contains("allocator_backstop")),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_UnknownException_Returns500WithExceptionDetailInDevelopment()
    {
        // Arrange
        _envMock.Setup(e => e.EnvironmentName).Returns(Environments.Development);
        var sut = CreateSut();
        var context = CreateContext();
        RequestDelegate next = _ => throw new InvalidOperationException("Secret internal detail");

        // Act
        await sut.InvokeAsync(context, next);

        // Assert
        context.Response.StatusCode.Should().Be(500);
        var body = await ReadResponseBodyAsync(context);
        body.RootElement.GetProperty("detail").GetString()
            .Should().Be("Secret internal detail");
    }

    [Fact]
    public async Task InvokeAsync_NoException_CallsNextAndDoesNotAlterResponse()
    {
        // Arrange
        _envMock.Setup(e => e.EnvironmentName).Returns(Environments.Production);
        var sut = CreateSut();
        var context = CreateContext();
        var nextCalled = false;
        RequestDelegate next = _ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        };

        // Act
        await sut.InvokeAsync(context, next);

        // Assert
        nextCalled.Should().BeTrue();
        context.Response.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task InvokeAsync_IdempotencyConflict_IncludesDivergentFields_InProduction()
    {
        // Arrange
        _envMock.Setup(e => e.EnvironmentName).Returns(Environments.Production);
        var sut = CreateSut();
        var context = CreateContext();
        RequestDelegate next = _ =>
            throw new IdempotencyConflictException(new[] { "paymentProcessor", "totalRon" });

        // Act
        await sut.InvokeAsync(context, next);

        // Assert
        context.Response.StatusCode.Should().Be(409);
        var body = await ReadResponseBodyAsync(context);
        var fields = body.RootElement.GetProperty("divergentFields")
            .EnumerateArray().Select(e => e.GetString()).ToArray();
        fields.Should().BeEquivalentTo("paymentProcessor", "totalRon");
    }

    [Fact]
    public async Task InvokeAsync_IdempotencyConflict_IncludesDivergentFields_InDevelopment()
    {
        // The documented 409 contract field must be present even in
        // Development, where the response uses the richer diagnostic shape. A FE developer
        // building against the dev API otherwise never sees `divergentFields`.
        _envMock.Setup(e => e.EnvironmentName).Returns(Environments.Development);
        var sut = CreateSut();
        var context = CreateContext();
        RequestDelegate next = _ =>
            throw new IdempotencyConflictException(new[] { "paymentProcessor", "totalRon" });

        // Act
        await sut.InvokeAsync(context, next);

        // Assert
        context.Response.StatusCode.Should().Be(409);
        var body = await ReadResponseBodyAsync(context);
        body.RootElement.TryGetProperty("divergentFields", out var divergent)
            .Should().BeTrue("the 409 contract field must be present in Development too (OBS-1)");
        var fields = divergent.EnumerateArray().Select(e => e.GetString()).ToArray();
        fields.Should().BeEquivalentTo("paymentProcessor", "totalRon");
    }

    [Fact]
    public async Task InvokeAsync_IdempotencyConflict_EmitsReservedConflictLogEvent()
    {
        // Ddd-01 reserves `payments.idempotency.conflict` as a
        // distinct structured event; the middleware must emit it (not only the generic
        // "Handled exception" warning) so a conflict is independently observable.
        _envMock.Setup(e => e.EnvironmentName).Returns(Environments.Production);
        var sut = CreateSut();
        var context = CreateContext();
        RequestDelegate next = _ =>
            throw new IdempotencyConflictException(new[] { "paymentProcessor" });

        await sut.InvokeAsync(context, next);

        _loggerMock.Verify(
            l => l.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains("payments.idempotency.conflict")),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_IdempotencyKeyTaken_Returns409_AndEmitsReservedCrossTenantLogEvent()
    {
        // A cross-tenant key collision must map to 409 (it is a
        // ConflictException subtype) AND be logged as its own reserved event, distinct
        // from both the generic "Handled exception" warning and the same-caller
        // `payments.idempotency.conflict`. Before the fix the type was unmapped (→ 500)
        // and there was no cross-tenant marker at all.
        _envMock.Setup(e => e.EnvironmentName).Returns(Environments.Production);
        var sut = CreateSut();
        var context = CreateContext();
        RequestDelegate next = _ => throw new IdempotencyKeyTakenException();

        await sut.InvokeAsync(context, next);

        context.Response.StatusCode.Should().Be(409);

        _loggerMock.Verify(
            l => l.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, _) =>
                    v.ToString()!.Contains("payments.idempotency.cross-tenant-conflict")),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_ClientCancelled_LogsInformationEvent()
    {
        // The client-abort branch logged at Debug, which is below the
        // Information floor in every environment, so the signal was never emitted. It must log
        // at Information as a distinct `request.client_aborted` event.
        _envMock.Setup(e => e.EnvironmentName).Returns(Environments.Production);
        var sut = CreateSut();
        var context = CreateContext();
        context.RequestAborted = new CancellationToken(canceled: true);
        RequestDelegate next = _ => throw new OperationCanceledException();

        await sut.InvokeAsync(context, next);

        _loggerMock.Verify(
            l => l.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains("request.client_aborted")),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_DecompressionBomb_Returns422_AndEmitsReservedEvent()
    {
        // A rejected pixel bomb must map to 422 (it subclasses
        // UnprocessableEntityException) AND be logged as its own reserved event carrying the
        // offending dimensions, so ops can alert on a bomb spike distinctly from an ordinary
        // "unreadable image" 422.
        _envMock.Setup(e => e.EnvironmentName).Returns(Environments.Production);
        var sut = CreateSut();
        var context = CreateContext();
        // Distinct width/height so the assertion proves BOTH dimensions are carried, not just one.
        RequestDelegate next = _ =>
            throw new DecompressionBombException(31_000, 32_000, "Image dimensions exceed limits.");

        await sut.InvokeAsync(context, next);

        context.Response.StatusCode.Should().Be(422);

        // L12: assert the event carries the dimensions it exists to convey, not
        // just its name — dropping width/height (the whole point of the event) must fail this.
        _loggerMock.Verify(
            l => l.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, _) =>
                    v.ToString()!.Contains("uploads.decompression_bomb.rejected") &&
                    v.ToString()!.Contains("31000") &&
                    v.ToString()!.Contains("32000")),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Theory]
    [InlineData(413)]
    [InlineData(400)]
    public async Task InvokeAsync_KestrelRejectedRequest_AnswersItsOwnStatusAtWarning(int status)
    {
        _envMock.Setup(e => e.EnvironmentName).Returns(Environments.Production);
        var sut = CreateSut();
        var context = CreateContext();
        RequestDelegate next = _ => throw new BadHttpRequestException("Request body too large.", status);

        await sut.InvokeAsync(context, next);

        context.Response.StatusCode.Should().Be(status,
            "a rejected request body is the caller's fault, not a 500 that burns the error budget");
        _loggerMock.Verify(
            l => l.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Never);
    }

    [Fact]
    public async Task InvokeAsync_KestrelRejectionAskingForA5xx_KeepsTheServerErrorTreatment()
    {
        _envMock.Setup(e => e.EnvironmentName).Returns(Environments.Production);
        var sut = CreateSut();
        var context = CreateContext();
        RequestDelegate next = _ => throw new BadHttpRequestException("HTTP version not supported.", 505);

        await sut.InvokeAsync(context, next);

        context.Response.StatusCode.Should().Be(500,
            "the client-error shortcut must not become a way to skip the Sentry capture invariant");
        _loggerMock.Verify(
            l => l.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_KnownException_IncludesCorrelationIdFromContext()
    {
        // Arrange
        _envMock.Setup(e => e.EnvironmentName).Returns(Environments.Production);
        var sut = CreateSut();
        var context = CreateContext();
        var expectedId = "abc-correlation-123";
        context.Items["CorrelationId"] = expectedId;
        RequestDelegate next = _ => throw new NotFoundException("Resursa nu a fost găsită.");

        // Act
        await sut.InvokeAsync(context, next);

        // Assert
        var body = await ReadResponseBodyAsync(context);
        body.RootElement.GetProperty("correlationId").GetString().Should().Be(expectedId);
    }
}
