using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using Moq;
using PhotoPrint.API.Services;
using Xunit;

namespace PhotoPrint.Tests.Unit.Services;

public sealed class RazorTemplateServiceTests : IDisposable
{
    private readonly string _tempRoot;
    private readonly string _templatesDir;

    public RazorTemplateServiceTests()
    {
        _tempRoot = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        _templatesDir = Path.Combine(_tempRoot, "EmailTemplates");
        Directory.CreateDirectory(_templatesDir);
    }

    public void Dispose() => Directory.Delete(_tempRoot, recursive: true);

    private RazorTemplateService CreateSut()
    {
        var env = new Mock<IWebHostEnvironment>();
        env.Setup(e => e.ContentRootPath).Returns(_tempRoot);
        return new RazorTemplateService(env.Object, Mock.Of<ILogger<RazorTemplateService>>());
    }

    [Fact]
    public async Task RenderAsync_SimpleHtmlTemplate_ReturnsHtml()
    {
        // Arrange
        File.WriteAllText(
            Path.Combine(_templatesDir, "simple.cshtml"),
            "<p>Bun venit la FotoTipar!</p>");

        var sut = CreateSut();

        // Act
        var result = await sut.RenderAsync("simple", new { });

        // Assert
        result.Should().Contain("Bun venit la FotoTipar!");
    }

    [Fact]
    public async Task RenderAsync_TemplateWithModel_RendersModelProperties()
    {
        // Arrange — RazorLight dynamic templates require ExpandoObject for property access
        File.WriteAllText(
            Path.Combine(_templatesDir, "welcome.cshtml"),
            "<p>Salut, @Model.FirstName!</p>");

        var sut = CreateSut();

        dynamic model = new System.Dynamic.ExpandoObject();
        model.FirstName = "Ion";

        // Act
        var result = await sut.RenderAsync<dynamic>("welcome", model);

        // Assert
        ((string)result).Should().Contain("Salut, Ion!");
    }

    [Fact]
    public async Task RenderAsync_MissingTemplate_ThrowsException()
    {
        // Arrange — no template file written
        var sut = CreateSut();

        // Act
        var act = async () => await sut.RenderAsync("nonexistent", new { });

        // Assert
        await act.Should().ThrowAsync<Exception>();
    }

    [Fact]
    public async Task RenderAsync_TemplatesDirectoryCreatedIfMissing()
    {
        // Arrange — use a root where EmailTemplates does not yet exist
        var freshRoot = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(freshRoot);

        try
        {
            var env = new Mock<IWebHostEnvironment>();
            env.Setup(e => e.ContentRootPath).Returns(freshRoot);

            // Act — constructor should create the directory, not throw
            var act = () => new RazorTemplateService(env.Object, Mock.Of<ILogger<RazorTemplateService>>());

            act.Should().NotThrow();
            Directory.Exists(Path.Combine(freshRoot, "EmailTemplates")).Should().BeTrue();
        }
        finally
        {
            Directory.Delete(freshRoot, recursive: true);
        }
    }
}
