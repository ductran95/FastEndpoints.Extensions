using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;

namespace FastEndpoints.OpenTelemetry.Middleware;

public class FastEndpointsDiagnosticsMiddleware
{
    private static readonly DiagnosticSource FastEndpointsLogger = new DiagnosticListener("FastEndpoints");
    
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
                if (FastEndpointsLogger.IsEnabled("FastEndpointsStart"))
                {
                    FastEndpointsLogger.Write("FastEndpointsStart", new
                    {
                        HttpContext = ctx,
                        EndpointDefinition = epDef
                    });
                }

                try
                {
                    await _next(ctx);

                    if (FastEndpointsLogger.IsEnabled("FastEndpointsStop"))
                    {
                        FastEndpointsLogger.Write("FastEndpointsStop", new
                        {
                            HttpContext = ctx,
                            EndpointDefinition = epDef
                        });
                    }
                }
                catch (ValidationFailureException validationFailureException)
                {
                    if (FastEndpointsLogger.IsEnabled("FastEndpointsOnValidationFailed"))
                    {
                        FastEndpointsLogger.Write("FastEndpointsOnValidationFailed", new
                        {
                            ValidationFailures = validationFailureException.Failures,
                            HttpContext = ctx,
                            EndpointDefinition = epDef
                        });
                    }
                    
                    if (FastEndpointsLogger.IsEnabled("FastEndpointsException"))
                    {
                        FastEndpointsLogger.Write("FastEndpointsException", new
                        {
                            HttpContext = ctx,
                            EndpointDefinition = epDef
                        });
                    }
                    
                    throw;
                }
                catch (Exception)
                {
                    if (FastEndpointsLogger.IsEnabled("FastEndpointsException"))
                    {
                        FastEndpointsLogger.Write("FastEndpointsException", new
                        {
                            HttpContext = ctx,
                            EndpointDefinition = epDef
                        });
                    }
                    
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