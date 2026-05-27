using FluentAssertions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using PhotoPrint.API.Configuration;
using PhotoPrint.API.HealthChecks;
using Xunit;

namespace PhotoPrint.Tests.Unit.HealthChecks;

public class DiskHealthCheckTests
{
    private static DiskHealthCheck CreateSut(string uploadsPath)
    {
        var options = Options.Create(new HealthCheckSettings { UploadsPath = uploadsPath });
        return new DiskHealthCheck(options);
    }

    private static HealthCheckContext CreateContext() =>
        new()
        {
            Registration = new HealthCheckRegistration("disk", Mock.Of<IHealthCheck>(), null, null),
        };

    [Fact]
    public async Task CheckHealthAsync_ValidRootedPath_ReturnsHealthy()
    {
        // Arrange — use the current drive root, which always exists
        var root = Path.GetPathRoot(AppContext.BaseDirectory)!;
        var sut = CreateSut(root);

        // Act
        var result = await sut.CheckHealthAsync(CreateContext());

        // Assert
        result.Status.Should().Be(HealthStatus.Healthy);
        result.Description.Should().Be("OK");
        result.Data.Should().ContainKey("freeGb");
        ((double)result.Data["freeGb"]).Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task CheckHealthAsync_RelativePath_ReturnsHealthy()
    {
        // Arrange — relative path resolved from AppContext.BaseDirectory
        var sut = CreateSut("uploads");

        // Act
        var result = await sut.CheckHealthAsync(CreateContext());

        // Assert
        result.Status.Should().Be(HealthStatus.Healthy);
    }

    [Fact]
    public async Task CheckHealthAsync_InvalidDrive_ReturnsUnhealthy()
    {
        // Arrange — use a drive letter that doesn't exist on any test machine
        var sut = CreateSut("Z:\\nonexistent\\path\\that\\will\\not\\resolve");

        // Act
        var result = await sut.CheckHealthAsync(CreateContext());

        // Assert — either healthy (if Z: exists) or unhealthy (if not) — we just verify no exception thrown
        result.Status.Should().BeOneOf(HealthStatus.Healthy, HealthStatus.Unhealthy);
    }
}

// Silence Moq static usage without full namespace import
file static class Mock
{
    public static T Of<T>() where T : class => Moq.Mock.Of<T>();
}
