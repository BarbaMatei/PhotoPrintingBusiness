using System.Text.RegularExpressions;
using FluentAssertions;
using PhotoPrint.Tests.Helpers;

namespace PhotoPrint.Tests.Unit.Configuration;

public class E2eCredentialsTests
{
    [Fact]
    public void PlaywrightSources_CarryNoSeededAdminCredential()
    {
        var email    = SeedConstant("AdminEmail");
        var password = SeedConstant("AdminPassword");

        foreach (var file in Directory.EnumerateFiles(
                     RepoFiles.Path("src", "PhotoPrint.UI", "e2e"), "*.ts", SearchOption.AllDirectories))
        {
            var text = File.ReadAllText(file);

            text.Should().NotContain(email,
                "{0} would publish the seeded admin's login to anyone reading the repo, and the "
                + "same account is what docs/DEPLOYMENT.md creates on a first production deploy",
                Path.GetFileName(file));
            text.Should().NotContain(password,
                "{0} would publish the seeded admin's password", Path.GetFileName(file));
        }
    }

    [Fact]
    public void E2eStack_ReadsTheAdminCredentialsWithNoFallback()
    {
        var stack = RepoFiles.ReadAllText("src", "PhotoPrint.UI", "e2e", "support", "stack.ts");

        stack.Should().MatchRegex(
            @"E2E_ADMIN_EMAIL",
            "the suite has to take the admin login from the environment");
        stack.Should().NotMatchRegex(
            @"E2E_ADMIN_(EMAIL|PASSWORD)[^\r\n]{0,6}(\?\?|\|\|)",
            "a default behind either variable silently restores a committed credential and turns a "
            + "missing-secret CI run green against whatever account that default names");
    }

    [Fact]
    public void E2eWorkflow_ResolvesTheAdminCredentialsBeforeItRunsTheSuite()
    {
        var workflow = RepoFiles.ReadAllText(".github", "workflows", "playwright-e2e.yml");

        var email = Exported(workflow, "E2E_ADMIN_EMAIL");
        var pass  = Exported(workflow, "E2E_ADMIN_PASSWORD");
        var suite = Regex.Match(workflow, @"^\s*run:\s*npm run e2e\s*$", RegexOptions.Multiline);

        suite.Success.Should().BeTrue("the workflow must still run the suite");
        var run = suite.Index;
        email.Should().BeInRange(0, run,
            "the specs refuse to start without E2E_ADMIN_EMAIL, so CI has to export it first");
        pass.Should().BeInRange(0, run,
            "the specs refuse to start without E2E_ADMIN_PASSWORD, so CI has to export it first");
    }

    private static int Exported(string workflow, string name)
    {
        var line = Regex.Match(workflow, $@"^\s*echo\s+""{name}=[^\r\n]*>>\s*""\$GITHUB_ENV""", RegexOptions.Multiline);

        line.Success.Should().BeTrue(
            "{0} has to reach the suite through $GITHUB_ENV — a mention in a comment or a step name "
            + "would satisfy a plain substring search while the real export sat after the run", name);
        return line.Index;
    }

    private static string SeedConstant(string name)
    {
        var seed  = RepoFiles.ReadAllText("src", "PhotoPrint.API", "Data", "Seed", "ProductCatalogSeed.cs");
        var match = Regex.Match(seed, $@"{name}\s*=\s*""([^""]+)""");

        match.Success.Should().BeTrue("the seeder declares {0}", name);
        return match.Groups[1].Value;
    }
}
