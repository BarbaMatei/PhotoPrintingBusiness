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

    private static DefaultHttpContext CreateContext(bool isDev = false)
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        context.Items["CorrelationId"] = "test-correlation-id";

        var envMock = new Mock<IHostEnvironment>();
        envMock.Setup(e => e.EnvironmentName)
               .Returns(isDev ? Environments.Development : Environments.Production);

        var servicesMock = new Mock<IServiceProvider>();
        servicesMock.Setup(s => s.GetService(typeof(IHostEnvironment)))
                    .Returns(envMock.Object);
        context.RequestServices = servicesMock.Object;

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
        var context = CreateContext(isDev: false);
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
    public async Task InvokeAsync_UnknownException_Returns500WithExceptionDetailInDevelopment()
    {
        // Arrange
        _envMock.Setup(e => e.EnvironmentName).Returns(Environments.Development);
        var sut = CreateSut();
        var context = CreateContext(isDev: true);
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
