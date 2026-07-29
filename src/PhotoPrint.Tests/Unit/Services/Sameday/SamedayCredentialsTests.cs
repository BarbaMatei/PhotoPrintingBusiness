using FluentAssertions;
using PhotoPrint.API.Services.Sameday;

namespace PhotoPrint.Tests.Unit.Services.Sameday;

public class SamedayCredentialsTests
{
    [Fact]
    public void ToString_does_not_expose_username()
    {
        var c = new SamedayCredentials("alice@fototipar.ro", "very-secret");
        c.ToString().Should().NotContain("alice@fototipar.ro");
    }

    [Fact]
    public void ToString_does_not_expose_password()
    {
        var c = new SamedayCredentials("alice", "very-secret-password-12345");
        c.ToString().Should().NotContain("very-secret-password-12345");
    }

    [Fact]
    public void ToString_only_emits_redacted_marker()
    {
        var c = new SamedayCredentials("alice", "pw");
        c.ToString().Should().Contain("***");
    }

    [Fact]
    public void Equality_is_by_value()
    {
        var a = new SamedayCredentials("u", "p");
        var b = new SamedayCredentials("u", "p");
        a.Should().Be(b);
    }
}
