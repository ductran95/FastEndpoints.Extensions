using System.Diagnostics;
using FastEndpoints.DiagnosticSources.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;

namespace FastEndpoints.DiagnosticSources.Middleware;

public class FastEndpointsDiagnosticsMiddleware
{
    private readonly RequestDelegate _next;

    public FastEndpointsDiagnosticsMiddleware(RequestDelegate next)
        => _next = next ?? throw new ArgumentNullException(nameof(next));

    public async Task Invoke(HttpContext ctx)
    {
        var endpoint = ((IEndpointFeature)ctx.Features[typeof(IEndpointFeature)]!)?.Endpoint;

        if (endpoint is null)
        {
            await _next(ctx);
        }
        else
        {
            var epDef = endpoint.Metadata.GetMetadata<EndpointDefinition>();

            if (epDef is not null)
            {
                var activityName = epDef.GetActivityName();
                using var activity = Trace.ActivitySource.StartActivity(activityName);
                try
                {
                    await _next(ctx);
                    activity?.SetStatus(ActivityStatusCode.Ok);
                }
                catch (Exception ex)
                {
                    activity?.SetStatus(ActivityStatusCode.Error, ex.GetType().Name);
                    throw;
                }
            }
            else
            {
                await _next(ctx);
            }
        }
        
    }
}