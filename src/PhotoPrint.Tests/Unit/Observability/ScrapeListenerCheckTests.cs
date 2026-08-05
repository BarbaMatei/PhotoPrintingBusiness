using FluentAssertions;
using PhotoPrint.API.Observability;

namespace PhotoPrint.Tests.Unit.Observability;

public class ScrapeListenerCheckTests
{
    // The display names Kestrel reports post-bind, and the raw forms a host reports before binding.
    private static readonly string[] TwoListeners = ["http://[::]:8080", "http://[::]:9090"];

    [Fact]
    public void Scrape_port_zero_is_never_checked() =>
        ScrapeListenerCheck.Verdict(["http://[::]:8080"], 0).Should().BeNull();

    [Fact]
    public void A_host_reporting_no_addresses_is_not_serving_sockets() =>
        ScrapeListenerCheck.Verdict([], 9090).Should().BeNull();

    [Fact]
    public void The_shipped_two_listener_topology_passes() =>
        ScrapeListenerCheck.Verdict(TwoListeners, 9090).Should().BeNull();

    [Theory]
    [InlineData("http://+:8080")]
    [InlineData("http://*:8080")]
    [InlineData("http://0.0.0.0:8080")]
    [InlineData("http://localhost:8080")]
    public void A_scrape_port_nothing_listens_on_refuses_to_start(string bound) =>
        ScrapeListenerCheck.Verdict([bound], 9090)
            .Should().NotBeNull().And.Subject.ToString()
            .Should().Contain("not a port this process listens on");

    [Fact]
    public void A_scrape_port_that_is_the_only_listener_refuses_to_start() =>
        ScrapeListenerCheck.Verdict(["http://+:8080"], 8080)
            .Should().NotBeNull().And.Subject.ToString()
            .Should().Contain("only port this process listens on");

    [Fact]
    public void Two_addresses_on_one_port_are_still_one_listener() =>
        ScrapeListenerCheck.Verdict(["http://127.0.0.1:9090", "http://10.0.0.5:9090"], 9090)
            .Should().NotBeNull().And.Subject.ToString()
            .Should().Contain("only port this process listens on");

    [Fact]
    public void Wildcard_and_display_forms_parse_to_the_same_port() =>
        ScrapeListenerCheck.Verdict(["http://+:8080", "http://[::]:9090"], 9090).Should().BeNull();

    [Fact]
    public void An_address_with_no_port_is_not_counted_as_a_listener() =>
        ScrapeListenerCheck.Verdict(["http://unix:/tmp/kestrel.sock", "http://+:9090"], 9090)
            .Should().NotBeNull().And.Subject.ToString()
            .Should().Contain("only port this process listens on");

    [Fact]
    public void The_message_names_the_bound_ports_so_an_operator_can_act() =>
        ScrapeListenerCheck.Verdict(TwoListeners, 9191)
            .Should().NotBeNull().And.Subject.ToString()
            .Should().Contain("8080, 9090").And.Contain("ASPNETCORE_URLS");
}
