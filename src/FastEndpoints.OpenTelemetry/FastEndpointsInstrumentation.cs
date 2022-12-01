using FastEndpoints.OpenTelemetry.Implementation;
using OpenTelemetry.Instrumentation;

namespace FastEndpoints.OpenTelemetry;

internal class FastEndpointsInstrumentation: IDisposable
{
    private readonly DiagnosticSourceSubscriber _diagnosticSourceSubscriber;

    public FastEndpointsInstrumentation(FastEndpointsListener httpInListener)
    {
        _diagnosticSourceSubscriber = new DiagnosticSourceSubscriber(httpInListener, null);
        _diagnosticSourceSubscriber.Subscribe();
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        _diagnosticSourceSubscriber?.Dispose();
    }
}