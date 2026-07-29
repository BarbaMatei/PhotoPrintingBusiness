using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PhotoPrint.API.BackgroundJobs;
using PhotoPrint.API.Data;
using PhotoPrint.API.Extensions;
using PhotoPrint.API.Services;
using PhotoPrint.API.Services.Sameday;

namespace PhotoPrint.Tests.Unit.Services.Sameday;

/// <summary>
/// Resolves the Sameday:Enabled=true composition root — the dormant enabled path is otherwise
/// never exercised, and its DI graph once closed a resolution cycle (token provider -> authenticator
/// -> client -> auth handler -> token provider) that only threw on the first real request. Resolving
/// <see cref="ISamedayClient"/> here builds the auth-handler pipeline and would rethrow the cycle.
/// Exercises the real registration extension used by Program.
/// </summary>
public class SamedayCompositionRootTests
{
    private static ServiceProvider BuildEnabledProvider()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Sameday:Enabled"]       = "true",
                ["Sameday:Username"]      = "test-user",
                ["Sameday:Password"]      = "test-pass",
                ["Sameday:PickupPointId"] = "PP1",
                ["Sameday:Jobs:Enabled"]  = "true",
            })
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMemoryCache();
        services.AddSingleton<TimeProvider>(_ => TimeProvider.System);
        services.AddSingleton<IConfiguration>(config);
        services.AddDbContext<PhotoPrintDbContext>(o =>
            o.UseInMemoryDatabase($"sameday-di-{Guid.NewGuid():N}"));

        services.AddSamedayIntegration(config);
        return services.BuildServiceProvider();
    }

    [Fact]
    public void Enabled_root_resolves_client_creator_and_jobs_without_a_DI_cycle()
    {
        using var provider = BuildEnabledProvider();
        using var scope = provider.CreateScope();
        var sp = scope.ServiceProvider;

        // Load-bearing: resolving the typed client builds the auth-handler pipeline, which the
        // old cycle re-entered and threw on.
        var resolveClient = () => sp.GetRequiredService<ISamedayClient>();
        resolveClient.Should().NotThrow();

        sp.GetRequiredService<IAwbCreator>().Should().NotBeNull();

        // SamedayShippingService takes the concrete StaticShippingService as its fallback, so that
        // registration is load-bearing on this path — without it every shipping call would throw.
        sp.GetRequiredService<IShippingService>().Should().BeOfType<SamedayShippingService>();

        var hosted = provider.GetServices<IHostedService>().ToList();
        hosted.Should().Contain(s => s is AwbDispatcher);
        hosted.Should().Contain(s => s is AwbRetryJob);
        hosted.Should().Contain(s => s is ShipmentTrackingJob);
    }
}
