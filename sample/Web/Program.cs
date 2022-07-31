global using FastEndpoints;
global using FastEndpoints.Security;
global using Web.Auth;
using Microsoft.AspNetCore.Localization;
using System.Globalization;
using FastEndpoints.ApiExplorer;
using FastEndpoints.DiagnosticSources.Middleware;
using FastEndpoints.Swagger.Swashbuckle;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Models;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Web.Services;

var builder = WebApplication.CreateBuilder();
builder.Services.AddCors();
builder.Services.AddResponseCaching();
builder.Services.AddFastEndpoints();
// builder.Services.AddAuthenticationJWTBearer(builder.Configuration["TokenKey"]);
// builder.Services.AddAuthorization(o => o.AddPolicy("AdminOnly", b => b.RequireRole(Role.Admin)));
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddFastEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "My API", Version = "v1" });
    c.CustomSchemaIds( type => type.ToString() );
    c.OperationFilter<FastEndpointsOperationFilter>();
});

// Define some important constants and the activity source
var serviceName = "Web";
var serviceVersion = "1.0.0";

// Configure important OpenTelemetry settings, the console exporter, and instrumentation library
builder.Services.AddOpenTelemetryTracing(b =>
{
    b
        .AddConsoleExporter()
        .AddSource(serviceName)
        .SetResourceBuilder(
            ResourceBuilder.CreateDefault()
                .AddService(serviceName: serviceName, serviceVersion: serviceVersion))
        .AddHttpClientInstrumentation()
        .AddAspNetCoreInstrumentation()
        .AddSource(FastEndpoints.DiagnosticSources.Trace.ActivitySourceName);
});

var app = builder.Build();

var supportedCultures = new[] { new CultureInfo("en-US") };
app.UseRequestLocalization(new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture("en-US"),
    SupportedCultures = supportedCultures,
    SupportedUICultures = supportedCultures
});

app.UseDefaultExceptionHandler();
app.UseResponseCaching();

app.UseRouting(); //if using, this call must go before auth/cors/fastendpoints middleware

app.UseCors(b => b.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
// app.UseAuthentication();
// app.UseAuthorization();

app.UseFastEndpointsDiagnosticsMiddleware();
app.UseFastEndpoints(config =>
{
    config.ShortEndpointNames = false;
    config.SerializerOptions = o => o.PropertyNamingPolicy = null;
    config.EndpointRegistrationFilter = ep => ep.Tags?.Contains("exclude") is not true;
    config.GlobalEndpointOptions = (epDef, builder) =>
    {
        if (epDef.Tags?.Contains("orders") is true)
            builder.Produces<ErrorResponse>(400, "application/problem+json");
    };
    config.RoutingOptions = o => o.Prefix = "api";
    config.VersioningOptions = o =>
    {
        o.Prefix = "v";
        //o.DefaultVersion = 1; 
        //o.SuffixedVersion = false; 
    };
    config.ThrottleOptions = o =>
    {
        o.HeaderName = "X-Custom-Throttle-Header";
        o.ThrottledResponse = "Custom Error Response";
    };
});

// app.UseEndpoints(c =>
// {
//     c.MapGet("test/{testId:int?}", (int? testId, [FromQuery] IEnumerable<string> data) => $"hello {testId}").WithTags("map-get");
// });

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "v1");
});

app.Run();

public partial class Program { }
