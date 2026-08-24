using System.Globalization;
using FluentAssertions;

namespace PhotoPrint.Tests.Unit.Services.Invoicing;

public class InvoicePdfCultureTests
{
    [Fact]
    public void The_invoice_culture_resolves_and_formats_the_romanian_way()
    {
        var ro = CultureInfo.GetCultureInfo("ro-RO");

        ro.NumberFormat.NumberDecimalSeparator.Should().Be(
            ",",
            "a Romanian fiscal invoice prints 1.234,56 — a host that answers '.' here is running "
                + "globalization-invariant or an English-only ICU data set, and every rendered "
                + "invoice would carry foreign number formatting");
        ro.NumberFormat.NumberGroupSeparator.Should().Be(".");
        1234.56m.ToString("N2", ro).Should().Be("1.234,56");
        new DateTimeOffset(2026, 8, 3, 10, 0, 0, TimeSpan.Zero)
            .ToString("dd MMMM yyyy", ro)
            .Should().Be(
                "03 august 2026",
                "the invoice header prints the issue date with a Romanian month name");
    }

    [Fact]
    public void The_api_project_keeps_invariant_globalization_off()
    {
        var csproj = File.ReadAllText(
            Path.Combine(RepoRoot(), "src", "PhotoPrint.API", "PhotoPrint.API.csproj"));

        csproj.Should().Contain(
            "<InvariantGlobalization>false</InvariantGlobalization>",
            "the runtime base image sets DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=true, and only the "
                + "runtimeconfig switch this property emits outranks an environment variable — "
                + "without it any .env or compose entry can silently take ro-RO away again");
    }

    [Fact]
    public void The_runtime_image_carries_the_icu_data_the_invoice_culture_needs()
    {
        var runtime = RuntimeStageOfTheDockerfile();

        runtime.Should().Contain(
            "icu-libs",
            "the aspnet Alpine base ships no ICU at all, so CultureInfo.GetCultureInfo(\"ro-RO\") "
                + "throws inside the invoice renderer's type initialiser on the first render");
        runtime.Should().Contain(
            "icu-data-full",
            "Alpine's icu-libs pulls only the English data set, and a Romanian locale that falls "
                + "back to root prints 1,234.56 on a fiscal invoice with no error anywhere");
        runtime.Should().Contain(
            "DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false",
            "installing ICU changes nothing while the base image's invariant flag is still true");
    }

    private static string RuntimeStageOfTheDockerfile()
    {
        var stages = File.ReadAllText(Path.Combine(RepoRoot(), "Dockerfile")).Split("\nFROM ");
        return stages.Single(
            s => s.Split('\n')[0].TrimEnd().EndsWith("AS runtime", StringComparison.Ordinal));
    }

    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "Dockerfile"))
                && File.Exists(Path.Combine(
                    dir.FullName, "src", "PhotoPrint.API", "PhotoPrint.API.csproj")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException("repo root not found from " + AppContext.BaseDirectory);
    }
}
