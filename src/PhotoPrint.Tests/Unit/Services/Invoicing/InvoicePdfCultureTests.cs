using System.Globalization;
using System.Reflection;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using PhotoPrint.API.Services.Invoicing;

namespace PhotoPrint.Tests.Unit.Services.Invoicing;

public class InvoicePdfCultureTests
{
    [Fact]
    public void The_invoice_renders_with_a_culture_that_formats_the_romanian_way()
    {
        var culture = TheCultureTheRendererUses();

        culture.Name.Should().Be(
            "ro-RO",
            "a Romanian fiscal invoice is not allowed to fall back to invariant formatting");
        1234.56m.ToString("N2", culture).Should().Be(
            "1.234,56",
            "a host answering 1,234.56 here has no Romanian locale data, and every invoice it "
                + "renders would carry foreign separators");
        new DateTimeOffset(2026, 8, 3, 10, 0, 0, TimeSpan.Zero)
            .ToString("dd MMMM yyyy", culture)
            .Should().Be(
                "03 august 2026",
                "the invoice header prints the issue date with a Romanian month name");
    }

    [Fact]
    public void The_api_ships_a_runtimeconfig_that_keeps_invariant_globalization_off()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "PhotoPrint.API.runtimeconfig.json");
        File.Exists(path).Should().BeTrue(
            "the API's runtimeconfig travels next to the test binaries; without it this test "
                + "proves nothing about what production runs");

        var configProperties = JsonDocument.Parse(File.ReadAllText(path)).RootElement
            .GetProperty("runtimeOptions").GetProperty("configProperties");

        configProperties.TryGetProperty("System.Globalization.Invariant", out var invariant)
            .Should().BeTrue(
                "the runtime base image sets DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=true, and this "
                    + "switch is the only setting that outranks an environment variable — without "
                    + "it any .env or compose entry can take ro-RO away again");
        invariant.GetBoolean().Should().BeFalse();
    }

    [Fact]
    public void The_runtime_image_carries_the_icu_data_the_invoice_culture_needs()
    {
        var instructions = RuntimeStageInstructions();

        var apk = instructions.Single(i => i.StartsWith("RUN apk add", StringComparison.Ordinal));
        apk.Should().Contain(
            "icu-libs",
            "the aspnet Alpine base ships no ICU at all, so CultureInfo.GetCultureInfo(\"ro-RO\") "
                + "throws inside the invoice renderer's type initialiser on the first render");
        apk.Should().Contain(
            "icu-data-full",
            "Alpine's icu-libs pulls only the English data set, which carries no Romanian locale "
                + "data, so ro-RO cannot be relied on to format a fiscal invoice without it");

        var env = instructions.Single(i => i.StartsWith("ENV ", StringComparison.Ordinal));
        env.Should().Contain(
            "DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false",
            "installing ICU changes nothing while the base image's invariant flag is still true");
    }

    private static CultureInfo TheCultureTheRendererUses()
    {
        var field = typeof(InvoicePdfDocument)
            .GetField("Ro", BindingFlags.NonPublic | BindingFlags.Static);
        field.Should().NotBeNull(
            "InvoicePdfDocument no longer holds its culture in a static field called Ro — point "
                + "this test at whatever formats the invoice now");
        return (CultureInfo)field!.GetValue(null)!;
    }

    private static List<string> RuntimeStageInstructions()
    {
        var stages = File.ReadAllText(Path.Combine(RepoRoot(), "Dockerfile")).Split("\nFROM ");
        var stage = stages.Single(
            s => s.Split('\n')[0].TrimEnd().EndsWith("AS runtime", StringComparison.Ordinal));

        var instructions = new List<string>();
        var current = new StringBuilder();
        foreach (var raw in stage.Replace("\r", "").Split('\n'))
        {
            var line = raw.TrimEnd();
            if (line.TrimStart().StartsWith("#", StringComparison.Ordinal)) continue;

            var continued = line.EndsWith("\\", StringComparison.Ordinal);
            current.Append((continued ? line[..^1] : line).Trim()).Append(' ');
            if (continued) continue;

            var instruction = current.ToString().Trim();
            if (instruction.Length > 0) instructions.Add(instruction);
            current.Clear();
        }

        return instructions;
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
