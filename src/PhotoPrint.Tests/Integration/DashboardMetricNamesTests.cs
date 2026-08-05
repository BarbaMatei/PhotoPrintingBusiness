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

    [Fact]
    public async Task Every_queried_label_exists_on_the_series_it_filters()
    {
        // Checking only metric names lets a renamed label or label value empty a panel while the
        // build stays green, which is the same silent drift the name check exists to stop.
        var exposedLabels = await ExposedSeriesLabelsAsync();

        var usages = DashboardQueries().Concat(SloQueries())
            .SelectMany(LabelUsagesIn)
            .Distinct()
            .ToList();

        usages.Should().NotBeEmpty("the queries filter on labels, so there is something to check");

        foreach (var (metric, label, value) in usages)
        {
            exposedLabels.Should().ContainKey(metric);
            exposedLabels[metric].Should().Contain(
                label,
                $"'{metric}' is queried with a '{label}' matcher but the exposition carries "
                    + $"[{string.Join(", ", exposedLabels[metric].Order())}]");

            if (value is null) continue;
            if (!MetricNames.LabelContract.TryGetValue(metric, out var declared)) continue;
            if (!declared.TryGetValue(label, out var allowed)) continue;

            allowed.Should().Contain(
                value,
                $"'{metric}{{{label}=\"{value}\"}}' is queried but MetricNames declares only "
                    + $"[{string.Join(", ", allowed)}]");
        }
    }

    private static IEnumerable<(string Metric, string Label, string? Value)> LabelUsagesIn(string expr)
    {
        foreach (Match m in Regex.Matches(expr, @"([a-zA-Z_][a-zA-Z0-9_]*)\s*\{([^}]*)\}"))
        {
            var metric = m.Groups[1].Value;
            if (PromQlKeywords.Contains(metric)) continue;

            foreach (Match matcher in Regex.Matches(
                m.Groups[2].Value, "([a-zA-Z_][a-zA-Z0-9_]*)\\s*(=~|!~|!=|=)\\s*\"([^\"]*)\""))
            {
                var op    = matcher.Groups[2].Value;
                var value = matcher.Groups[3].Value;

                // A negative or regex matcher does not have to name a value that exists.
                yield return (metric, matcher.Groups[1].Value,
                    op == "=" ? value : null);
            }
        }
    }

    private static async Task<Dictionary<string, HashSet<string>>> ExposedSeriesLabelsAsync()
    {
        var body = await ScrapeAsync();
        var labels = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);

        foreach (var line in body.Split('\n'))
        {
            var trimmed = line.Trim();
            if (trimmed.Length == 0 || trimmed[0] == '#') continue;

            var brace = trimmed.IndexOf('{');
            if (brace < 0) continue;

            var close = trimmed.IndexOf('}', brace);
            if (close < 0) continue;

            var series = trimmed[..brace];
            var bag    = labels.TryGetValue(series, out var existing)
                ? existing
                : labels[series] = new HashSet<string>(StringComparer.Ordinal);

            foreach (Match pair in Regex.Matches(
                trimmed[(brace + 1)..close], "([a-zA-Z_][a-zA-Z0-9_]*)\\s*="))
            {
                bag.Add(pair.Groups[1].Value);
            }
        }

        return labels;
    }

    private static async Task<HashSet<string>> ExposedSeriesNamesAsync()
    {
        var body  = await ScrapeAsync();
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

    private static async Task<string> ScrapeAsync()
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

        return await (await client.GetAsync("/metrics")).Content.ReadAsStringAsync();
    }

    private static List<string> DashboardMetricNames() =>
        DashboardQueries().SelectMany(MetricNamesIn).Distinct(StringComparer.Ordinal).ToList();

    private static List<string> DashboardQueries()
    {
        var json = File.ReadAllText(
            Path.Combine(RepoRoot(), "ops", "dashboards", "fototipar-overview.json"));
        using var document = JsonDocument.Parse(json);

        var queries = new List<string>();
        CollectPanelQueries(document.RootElement.GetProperty("panels"), queries);
        return queries;
    }

    // Grafana nests a row's children under the row panel, so a dashboard grouped into rows would
    // otherwise present no queries at all and still satisfy a non-empty check.
    private static void CollectPanelQueries(JsonElement panels, List<string> queries)
    {
        foreach (var panel in panels.EnumerateArray())
        {
            if (panel.TryGetProperty("panels", out var nested))
                CollectPanelQueries(nested, queries);

            if (!panel.TryGetProperty("targets", out var targets)) continue;
            foreach (var target in targets.EnumerateArray())
            {
                if (target.TryGetProperty("expr", out var expr) && expr.GetString() is { } q)
                    queries.Add(q);
            }
        }
    }

    private static List<string> SloMetricNames() =>
        SloQueries().SelectMany(MetricNamesIn).Distinct(StringComparer.Ordinal).ToList();

    private static List<string> SloQueries()
    {
        var text = File.ReadAllText(
            Path.Combine(RepoRoot(), "memory-bank", "operations", "slos.md"));

        return Regex.Matches(text, "```\\r?\\n(.*?)```", RegexOptions.Singleline)
            .Select(block => block.Groups[1].Value)
            .ToList();
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
