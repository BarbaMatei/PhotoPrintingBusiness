namespace PhotoPrint.Tests.Integration;

// OpenTelemetry's TracerProvider/MeterProvider and Sentry's SDK are process-global listeners:
// a live host in one test class observes the spans and measurements another class produces.
// Every class that boots a host with observability or Sentry switched on belongs here so no
// two of them are ever alive at the same time.
[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class ObservabilityHostCollection
{
    public const string Name = "observability-hosts";
}
