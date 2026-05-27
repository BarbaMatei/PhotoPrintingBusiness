using System.Text.Json;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace PhotoPrint.API.HealthChecks;

public static class HealthCheckResponseWriter
{
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
    };

    public static async Task WriteAsync(HttpContext context, HealthReport report)
    {
        context.Response.ContentType = "application/json";

        var checks = report.Entries.ToDictionary(
            entry => entry.Key,
            entry =>
            {
                var check = new Dictionary<string, object>
                {
                    ["status"] = entry.Value.Status == HealthStatus.Healthy ? "OK" : "Error",
                    ["duration"] = entry.Value.Duration.ToString(),
                };

                foreach (var data in entry.Value.Data)
                {
                    check[data.Key] = data.Value;
                }

                return check;
            });

        var response = new
        {
            status = report.Status == HealthStatus.Healthy ? "Healthy" : "Unhealthy",
            checks,
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(response, _jsonOptions));
    }
}
