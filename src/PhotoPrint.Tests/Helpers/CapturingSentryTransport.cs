using System.Text;
using Sentry;
using Sentry.Extensibility;
using Sentry.Protocol.Envelopes;

namespace PhotoPrint.Tests.Helpers;

// Serializing the envelope is the point: it is the exact bytes that would leave the process,
// so an assertion over it cannot be satisfied by a field the SDK would have sent anyway.
public sealed class CapturingSentryTransport : ITransport
{
    public List<string> Payloads { get; } = new();

    public Task SendEnvelopeAsync(Envelope envelope, CancellationToken cancellationToken = default)
    {
        using var buffer = new MemoryStream();
        envelope.Serialize(buffer, null);
        Payloads.Add(Encoding.UTF8.GetString(buffer.ToArray()));
        return Task.CompletedTask;
    }
}
