using System.Text.RegularExpressions;
using FluentAssertions;
using PhotoPrint.Tests.Helpers;

namespace PhotoPrint.Tests.Unit.Configuration;

public class ComposeEnvPrecedenceTests
{
    [Fact]
    public void DevCompose_LetsAStripeKeyFromEnvWin()
    {
        var overrides = Regex.Matches(
                RepoFiles.ReadAllText("docker-compose.yml"),
                @"^\s+(Stripe__\w+):\s*(\S+)\s*$",
                RegexOptions.Multiline)
            .Select(m => (Key: m.Groups[1].Value, Value: m.Groups[2].Value))
            .ToList();

        overrides.Should().NotBeEmpty("the dev stack needs a Stripe placeholder to construct the client");

        foreach (var (key, value) in overrides)
        {
            value.Should().StartWith(
                "${" + key + ":-",
                "`environment:` takes precedence over `env_file:`, so a bare literal overrides the "
                + "real {0} a developer put in .env and every payment intent then gets a Stripe 401",
                key);
        }
    }
}
