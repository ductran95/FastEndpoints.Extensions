using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace FastEndpoints.Swagger.Swashbuckle;

public class FastEndpointsOperationSecurityFilter : IOperationFilter
{
    private readonly string _schemeName;

    public FastEndpointsOperationSecurityFilter(string schemeName)
    {
        _schemeName = schemeName;
    }

    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var epMeta = context.ApiDescription.ActionDescriptor.EndpointMetadata;

        if (epMeta is null)
            return;

        if (epMeta.OfType<AllowAnonymousAttribute>().Any() || !epMeta.OfType<AuthorizeAttribute>().Any())
            return;

        var epDef = epMeta.OfType<EndpointDefinition>().SingleOrDefault();

        if (epDef == null)
        {
            if (epMeta.OfType<ControllerAttribute>().Any()) // it is an ApiController
                return; // todo: return false if the documentation of such ApiControllers is not wanted.

            throw new InvalidOperationException(
                $"Endpoint `{context.ApiDescription.GroupName}` is missing an endpoint description. " +
                "This may indicate an MvcController. Consider adding `[ApiExplorerSettings(IgnoreApi = true)]`");
        }

        var epSchemes = epDef.AuthSchemeNames;
        if (epSchemes?.Contains(_schemeName) == false)
            return;

        var oAuthScheme = new OpenApiSecurityScheme
        {
            Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = _schemeName }
        };
        
        operation.Security = new List<OpenApiSecurityRequirement>
        {
            new OpenApiSecurityRequirement
            {
                [oAuthScheme] = BuildScopes(epMeta.OfType<AuthorizeAttribute>())
            }
        };
    }
    
    private static List<string> BuildScopes(IEnumerable<AuthorizeAttribute> authorizeAttributes)
    {
        return authorizeAttributes
            .Where(a => a.Roles != null)
            .SelectMany(a => a.Roles!.Split(','))
            .Distinct()
            .ToList();
    }
}