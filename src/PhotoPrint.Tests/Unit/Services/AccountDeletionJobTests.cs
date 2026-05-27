using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using PhotoPrint.API.BackgroundJobs;
using PhotoPrint.API.Data;
using PhotoPrint.API.Models;
using Xunit;

namespace PhotoPrint.Tests.Unit.Services;

public class AccountDeletionJobTests
{
    private static PhotoPrintDbContext CreateDb() =>
        new(new DbContextOptionsBuilder<PhotoPrintDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static IServiceScopeFactory BuildScopeFactory(PhotoPrintDbContext db)
    {
        var services = new ServiceCollection();
        services.AddSingleton<PhotoPrintDbContext>(_ => db);
        services.AddLogging();
        return services.BuildServiceProvider().GetRequiredService<IServiceScopeFactory>();
    }

    private static User MakeUser(DateTimeOffset? deletionRequestedAt = null) => new()
    {
        Id = Guid.NewGuid(),
        Email = $"user-{Guid.NewGuid()}@example.com",
        NormalizedEmail = $"USER-{Guid.NewGuid()}@EXAMPLE.COM",
        DeletionRequestedAt = deletionRequestedAt,
    };

    private static async Task<int> InvokeDeleteAsync(AccountDeletionJob job, CancellationToken ct)
    {
        var method = typeof(AccountDeletionJob).GetMethod(
            "DeleteExpiredAccountsAsync",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        return await (Task<int>)method!.Invoke(job, new object[] { ct })!;
    }

    [Fact]
    public async Task UserWithExpiredDeletionRequest_IsHardDeleted()
    {
        var db = CreateDb();
        var user = MakeUser(deletionRequestedAt: DateTimeOffset.UtcNow.AddDays(-31));
        await db.Users.AddAsync(user);
        await db.SaveChangesAsync();

        var job = new AccountDeletionJob(BuildScopeFactory(db),
            Mock.Of<ILogger<AccountDeletionJob>>());

        var deleted = await InvokeDeleteAsync(job, CancellationToken.None);

        deleted.Should().Be(1);
        var remaining = await db.Users.FindAsync(user.Id);
        remaining.Should().BeNull();
    }

    [Fact]
    public async Task UserWithRecentDeletionRequest_IsNotDeleted()
    {
        var db = CreateDb();
        var user = MakeUser(deletionRequestedAt: DateTimeOffset.UtcNow.AddDays(-10));
        await db.Users.AddAsync(user);
        await db.SaveChangesAsync();

        var job = new AccountDeletionJob(BuildScopeFactory(db),
            Mock.Of<ILogger<AccountDeletionJob>>());

        var deleted = await InvokeDeleteAsync(job, CancellationToken.None);

        deleted.Should().Be(0);
        var remaining = await db.Users.FindAsync(user.Id);
        remaining.Should().NotBeNull();
    }

    [Fact]
    public async Task UserWithoutDeletionRequest_IsNotDeleted()
    {
        var db = CreateDb();
        var user = MakeUser(deletionRequestedAt: null);
        await db.Users.AddAsync(user);
        await db.SaveChangesAsync();

        var job = new AccountDeletionJob(BuildScopeFactory(db),
            Mock.Of<ILogger<AccountDeletionJob>>());

        var deleted = await InvokeDeleteAsync(job, CancellationToken.None);

        deleted.Should().Be(0);
        var remaining = await db.Users.FindAsync(user.Id);
        remaining.Should().NotBeNull();
    }

    [Fact]
    public async Task ExactlyAt30Days_UserIsNotDeleted()
    {
        var db = CreateDb();
        // Exactly 30 days ago — NOT yet past the 30-day retention period
        var user = MakeUser(deletionRequestedAt: DateTimeOffset.UtcNow.AddDays(-30).AddMinutes(1));
        await db.Users.AddAsync(user);
        await db.SaveChangesAsync();

        var job = new AccountDeletionJob(BuildScopeFactory(db),
            Mock.Of<ILogger<AccountDeletionJob>>());

        var deleted = await InvokeDeleteAsync(job, CancellationToken.None);

        deleted.Should().Be(0);
    }

    [Fact]
    public async Task MultipleExpiredUsers_AllDeleted()
    {
        var db = CreateDb();
        var users = Enumerable.Range(0, 3)
            .Select(_ => MakeUser(deletionRequestedAt: DateTimeOffset.UtcNow.AddDays(-31)))
            .ToList();
        await db.Users.AddRangeAsync(users);
        await db.SaveChangesAsync();

        var job = new AccountDeletionJob(BuildScopeFactory(db),
            Mock.Of<ILogger<AccountDeletionJob>>());

        var deleted = await InvokeDeleteAsync(job, CancellationToken.None);

        deleted.Should().Be(3);
        (await db.Users.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task MixedUsers_OnlyExpiredDeleted()
    {
        var db = CreateDb();
        await db.Users.AddRangeAsync(
            MakeUser(deletionRequestedAt: DateTimeOffset.UtcNow.AddDays(-31)), // → DELETE
            MakeUser(deletionRequestedAt: DateTimeOffset.UtcNow.AddDays(-10)), // → keep
            MakeUser(deletionRequestedAt: null)                                 // → keep
        );
        await db.SaveChangesAsync();

        var job = new AccountDeletionJob(BuildScopeFactory(db),
            Mock.Of<ILogger<AccountDeletionJob>>());

        var deleted = await InvokeDeleteAsync(job, CancellationToken.None);

        deleted.Should().Be(1);
        (await db.Users.CountAsync()).Should().Be(2);
    }
}
