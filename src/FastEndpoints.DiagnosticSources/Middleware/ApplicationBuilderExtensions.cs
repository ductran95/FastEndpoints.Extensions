using Microsoft.AspNetCore.Builder;

namespace FastEndpoints.DiagnosticSources.Middleware;

public static class ApplicationBuilderExtensions
{
    public static IApplicationBuilder UseFastEndpointsDiagnosticsMiddleware(this IApplicationBuilder app)
    {
        app.UseMiddleware<FastEndpointsDiagnosticsMiddleware>();
        return app;
    }
}