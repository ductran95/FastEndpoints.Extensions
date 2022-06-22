using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace FastEndpoints.Swagger.Swashbuckle;

public class FastEndpointsOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var endpointDefinition = context.ApiDescription.ActionDescriptor.EndpointMetadata
                .OfType<EndpointDefinition>()
                .FirstOrDefault();

        if (endpointDefinition != null)
        {
            var requestBody =
                context.ApiDescription.ParameterDescriptions.FirstOrDefault(x => x.Source == BindingSource.Body);
            if (requestBody != null)
            {
                var tobeRemovedParameters = requestBody.ModelMetadata.Properties.Where(x =>
                        x.AdditionalValues.TryGetValue(nameof(JsonIgnoreAttribute), out var value) &&
                        value is true)
                    .Select(x => x.Name)
                    .ToList();

                foreach (var bodyContent in operation.RequestBody.Content)
                {
                    var schemaDefault = GenerateOpenApiObject(bodyContent.Value.Schema, context.SchemaRepository);
                    var schemaObject = (OpenApiObject) schemaDefault;
                    var keysToRemoved = schemaObject.Keys
                        .Where(x => tobeRemovedParameters.Any(y => x.Equals(y, StringComparison.InvariantCultureIgnoreCase)))
                        .ToList();
                    foreach (var keyToRemoved in keysToRemoved)
                    {
                        schemaObject.Remove(keyToRemoved);
                    }
                    bodyContent.Value.Example = schemaDefault;
                }
            }
        }
    }

    private IOpenApiAny GenerateOpenApiObject(OpenApiSchema schema, SchemaRepository schemaRepository)
    {
        var realSchema = schema;
        if (schema.Reference != null)
        {
            var reference = schema.Reference.Id;
            realSchema = schemaRepository.Schemas[reference];
        }

        switch (realSchema.Type)
        {
            case "boolean":
                return new OpenApiBoolean(true);

            case "number":
                switch (realSchema.Format)
                {
                    case "float":
                        return new OpenApiFloat(0);
                    case "double":
                        return new OpenApiDouble(0);
                    default:
                        return null;
                }

            case "integer":
                switch (realSchema.Format)
                {
                    case "int32":
                        return new OpenApiInteger(0);
                    case "int64":
                        return new OpenApiLong(0);
                    default:
                        return null;
                }

            case "string":
                switch (realSchema.Format)
                {
                    case "byte":
                        return new OpenApiByte(0);
                    case "date-time":
                        return new OpenApiDateTime(DateTimeOffset.UtcNow);
                    case "uuid":
                        return new OpenApiString(Guid.NewGuid().ToString());
                    case "date-span":
                        return new OpenApiString(TimeSpan.Zero.ToString());
                    default:
                        return new OpenApiString("string");
                }

            case "array":
                var openArray = new OpenApiArray();
                openArray.Add(GenerateOpenApiObject(realSchema.Items, schemaRepository));
                return openArray;

            case "object":
                var openObject = new OpenApiObject();
                foreach (var schemaProperty in realSchema.Properties)
                {
                    openObject.Add(schemaProperty.Key, GenerateOpenApiObject(schemaProperty.Value, schemaRepository));
                }

                return openObject;

            default:
                return null;
        }
    }
}