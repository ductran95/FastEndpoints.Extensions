using System.Diagnostics;
using FastEndpoints.OpenTelemetry.Extensions;
using FastEndpoints.OpenTelemetry.Implementation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;

namespace FastEndpoints.OpenTelemetry.Middleware;

public class FastEndpointsDiagnosticsMiddleware
{
    private static readonly DiagnosticSource _fastEndpointsLogger = new DiagnosticListener("FastEndpoints");
    
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
                if (_fastEndpointsLogger.IsEnabled("FastEndpointsStart"))
                {
                    _fastEndpointsLogger.Write("FastEndpointsStart", new
                    {
                        HttpContext = ctx,
                        EndpointDefinition = epDef
                    });
                }

                try
                {
                    await _next(ctx);

                    if (_fastEndpointsLogger.IsEnabled("FastEndpointsStop"))
                    {
                        _fastEndpointsLogger.Write("FastEndpointsStop", new
                        {
                            HttpContext = ctx,
                            EndpointDefinition = epDef
                        });
                    }
                }
                catch (ValidationFailureException validationFailureException)
                {
                    if (_fastEndpointsLogger.IsEnabled("FastEndpointsOnValidationFailed"))
                    {
                        _fastEndpointsLogger.Write("FastEndpointsOnValidationFailed", new
                        {
                            ValidationFailures = validationFailureException.Failures,
                            HttpContext = ctx,
                            EndpointDefinition = epDef
                        });
                    }
                    
                    if (_fastEndpointsLogger.IsEnabled("FastEndpointsException"))
                    {
                        _fastEndpointsLogger.Write("FastEndpointsException", new
                        {
                            HttpContext = ctx,
                            EndpointDefinition = epDef
                        });
                    }
                    
                    throw;
                }
                catch (Exception ex)
                {
                    if (_fastEndpointsLogger.IsEnabled("FastEndpointsException"))
                    {
                        _fastEndpointsLogger.Write("FastEndpointsException", new
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