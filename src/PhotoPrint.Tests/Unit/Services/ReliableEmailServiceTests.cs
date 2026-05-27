using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using PhotoPrint.API.Configuration;
using PhotoPrint.API.Data;
using PhotoPrint.API.Models;
using PhotoPrint.API.Services;
using Xunit;

namespace PhotoPrint.Tests.Unit.Services;

public class ReliableEmailServiceTests
{
    private static PhotoPrintDbContext CreateInMemoryDb()
    {
        var options = new DbContextOptionsBuilder<PhotoPrintDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new PhotoPrintDbContext(options);
    }

    private static ReliableEmailService CreateSut(
        IEmailSender sender,
        IRazorTemplateService? templates = null,
        PhotoPrintDbContext? db = null,
        ILogger<ReliableEmailService>? logger = null)
    {
        return new ReliableEmailService(
            sender,
            templates ?? Mock.Of<IRazorTemplateService>(),
            db ?? CreateInMemoryDb(),
            logger ?? Mock.Of<ILogger<ReliableEmailService>>());
    }

    [Fact]
    public async Task SendAsync_SuccessfulSend_DoesNotQueueToDatabase()
    {
        // Arrange
        var senderMock = new Mock<IEmailSender>();
        var db = CreateInMemoryDb();
        var sut = CreateSut(senderMock.Object, db: db);

        // Act
        await sut.SendAsync("user@example.com", "Test", "<p>body</p>");

        // Assert
        senderMock.Verify(s => s.SendAsync("user@example.com", "Test", "<p>body</p>", default), Times.Once);
        var queued = await db.EmailQueue.CountAsync();
        queued.Should().Be(0);
    }

    [Fact]
    public async Task SendAsync_FailedSend_QueuesEmailToDatabase()
    {
        // Arrange
        var senderMock = new Mock<IEmailSender>();
        senderMock.Setup(s => s.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                  .ThrowsAsync(new InvalidOperationException("SMTP connection refused"));

        var db = CreateInMemoryDb();
        var sut = CreateSut(senderMock.Object, db: db);

        // Act
        await sut.SendAsync("user@example.com", "Test", "<p>body</p>");

        // Assert — email queued, not re-thrown
        var entry = await db.EmailQueue.SingleAsync();
        entry.To.Should().Be("user@example.com");
        entry.Subject.Should().Be("Test");
        entry.Status.Should().Be(EmailStatus.Pending);
        entry.Attempts.Should().Be(0);
        entry.NextRetryAt.Should().BeAfter(DateTimeOffset.UtcNow.AddSeconds(-1));
        entry.LastError.Should().Contain("SMTP connection refused");
    }

    [Fact]
    public async Task SendAsync_FailedSend_SetsNextRetryToOneSecondFromNow()
    {
        // Arrange
        var senderMock = new Mock<IEmailSender>();
        senderMock.Setup(s => s.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                  .ThrowsAsync(new Exception("fail"));

        var db = CreateInMemoryDb();
        var sut = CreateSut(senderMock.Object, db: db);
        var before = DateTimeOffset.UtcNow;

        // Act
        await sut.SendAsync("a@b.com", "S", "H");

        // Assert
        var entry = await db.EmailQueue.SingleAsync();
        entry.NextRetryAt.Should().BeCloseTo(before.AddSeconds(1), precision: TimeSpan.FromMilliseconds(500));
    }

    [Fact]
    public async Task SendTemplatedAsync_TemplateRenderFails_PropagatesException()
    {
        // Arrange
        var templatesMock = new Mock<IRazorTemplateService>();
        templatesMock.Setup(t => t.RenderAsync("missing", It.IsAny<object>()))
                     .ThrowsAsync(new FileNotFoundException("Template not found"));

        var senderMock = new Mock<IEmailSender>();
        var db = CreateInMemoryDb();
        var sut = CreateSut(senderMock.Object, templatesMock.Object, db);

        // Act
        var act = async () => await sut.SendTemplatedAsync("a@b.com", "S", "missing", new { });

        // Assert — template errors propagate (not swallowed)
        await act.Should().ThrowAsync<FileNotFoundException>();
        var queued = await db.EmailQueue.CountAsync();
        queued.Should().Be(0); // template failure not queued
    }

    [Fact]
    public async Task SendTemplatedAsync_RenderSucceedsButSendFails_QueuesRenderedHtml()
    {
        // Arrange
        var templatesMock = new Mock<IRazorTemplateService>();
        templatesMock.Setup(t => t.RenderAsync("welcome", It.IsAny<object>()))
                     .ReturnsAsync("<h1>Bun venit!</h1>");

        var senderMock = new Mock<IEmailSender>();
        senderMock.Setup(s => s.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                  .ThrowsAsync(new Exception("SMTP down"));

        var db = CreateInMemoryDb();
        var sut = CreateSut(senderMock.Object, templatesMock.Object, db);

        // Act
        await sut.SendTemplatedAsync("u@e.com", "Bun venit", "welcome", new { });

        // Assert
        var entry = await db.EmailQueue.SingleAsync();
        entry.HtmlBody.Should().Be("<h1>Bun venit!</h1>");
        entry.Status.Should().Be(EmailStatus.Pending);
    }
}
