using System.Reflection;
using System.Text.Json;
using FastEndpoints.ApiExplorer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace FastEndpoints.Swagger.Swashbuckle;

/// <summary>
/// a set of extension methods for adding swagger support
/// </summary>
public static class Extensions
{
    public static IServiceCollection AddSwaggerDoc(this IServiceCollection services,
        string name,
        Action<OpenApiInfo> action)
    {
        var openApiInfo = new OpenApiInfo();
        action.Invoke(openApiInfo);

        return AddSwaggerDoc(services, name, openApiInfo);
    }

    public static IServiceCollection AddSwaggerDoc(this IServiceCollection services,
        string name,
        OpenApiInfo openApiInfo)
    {
        services.AddFastEndpointsApiExplorer();

        services.Configure<SwaggerGenOptions>(c =>
        {
            c.SwaggerDoc(name, openApiInfo);

            if (!c.OperationFilterDescriptors.Any(x => x.Type == typeof(FastEndpointsOperationFilter)))
            {
                c.OperationFilter<FastEndpointsOperationFilter>();
            }
        });

        return services;
    }

    public static IServiceCollection AddSwaggerAuth(this IServiceCollection services,
        string schemeName,
        OpenApiSecurityScheme securityScheme)
    {
        services.Configure<SwaggerGenOptions>(c =>
        {
            c.AddSwaggerAuth(schemeName, securityScheme);
        });
        
        return services;
    }
    
    public static SwaggerGenOptions AddSwaggerAuth(this SwaggerGenOptions swaggerGenOptions,
        string schemeName,
        OpenApiSecurityScheme securityScheme)
    {
        swaggerGenOptions.AddSecurityDefinition(schemeName, securityScheme);

        swaggerGenOptions.OperationFilterDescriptors.Add(new FilterDescriptor
        {
            Type = typeof(FastEndpointsOperationSecurityFilter),
            Arguments = new[] { schemeName }
        });
        
        return swaggerGenOptions;
    }
    
    public static void ConfigureDefaults(this SwaggerGenOptions swaggerGenOptions, 
        bool groupByVersion = true,
        bool showNoGroupInAllDocuments = true,
        Action<SwaggerGenOptions> settings = null)
    {
        swaggerGenOptions.TagActionsBy(api =>
        {
            var epDefinition =
                (EndpointDefinition)api.ActionDescriptor.EndpointMetadata.FirstOrDefault(x =>
                    x is EndpointDefinition);
            return new List<string>
            {
                epDefinition == null
                    ? api.ActionDescriptor.RouteValues["controller"]
                    : epDefinition.EndpointType.Namespace?.Split(".", StringSplitOptions.RemoveEmptyEntries)
                        .LastOrDefault()
            };
        });

        if (groupByVersion)
        {
            swaggerGenOptions.DocInclusionPredicate((docName, apiDesc) =>
            {
                if (showNoGroupInAllDocuments && string.IsNullOrEmpty(apiDesc.GroupName))
                {
                    return true;
                }
                
                return docName == apiDesc.GroupName;
            });
        }
        
        swaggerGenOptions.CustomSchemaIds(type => type.FullName);
        
        settings?.Invoke(swaggerGenOptions);
    }
}