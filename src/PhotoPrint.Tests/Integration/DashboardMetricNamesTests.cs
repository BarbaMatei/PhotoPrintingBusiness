using System.Diagnostics;
using System.Text.Json;
using System.Text.RegularExpressions;
using FluentAssertions;
using PhotoPrint.API.Observability;

namespace PhotoPrint.Tests.Integration;

/// <summary>
/// The dashboard and the API agree on metric names only by convention, and a panel querying a
/// name nothing emits renders "No Data" rather than failing — so nothing notices. This scrapes
/// a real exposition after emitting one observation per instrument and holds every dashboard
/// query's metric name against it.
/// </summary>
[Collection(ObservabilityHostCollection.Name)]
public class DashboardMetricNamesTests
{
    // PromQL identifiers that are never metric names. Everything else an expression mentions
    // outside a label matcher, a duration or a string has to exist in the exposition.
    private static readonly HashSet<string> PromQlKeywords = new(StringComparer.Ordinal)
    {
        "sum", "avg", "min", "max", "count", "count_values", "quantile", "topk", "bottomk",
        "stddev", "stdvar", "group", "rate", "irate", "increase", "delta", "idelta", "deriv",
        "histogram_quantile", "by", "without", "on", "ignoring", "group_left", "group_right",
        "le", "offset", "and", "or", "unless", "bool",
    };

    [Fact]
    public async Task Every_dashboard_query_names_a_metric_the_api_actually_exposes()
    {
        var exposed = await ExposedSeriesNamesAsync();

        var queried = DashboardMetricNames();

        queried.Should().NotBeEmpty("the dashboard is the thing under test");
        queried.Should().OnlyContain(
            name => exposed.Contains(name),
            "a panel that queries a name nothing emits is permanently No Data. Exposed: "
                + string.Join(", ", exposed.Where(n => !n.StartsWith("process_runtime")).Order()));
    }

    [Fact]
    public async Task Every_slo_query_names_a_metric_the_api_actually_exposes()
    {
        // slos.md is maintained separately from the dashboard, and an SLO whose query names a
        // metric nothing emits reads as 100% healthy rather than as broken.
        var exposed = await ExposedSeriesNamesAsync();

        var queried = SloMetricNames();

        queried.Should().NotBeEmpty("slos.md is the thing under test");
        queried.Should().OnlyContain(name => exposed.Contains(name));
    }

    private static async Task<HashSet<string>> ExposedSeriesNamesAsync()
    {
        using var factory = new ObservabilityEnabledLoopbackFactory();
        using var client = factory.CreateClient();

        // One observation per business instrument — an instrument that never records is absent
        // from the exposition entirely, so an empty scrape would pass a substring check.
        FotoMetrics.OrdersCreated.Add(1, new TagList
        {
            { MetricNames.Labels.Processor, MetricNames.ProcessorValues.Stripe },
            { MetricNames.Labels.Status,    MetricNames.OrderStatusValues.Created },
        });
        FotoMetrics.PaymentWebhook.Add(1, new TagList
        {
            { MetricNames.Labels.Processor, MetricNames.ProcessorValues.Stripe },
            { MetricNames.Labels.Result,    MetricNames.WebhookResultValues.Ok },
        });
        FotoMetrics.AwbCreation.Add(1, new TagList
        {
            { MetricNames.Labels.Result, MetricNames.AwbResultValues.Ok },
        });
        FotoMetrics.InvoiceAnafStatus.Add(1, new TagList
        {
            { MetricNames.Labels.Status, MetricNames.AnafStatusValues.Accepted },
        });
        FotoMetrics.UploadSize.Record(1024);
        FotoMetrics.OrderProcessingDuration.Record(1.5);

        // A handled request is what populates the ASP.NET Core instrumentation's histogram.
        await client.GetAsync("/health");

        var body = await (await client.GetAsync("/metrics")).Content.ReadAsStringAsync();

        var names = new HashSet<string>(StringComparer.Ordinal);
        foreach (var line in body.Split('\n'))
        {
            if (!line.StartsWith("# TYPE ", StringComparison.Ordinal)) continue;
            var parts = line.Trim().Split(' ');
            if (parts.Length < 4) continue;

            var (family, type) = (parts[2], parts[3]);
            names.Add(family);
            if (type is "histogram" or "summary")
            {
                names.Add($"{family}_bucket");
                names.Add($"{family}_count");
                names.Add($"{family}_sum");
            }
        }

        return names;
    }

    private static List<string> DashboardMetricNames()
    {
        var json = File.ReadAllText(
            Path.Combine(RepoRoot(), "ops", "dashboards", "fototipar-overview.json"));
        using var document = JsonDocument.Parse(json);

        var names = new List<string>();
        foreach (var panel in document.RootElement.GetProperty("panels").EnumerateArray())
        {
            if (!panel.TryGetProperty("targets", out var targets)) continue;
            foreach (var target in targets.EnumerateArray())
            {
                if (!target.TryGetProperty("expr", out var expr)) continue;
                names.AddRange(MetricNamesIn(expr.GetString()!));
            }
        }

        return names.Distinct(StringComparer.Ordinal).ToList();
    }

    private static List<string> SloMetricNames()
    {
        var text = File.ReadAllText(
            Path.Combine(RepoRoot(), "memory-bank", "operations", "slos.md"));

        var names = new List<string>();
        foreach (Match block in Regex.Matches(text, "```\\r?\\n(.*?)```", RegexOptions.Singleline))
            names.AddRange(MetricNamesIn(block.Groups[1].Value));

        return names.Distinct(StringComparer.Ordinal).ToList();
    }

    private static IEnumerable<string> MetricNamesIn(string expr)
    {
        // Label matchers, durations and strings all carry identifiers that are not metric
        // names, so they go before the identifiers are read.
        var stripped = Regex.Replace(expr, "\\{[^}]*\\}", " ");
        stripped = Regex.Replace(stripped, "\\[[^\\]]*\\]", " ");
        stripped = Regex.Replace(stripped, "\"[^\"]*\"", " ");

        foreach (Match m in Regex.Matches(stripped, "[a-zA-Z_][a-zA-Z0-9_]*"))
        {
            if (PromQlKeywords.Contains(m.Value)) continue;

            // A function call is not a metric reference.
            var after = stripped.AsSpan(m.Index + m.Length).TrimStart();
            if (after.Length > 0 && after[0] == '(') continue;

            yield return m.Value;
        }
    }

    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "ops", "dashboards", "fototipar-overview.json")))
                return dir.FullName;
            dir = dir.Parent;
        }

        throw new InvalidOperationException("repo root not found from " + AppContext.BaseDirectory);
    }
}
