using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using PhotoPrint.API.BackgroundJobs;
using PhotoPrint.API.Data;
using PhotoPrint.API.Extensions;
using PhotoPrint.API.Models;
using PhotoPrint.API.Services;
using Xunit;

namespace PhotoPrint.Tests.Unit.Services;

public class EmailRetryJobTests
{
    private static PhotoPrintDbContext CreateInMemoryDb()
    {
        var options = new DbContextOptionsBuilder<PhotoPrintDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new PhotoPrintDbContext(options);
    }

    private static IServiceProvider BuildServiceProvider(IEmailSender sender, PhotoPrintDbContext db)
    {
        var services = new ServiceCollection();
        services.AddScoped<PhotoPrintDbContext>(_ => db);
        services.AddKeyedScoped<IEmailSender>(EmailExtensions.RawSenderKey, (_, _) => sender);
        return services.BuildServiceProvider();
    }

    private static EmailQueue MakePendingEmail(int attempts = 0, DateTimeOffset? nextRetry = null)
    {
        return new EmailQueue
        {
            Id = Guid.NewGuid(),
            To = "test@example.com",
            Subject = "Test",
            HtmlBody = "<p>Test</p>",
            Status = EmailStatus.Pending,
            Attempts = attempts,
            NextRetryAt = nextRetry ?? DateTimeOffset.UtcNow.AddSeconds(-1),
            CreatedAt = DateTimeOffset.UtcNow.AddMinutes(-5),
        };
    }

    [Fact]
    public async Task Processing_SuccessfulSend_MarksEmailAsSent()
    {
        // Arrange
        var db = CreateInMemoryDb();
        var email = MakePendingEmail();
        await db.EmailQueue.AddAsync(email);
        await db.SaveChangesAsync();

        var senderMock = new Mock<IEmailSender>();
        var scopeFactory = BuildScopeFactory(senderMock.Object, db);
        var job = new EmailRetryJob(scopeFactory, Mock.Of<ILogger<EmailRetryJob>>());

        // Act — run one cycle via reflection or CancellationToken trick
        using var cts = new CancellationTokenSource();
        cts.CancelAfter(TimeSpan.FromSeconds(1));
        await RunOneCycleAsync(job, cts.Token);

        // Assert
        var updated = await db.EmailQueue.FindAsync(email.Id);
        updated!.Status.Should().Be(EmailStatus.Sent);
        updated.SentAt.Should().NotBeNull();
        updated.LastError.Should().BeNull();
    }

    [Fact]
    public async Task Processing_FailedSend_IncrementsAttemptsAndSetsBackoff()
    {
        // Arrange
        var db = CreateInMemoryDb();
        var email = MakePendingEmail(attempts: 0);
        await db.EmailQueue.AddAsync(email);
        await db.SaveChangesAsync();

        var senderMock = new Mock<IEmailSender>();
        senderMock.Setup(s => s.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                  .ThrowsAsync(new Exception("SMTP unavailable"));

        var scopeFactory = BuildScopeFactory(senderMock.Object, db);
        var job = new EmailRetryJob(scopeFactory, Mock.Of<ILogger<EmailRetryJob>>());

        // Act
        using var cts = new CancellationTokenSource();
        cts.CancelAfter(TimeSpan.FromSeconds(1));
        await RunOneCycleAsync(job, cts.Token);

        // Assert
        var updated = await db.EmailQueue.FindAsync(email.Id);
        updated!.Status.Should().Be(EmailStatus.Pending);
        updated.Attempts.Should().Be(1);
        updated.NextRetryAt.Should().BeAfter(DateTimeOffset.UtcNow.AddSeconds(2)); // ~4s backoff
        updated.LastError.Should().Contain("SMTP unavailable");
    }

    [Fact]
    public async Task Processing_ThirdFailure_MarksEmailAsFailed()
    {
        // Arrange
        var db = CreateInMemoryDb();
        var email = MakePendingEmail(attempts: 2); // already had 2 retries
        await db.EmailQueue.AddAsync(email);
        await db.SaveChangesAsync();

        var senderMock = new Mock<IEmailSender>();
        senderMock.Setup(s => s.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                  .ThrowsAsync(new Exception("still failing"));

        var scopeFactory = BuildScopeFactory(senderMock.Object, db);
        var job = new EmailRetryJob(scopeFactory, Mock.Of<ILogger<EmailRetryJob>>());

        // Act
        using var cts = new CancellationTokenSource();
        cts.CancelAfter(TimeSpan.FromSeconds(1));
        await RunOneCycleAsync(job, cts.Token);

        // Assert
        var updated = await db.EmailQueue.FindAsync(email.Id);
        updated!.Status.Should().Be(EmailStatus.Failed);
        updated.Attempts.Should().Be(3);
    }

    [Fact]
    public async Task Processing_FutureNextRetryAt_SkipsEmail()
    {
        // Arrange
        var db = CreateInMemoryDb();
        var email = MakePendingEmail(nextRetry: DateTimeOffset.UtcNow.AddMinutes(10)); // not due
        await db.EmailQueue.AddAsync(email);
        await db.SaveChangesAsync();

        var senderMock = new Mock<IEmailSender>();
        var scopeFactory = BuildScopeFactory(senderMock.Object, db);
        var job = new EmailRetryJob(scopeFactory, Mock.Of<ILogger<EmailRetryJob>>());

        // Act
        using var cts = new CancellationTokenSource();
        cts.CancelAfter(TimeSpan.FromSeconds(1));
        await RunOneCycleAsync(job, cts.Token);

        // Assert — sender never called
        senderMock.Verify(s => s.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        var updated = await db.EmailQueue.FindAsync(email.Id);
        updated!.Status.Should().Be(EmailStatus.Pending);
    }

    // Helper: Build a scope factory wired to the given sender and db
    private static IServiceScopeFactory BuildScopeFactory(IEmailSender sender, PhotoPrintDbContext db)
    {
        var services = new ServiceCollection();
        services.AddSingleton<PhotoPrintDbContext>(_ => db);
        services.AddKeyedSingleton<IEmailSender>(EmailExtensions.RawSenderKey, (_, _) => sender);
        services.AddLogging();
        var provider = services.BuildServiceProvider();
        return provider.GetRequiredService<IServiceScopeFactory>();
    }

    // Helper: runs the job's internal processing method once via a short-lived token
    private static async Task RunOneCycleAsync(EmailRetryJob job, CancellationToken token)
    {
        // Use reflection to invoke the private ProcessPendingEmailsAsync for unit test isolation
        var method = typeof(EmailRetryJob).GetMethod(
            "ProcessPendingEmailsAsync",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        await (Task)method!.Invoke(job, new object[] { token })!;
    }
}
