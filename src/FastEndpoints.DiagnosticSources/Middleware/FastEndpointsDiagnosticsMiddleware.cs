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

    public Task Invoke(HttpContext ctx)
    {
        var endpoint = ((IEndpointFeature)ctx.Features[typeof(IEndpointFeature)]!)?.Endpoint;

        if (endpoint is null) return _next(ctx);

        var epDef = endpoint.Metadata.GetMetadata<EndpointDefinition>();

        if (epDef is not null)
        {
            var activityName = epDef.GetActivityName();
            var activity = Trace.ActivitySource.StartActivity(activityName);
            return _next(ctx).ContinueWith(task =>
            {
                if (task.IsFaulted)
                {
                    var exception = task.Exception?.InnerException;
                    activity?.SetStatus(ActivityStatusCode.Error, exception?.GetType()?.Name);
                }
                else
                {
                    activity?.SetStatus(ActivityStatusCode.Ok);
                }
                activity?.Dispose();
            });
        }
        else
        {
            return _next(ctx);
        }
    }
}