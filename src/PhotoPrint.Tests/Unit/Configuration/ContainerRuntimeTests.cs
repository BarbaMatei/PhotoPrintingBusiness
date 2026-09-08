using System.Text.RegularExpressions;
using FluentAssertions;
using PhotoPrint.Tests.Helpers;

namespace PhotoPrint.Tests.Unit.Configuration;

public class ContainerRuntimeTests
{
    private const int VolumeOwnerUid = 1001;

    [Fact]
    public void Dockerfile_PinsTheRuntimeUidInsteadOfInheritingTheBaseImageUser()
    {
        var dockerfile = RepoFiles.ReadAllText("Dockerfile");

        dockerfile.Should().NotMatchRegex(
            @"id -u app .*\|\|",
            "a guard that skips user creation when the base tag already ships `app` leaves the "
            + "runtime on the base image's uid, which drifts per tag while /app/Storage on the "
            + "named volume stays owned by the uid the previous image used");
        dockerfile.Should().NotMatchRegex(
            @"getent group app .*\|\|",
            "same short-circuit for the group");

        ResolvedRuntimeUid(dockerfile).Should().Be(
            VolumeOwnerUid,
            "the runtime user must own /app/Storage on an apidata volume created by an earlier "
            + "build, or every upload write fails with permission denied while /health stays 200");

        dockerfile.Should().MatchRegex(
            @"(?m)^USER app\s*$",
            "creating the user at a pinned uid changes nothing unless the image actually runs as it");
    }

    [Fact]
    public void Dockerfile_DoesNotNameItsUidArgAfterTheBaseImageEnvVar()
    {
        RepoFiles.ReadAllText("Dockerfile").Should().NotContain(
            "ARG APP_UID",
            "the aspnet base image sets ENV APP_UID, and an environment variable takes precedence "
            + "over a build ARG of the same name, so the pinned default would be silently ignored");
    }

    private static int ResolvedRuntimeUid(string dockerfile)
    {
        var declared = Regex.Match(dockerfile, @"adduser\s+(?:-\S+\s+)*-u\s+(\S+)");
        declared.Success.Should().BeTrue("the runtime stage must create its own user at a pinned uid");

        var uid = declared.Groups[1].Value;
        var arg = Regex.Match(uid, @"^\$\{?(\w+)\}?$");
        if (arg.Success)
        {
            var value = Regex.Match(dockerfile, $@"^ARG\s+{arg.Groups[1].Value}=(\d+)", RegexOptions.Multiline);
            value.Success.Should().BeTrue("ARG {0} must carry a default, or the build uses no uid at all", arg.Groups[1].Value);
            uid = value.Groups[1].Value;
        }

        return int.Parse(uid);
    }
}
