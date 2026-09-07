using FluentAssertions;
using Microsoft.Extensions.Configuration;
using PhotoPrint.API.Configuration;
using PhotoPrint.API.Validators;

namespace PhotoPrint.Tests.Unit.Configuration;

public class ForwardedHeadersSettingsValidatorTests
{
    [Fact]
    public void An_empty_trusted_proxy_list_is_valid()
    {
        var result = Validate(new ForwardedHeadersSettings());

        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public void A_plain_address_and_a_single_pair_range_are_valid()
    {
        var result = Validate(new ForwardedHeadersSettings
        {
            TrustedProxies = ["172.28.0.2", "10.0.0.0/31", "::1"],
        });

        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public void A_single_pinned_proxy_address_is_valid()
    {
        var result = Validate(new ForwardedHeadersSettings { TrustedProxies = ["172.28.0.2"] });

        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public void A_subnet_wide_range_fails_validation()
    {
        var result = Validate(new ForwardedHeadersSettings { TrustedProxies = ["172.28.0.0/24"] });

        result.Failed.Should().BeTrue();
        result.FailureMessage.Should().Contain("ForwardedHeaders:TrustedProxies")
            .And.Contain("172.28.0.0/24");
    }

    [Fact]
    public void The_whole_internet_fails_validation()
    {
        var result = Validate(new ForwardedHeadersSettings { TrustedProxies = ["0.0.0.0/0"] });

        result.Failed.Should().BeTrue();
        result.FailureMessage.Should().Contain("0.0.0.0/0");
    }

    [Fact]
    public void An_ipv6_range_wider_than_one_pair_fails_validation()
    {
        var result = Validate(new ForwardedHeadersSettings { TrustedProxies = ["2001:db8::/64"] });

        result.Failed.Should().BeTrue();
        result.FailureMessage.Should().Contain("2001:db8::/64");
    }

    [Fact]
    public void An_ipv6_single_pair_range_is_valid()
    {
        var result = Validate(new ForwardedHeadersSettings { TrustedProxies = ["2001:db8::/127"] });

        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public void An_ipv6_link_local_entry_fails_validation()
    {
        var result = Validate(new ForwardedHeadersSettings { TrustedProxies = ["fe80::1"] });

        result.Failed.Should().BeTrue();
        result.FailureMessage.Should().Contain("fe80::1");
    }

    [Fact]
    public void An_unparseable_entry_fails_validation()
    {
        var result = Validate(new ForwardedHeadersSettings { TrustedProxies = ["not.an.ip"] });

        result.Failed.Should().BeTrue();
        result.FailureMessage.Should().Contain("ForwardedHeaders:TrustedProxies")
            .And.Contain("not.an.ip");
    }

    [Fact]
    public void A_cidr_range_with_host_bits_set_fails_validation()
    {
        var result = Validate(new ForwardedHeadersSettings { TrustedProxies = ["172.28.0.5/24"] });

        result.Failed.Should().BeTrue();
        result.FailureMessage.Should().Contain("172.28.0.0/24");
    }

    [Fact]
    public void A_leading_zero_form_fails_rather_than_becoming_an_octal_address()
    {
        var result = Validate(new ForwardedHeadersSettings { TrustedProxies = ["010.0.0.1"] });

        result.Failed.Should().BeTrue();
        result.FailureMessage.Should().Contain("010.0.0.1");
    }

    [Fact]
    public void Trusted_proxies_without_a_scrape_listener_fails_validation()
    {
        var result = Validate(
            new ForwardedHeadersSettings { TrustedProxies = ["172.28.0.2"] },
            observability: new Dictionary<string, string?>
            {
                ["Observability:Enabled"]            = "true",
                ["Observability:Metrics:ScrapePort"] = "0",
            });

        result.Failed.Should().BeTrue();
        result.FailureMessage.Should().Contain("Observability:Metrics:ScrapePort");
    }

    [Fact]
    public void Trusted_proxies_with_a_scrape_listener_is_valid()
    {
        var result = Validate(
            new ForwardedHeadersSettings { TrustedProxies = ["172.28.0.2"] },
            observability: new Dictionary<string, string?>
            {
                ["Observability:Enabled"]            = "true",
                ["Observability:Metrics:ScrapePort"] = "9090",
            });

        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public void Trusted_proxies_with_observability_off_does_not_require_a_scrape_listener()
    {
        var result = Validate(
            new ForwardedHeadersSettings { TrustedProxies = ["172.28.0.2"] },
            observability: new Dictionary<string, string?>
            {
                ["Observability:Enabled"]            = "false",
                ["Observability:Metrics:ScrapePort"] = "0",
            });

        result.Succeeded.Should().BeTrue();
    }

    private static Microsoft.Extensions.Options.ValidateOptionsResult Validate(
        ForwardedHeadersSettings settings,
        Dictionary<string, string?>? observability = null)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(observability ?? [])
            .Build();

        return new ForwardedHeadersSettingsValidator(configuration).Validate(null, settings);
    }
}
