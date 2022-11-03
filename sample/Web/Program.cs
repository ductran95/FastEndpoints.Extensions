global using FastEndpoints;
global using FastEndpoints.Security;
global using Web.Auth;
using Microsoft.AspNetCore.Localization;
using System.Globalization;
using FastEndpoints.ApiExplorer;
using FastEndpoints.DiagnosticSources.Middleware;
using FastEndpoints.Swagger.Swashbuckle;
using Microsoft.OpenApi.Models;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Web.PipelineBehaviors.PreProcessors;
using Web.Services;

var builder = WebApplication.CreateBuilder();
builder.Services.AddCors();
builder.Services.AddResponseCaching();
builder.Services.AddFastEndpoints();
builder.Services.AddAuthenticationJWTBearer(builder.Configuration["TokenKey"]!);
builder.Services.AddAuthorization(o => o.AddPolicy("AdminOnly", b => b.RequireRole(Role.Admin)));
builder.Services.AddScoped<IEmailService, EmailService>();

// Swagger
builder.Services.AddSwaggerGen(c =>
{
    c.ConfigureDefaults(groupByVersion: true, showNoGroupInAllDocuments: true);
});

builder.Services.AddSwaggerDoc("v1", c =>
{
    c.Title = "Web API v1";
    c.Version = "v1";
});

builder.Services.AddSwaggerDoc("v2", c =>
{
    c.Title = "Web API v2";
    c.Version = "v2";
});

builder.Services.AddSwaggerAuth("ApiKey", new()
{
    Name = "api_key",
    In = ParameterLocation.Header,
    Type = SecuritySchemeType.ApiKey,
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
app.UseAuthentication();
app.UseAuthorization();

app.UseFastEndpointsDiagnosticsMiddleware();
app.UseFastEndpoints(c =>
{
    c.Serializer.Options.PropertyNamingPolicy = null;

    c.Binding.ValueParserFor<Guid>(x => new(Guid.TryParse(x?.ToString(), out var res), res));

    c.Endpoints.RoutePrefix = "api";
    c.Endpoints.ShortNames = false;
    c.Endpoints.Filter = ep => ep.EndpointTags?.Contains("exclude") is not true;
    c.Endpoints.Configurator = (ep) =>
    {
        ep.PreProcessors(Order.Before, new AdminHeaderChecker());
        if (ep.EndpointTags?.Contains("orders") is true)
            ep.Description(b => b.Produces<ErrorResponse>(400, "application/problem+json"));
        
        ep.AddApiExplorerGroupName();
    };

    c.Versioning.Prefix = "v";

    c.Throttle.HeaderName = "X-Custom-Throttle-Header";
    c.Throttle.Message = "Custom Error Response";
});

//this must go after usefastendpoints (only if using endpoints)
app.UseEndpoints(c =>
{
    c.MapGet("test", () => "hello world!").WithTags("map-get");
    c.MapGet("test/{testId:int?}", (int? testId) => $"hello {testId}").WithTags("map-get");
});

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "v1");
    c.SwaggerEndpoint("/swagger/v2/swagger.json", "v2");
});

app.Run();