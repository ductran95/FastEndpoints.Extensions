using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace FastEndpoints.Swashbuckle;

public class FastEndpointSchemaFilter: ISchemaFilter, IOperationFilter, IParameterFilter, IRequestBodyFilter, IDocumentFilter
{
    public void Apply(OpenApiSchema schema, SchemaFilterContext context)
    {
        var parameterInfo = context.ParameterInfo;
        _ = parameterInfo?.Name;
    }

    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        _ = context.ApiDescription;
        _ = operation.Parameters;
    }

    public void Apply(OpenApiParameter parameter, ParameterFilterContext context)
    {
        var parameterInfo = context.ApiParameterDescription;
        _ = parameterInfo?.Name;
    }

    public void Apply(OpenApiRequestBody requestBody, RequestBodyFilterContext context)
    {
        var parameterInfo = context.BodyParameterDescription;
        _ = parameterInfo?.Name;
    }

    public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
    {
        var parameterInfo = context.ApiDescriptions;
        _ = swaggerDoc?.Info;
    }
}