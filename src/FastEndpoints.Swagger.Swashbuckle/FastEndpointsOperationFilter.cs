using System.Buffers;
using System.Reflection;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace FastEndpoints.Swagger.Swashbuckle;
using BindingSource = Microsoft.AspNetCore.Mvc.ModelBinding.BindingSource;

public class FastEndpointsOperationFilter : IOperationFilter
{
 public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var endpointDefinition = context.ApiDescription.ActionDescriptor.EndpointMetadata
            .OfType<EndpointDefinition>()
            .FirstOrDefault();

        if (endpointDefinition == null)
        {
            return;
        }

        operation.Summary = endpointDefinition.EndpointSummary?.Summary;
        operation.Description = endpointDefinition.EndpointSummary?.Description;

        GenerateRequestBodyDefinitions(operation, context, endpointDefinition);
        GenerateResponseDefinitions(operation, context, endpointDefinition);
    }

    private void GenerateResponseDefinitions(OpenApiOperation operation, OperationFilterContext context, EndpointDefinition endpointDefinition)
    {
        foreach (var responseType in context.ApiDescription.SupportedResponseTypes)
        {
            if (endpointDefinition.EndpointSummary?.ResponseExamples.ContainsKey(responseType.StatusCode) != true)
            {
                continue;
            }

            var exampleObject = endpointDefinition.EndpointSummary?.ResponseExamples[responseType.StatusCode];
            if (exampleObject == null)
            {
                continue;
            }

            var responseStatusCode = responseType.StatusCode.ToString();
           
            if (!operation.Responses.ContainsKey(responseStatusCode))
            {
                continue;
            }

            var responseDefinition = operation.Responses[responseStatusCode];
            foreach (var openApiMediaType in responseDefinition.Content)
            {
                var schema = openApiMediaType.Value.Schema;

                openApiMediaType.Value.Example =
                    GenerateOpenApiObjectFromExample(schema, context.SchemaRepository, exampleObject);
            }
        }
    }

    private void GenerateRequestBodyDefinitions(OpenApiOperation operation, OperationFilterContext context,
        EndpointDefinition endpointDefinition)
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
                var exampleResponse = endpointDefinition.EndpointSummary?.ExampleRequest;
                IOpenApiAny schemaDefault;
                if (exampleResponse != null)
                {
                    schemaDefault =
                        GenerateOpenApiObjectFromExample(bodyContent.Value.Schema, context.SchemaRepository,
                            exampleResponse);
                }
                else
                {
                    schemaDefault = GenerateOpenApiObject(bodyContent.Value.Schema, context.SchemaRepository);
                }

                OpenApiObject schema = null;
                if (schemaDefault is OpenApiArray schemaArray)
                {
                    schema = schemaArray.FirstOrDefault() as OpenApiObject;
                }
                else if (schemaDefault is OpenApiObject schemaObject)
                {
                    schema = schemaObject;
                }

                var keysToRemoved = schema?.Keys
                    .Where(
                        x => tobeRemovedParameters.Any(y => x.Equals(y, StringComparison.InvariantCultureIgnoreCase)))
                    .ToList();
                if (keysToRemoved != null)
                {
                    foreach (var keyToRemoved in keysToRemoved)
                    {
                        schema.Remove(keyToRemoved);
                    }
                }

                bodyContent.Value.Example = schemaDefault;
            }
        }
    }

    private IOpenApiAny GenerateOpenApiObjectFromExample(OpenApiSchema schema, SchemaRepository schemaRepository,
        object exampleObject)
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
                return new OpenApiBoolean(Convert.ToBoolean(exampleObject));

            case "number":
                switch (realSchema.Format)
                {
                    case "float":
                        return new OpenApiFloat(Convert.ToSingle(exampleObject));
                    case "double":
                        return new OpenApiDouble(Convert.ToDouble(exampleObject));
                    default:
                        return null;
                }

            case "integer":
                switch (realSchema.Format)
                {
                    case "int32":
                        return new OpenApiInteger(Convert.ToInt32(exampleObject));
                    case "int64":
                        return new OpenApiLong(Convert.ToInt64(exampleObject));
                    default:
                        return null;
                }

            case "string":
                switch (realSchema.Format)
                {
                    case "byte":
                        return new OpenApiByte(Convert.ToByte(exampleObject.ToString()));
                    case "date-time":
                        return new OpenApiDateTime(DateTimeOffset.Parse(exampleObject.ToString()!));
                    case "uuid":
                        return new OpenApiString(exampleObject.ToString());
                    case "date-span":
                        return new OpenApiString(exampleObject.ToString());
                    default:
                        return new OpenApiString(exampleObject.ToString());
                }

            case "array":
                var openArray = new OpenApiArray();
                if (exampleObject is Array arr)
                    foreach (var value in arr)
                    {
                        openArray.Add(GenerateOpenApiObjectFromExample(realSchema.Items, schemaRepository, value));
                    }

                return openArray;

            case "object":
                var openObject = new OpenApiObject();
                var exampleObjectType = exampleObject.GetType();
                //Only Dictionary<string, ?> is supported
                if (!realSchema.Properties.Any() && exampleObjectType.IsGenericType &&
                    (exampleObjectType.GetGenericTypeDefinition() == typeof(IDictionary<,>) ||
                     exampleObjectType.GetGenericTypeDefinition() == typeof(Dictionary<,>)) &&
                    exampleObjectType.GenericTypeArguments.FirstOrDefault() == typeof(string))
                {
                    //For dictionaryProperties no schema is generated so just use keys and values
                    //Also to make it easy we use hack to call generic method
                    var thisType = this.GetType();
                    var methodInfo = thisType.GetMethod(nameof(ProcessDictionary),
                        BindingFlags.Instance | BindingFlags.NonPublic);
                    var callableMethodInfo = methodInfo?.MakeGenericMethod(exampleObjectType.GenericTypeArguments[1]);
                    callableMethodInfo?.Invoke(this, new[] { openObject, exampleObject });
                }

                foreach (var schemaProperty in realSchema.Properties)
                {
                    var property = GetProperty(exampleObjectType, schemaProperty);
                    var propertyValue = property?.GetValue(exampleObject);
                    openObject.Add(schemaProperty.Key,
                        propertyValue == null
                            ? GenerateOpenApiObject(schemaProperty.Value, schemaRepository)
                            : GenerateOpenApiObjectFromExample(schemaProperty.Value, schemaRepository, propertyValue));
                }

                return openObject;

            default:
                return null;
        }
    }

    private void ProcessDictionary<TValue>(OpenApiObject openApiObject, IDictionary<string, TValue> dictionary)
    {
        foreach (var keyValuePair in dictionary)
        {
            if (keyValuePair.Value != null)
            {
                openApiObject.Add(keyValuePair.Key, GenerateOpenApiObjectFromObject(keyValuePair.Value));
            }
        }
    }

    private IOpenApiAny GenerateOpenApiObjectFromObject(object value)
    {
        var type = value.GetType();

        //For now only primitive value types and strings are supported
        if (type.IsValueType || type == typeof(string))
        {
            return value switch
            {
                int i => new OpenApiInteger(i),
                long l => new OpenApiLong(l),
                float f => new OpenApiFloat(f),
                double d => new OpenApiDouble(d),
                decimal dec => new OpenApiDouble(Convert.ToDouble(dec)),
                string s => new OpenApiString(s),
                byte b => new OpenApiByte(b),
                Guid g => new OpenApiString(g.ToString()),
                DateTimeOffset d => new OpenApiDateTime(d),
                DateTime dt => new OpenApiDate(dt),
                bool boolean => new OpenApiBoolean(boolean),
                _ => throw new ArgumentOutOfRangeException(nameof(value), value, null)
            };
        }
        //ToDo add else branch for complex types
        return new OpenApiNull();

    }

    private static PropertyInfo GetProperty(Type type, KeyValuePair<string, OpenApiSchema> schemaProperty)
    {
        var propertyInfo = type.GetProperty(schemaProperty.Key, BindingFlags.Instance | BindingFlags.Public);
        if (propertyInfo == null)
        {
            var firstChar = schemaProperty.Key[0];
            //Check if is camelCase
            if (char.IsLower(firstChar))
            {
                var upperCamelCaseKey = ToUpperCamelCaseKey(schemaProperty.Key);
                propertyInfo = type.GetProperty(upperCamelCaseKey, BindingFlags.Instance | BindingFlags.Public);
            }
        }
        return propertyInfo;
    }

    private static string ToUpperCamelCaseKey(string key)
    {
        var firstChar = key[0];
        var keySpan = key.AsSpan();
        var buffer = ArrayPool<char>.Shared.Rent(keySpan.Length);
        var span = new Span<char>(buffer) { [0] = Char.ToUpper(firstChar) };
        keySpan[1..keySpan.Length].CopyTo(span[1..keySpan.Length]);
        var upperCamelCaseKey = new string(span[..keySpan.Length]);
        ArrayPool<char>.Shared.Return(buffer);
        return upperCamelCaseKey;
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
                var openArray = new OpenApiArray { GenerateOpenApiObject(realSchema.Items, schemaRepository) };
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