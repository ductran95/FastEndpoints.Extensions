using FastEndpoints.OpenTelemetry.Implementation;
using OpenTelemetry.Instrumentation;

namespace FastEndpoints.OpenTelemetry;

internal class FastEndpointsInstrumentation: IDisposable
{
    private readonly DiagnosticSourceSubscriber diagnosticSourceSubscriber;

    public FastEndpointsInstrumentation(FastEndpointsListener httpInListener)
    {
        this.diagnosticSourceSubscriber = new DiagnosticSourceSubscriber(httpInListener, null);
        this.diagnosticSourceSubscriber.Subscribe();
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        this.diagnosticSourceSubscriber?.Dispose();
    }
}