using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using PhotoPrint.API.Models;
using PhotoPrint.API.Services;

namespace PhotoPrint.Tests.Unit.Services;

public class StorageRouterTests
{
    [Fact]
    public void For_Local_ReturnsLocalAdapter()
    {
        var local = Mock.Of<IStorageService>();
        var sp = BuildProvider(local, cloud: null);

        var router = new StorageRouter(sp);

        router.For(StorageLocation.Local).Should().BeSameAs(local);
    }

    [Fact]
    public void For_Cloud_WhenEnabled_ReturnsCloudAdapter()
    {
        var local = Mock.Of<IStorageService>();
        var cloud = Mock.Of<IStorageService>();
        var sp = BuildProvider(local, cloud);

        var router = new StorageRouter(sp);

        router.For(StorageLocation.Cloud).Should().BeSameAs(cloud);
    }

    [Fact]
    public void CloudEnabled_FalseWhenNoCloudRegistered()
    {
        var router = new StorageRouter(BuildProvider(Mock.Of<IStorageService>(), cloud: null));

        router.CloudEnabled.Should().BeFalse();
    }

    [Fact]
    public void CloudEnabled_TrueWhenCloudRegistered()
    {
        var router = new StorageRouter(BuildProvider(Mock.Of<IStorageService>(), Mock.Of<IStorageService>()));

        router.CloudEnabled.Should().BeTrue();
    }

    [Fact]
    public void Cloud_WhenDisabled_Throws()
    {
        var router = new StorageRouter(BuildProvider(Mock.Of<IStorageService>(), cloud: null));

        var act = () => router.Cloud;

        act.Should().Throw<InvalidOperationException>().WithMessage("*Provider=S3*");
    }

    [Fact]
    public void For_Cloud_WhenDisabled_Throws()
    {
        var router = new StorageRouter(BuildProvider(Mock.Of<IStorageService>(), cloud: null));

        var act = () => router.For(StorageLocation.Cloud);

        act.Should().Throw<InvalidOperationException>();
    }

    private static IServiceProvider BuildProvider(IStorageService local, IStorageService? cloud)
    {
        var services = new ServiceCollection();
        services.AddKeyedSingleton<IStorageService>("local", local);
        if (cloud is not null)
            services.AddKeyedSingleton<IStorageService>("cloud", cloud);
        return services.BuildServiceProvider();
    }
}
