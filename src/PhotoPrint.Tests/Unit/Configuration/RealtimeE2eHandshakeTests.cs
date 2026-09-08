using System.Text.RegularExpressions;
using FluentAssertions;
using PhotoPrint.Tests.Helpers;

namespace PhotoPrint.Tests.Unit.Configuration;

public class RealtimeE2eHandshakeTests
{
    [Fact]
    public void RealtimeSpec_CountsEitherTransportIntoOneHandshakeSignal()
    {
        var spec = Spec();
        var (longPollCondition, longPollCounter) = LongPollCount(spec);
        var (socketCondition, socketCounter)     = SocketFrameCount(spec);

        longPollCondition.Should().Contain("/hubs/admin-orders",
            "a transport request proves the hub connection only when it is addressed to that hub");
        socketCondition.Should().Contain("/hubs/admin-orders",
            "a socket to any other URL — a dev-server reload channel included — says nothing about "
            + "the admin-orders hub");

        socketCounter.Should().Be(longPollCounter,
            "SignalR picks the transport, not the spec, so both listeners have to feed one counter "
            + "— a wait fed by a single transport times out on every run that negotiates the other");

        var wait = Regex.Match(spec,
            @"await\s+expect\s*\.\s*poll\(\s*\(\)\s*=>\s*(\w+)[\s\S]*?\.toBeGreaterThan\(\s*(\d+)\s*\)");

        wait.Success.Should().BeTrue(
            "the handshake gate has to be an awaited poll over the signal counter — a poll that is "
            + "built and never awaited lets the status change race the connection");
        wait.Groups[1].Value.Should().Be(longPollCounter,
            "the gate has to poll the counter both listeners feed");
        wait.Groups[2].Value.Should().Be("0",
            "one signal from either transport has to satisfy the gate — a higher floor makes the "
            + "transport that did connect look like a failure");
    }

    [Fact]
    public void RealtimeSpec_DoesNotCountTheNegotiatePostAsAConnection()
    {
        var spec = Spec();
        var (condition, _) = LongPollCount(spec);

        condition.Should().Contain("id=",
            "every transport POSTs /hubs/admin-orders/negotiate before it connects and that POST "
            + "succeeds even when the connection after it fails, so only the connection id tells a "
            + "live transport request apart from the handshake");
        Region(spec, "request").Should().NotContain("negotiate",
            "counting the negotiate POST turns the gate green on a hub the client never reached");
    }

    private static string Spec() =>
        RepoFiles.ReadAllText("src", "PhotoPrint.UI", "e2e", "realtime-order.spec.ts");

    private static (string Condition, string Counter) LongPollCount(string spec)
    {
        var counted = Regex.Match(Region(spec, "request"),
            @"if\s*\(([^{;]*)\)\s*\{?\s*(\w+)\s*(?:\+=\s*1|\+\+)");

        counted.Success.Should().BeTrue(
            "the page.on('request') listener has to count a matching request under a condition "
            + "that guards the count itself — a separate boolean the counter never reads narrows "
            + "nothing");
        return (counted.Groups[1].Value, counted.Groups[2].Value);
    }

    private static (string Condition, string Counter) SocketFrameCount(string spec)
    {
        var counted = Regex.Match(Region(spec, "websocket"),
            @"if\s*\(([^{;]*)\)\s*\{?\s*[\w.]*on\(\s*['""]framereceived['""][^;]*?(\w+)\s*(?:\+=\s*1|\+\+)");

        counted.Success.Should().BeTrue(
            "the page.on('websocket') listener has to count a received frame under a condition on "
            + "the socket URL — a socket that only opens is not yet a live connection, and a frame "
            + "hook outside that condition counts traffic from any other socket");
        return (counted.Groups[1].Value, counted.Groups[2].Value);
    }

    private static string Region(string spec, string channel)
    {
        var open = Regex.Match(spec, $@"page\.on\(\s*['""]{channel}['""]");

        open.Success.Should().BeTrue(
            "realtime-order.spec.ts has to register a page.on('{0}') listener to see that transport",
            channel);

        var rest = spec[(open.Index + open.Length)..];
        var end  = new[]
            {
                Regex.Match(rest, @"page\.on\(").Index,
                Regex.Match(rest, @"page\.goto\(").Index,
            }
            .Where(i => i > 0)
            .DefaultIfEmpty(rest.Length)
            .Min();

        return rest[..end];
    }
}
