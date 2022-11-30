using FastEndpoints.Reflection.Extensions;

namespace FastEndpoints.OpenTelemetry.Extensions;

internal static class EndpointDefinitionExtensions
{
    internal static string GetActivityName(this BaseEndpoint endpoint)
    {
        return $"{endpoint.GetEndpointName()}.{(endpoint.Definition.GetExecuteAsyncImplemented() ? "ExecuteAsync" : "HandleAsync")}";
    }
    
    internal static string GetActivityName(this EndpointDefinition endpointDefinition)
    {
        return $"{endpointDefinition.GetEndpointName()}.{(endpointDefinition.GetExecuteAsyncImplemented() ? "ExecuteAsync" : "HandleAsync")}";
    }

    internal static string GetEndpointName(this BaseEndpoint endpoint)
    {
        return endpoint.Definition.EndpointSummary?.Summary ?? endpoint.GetType().FullName;
    }
    
    internal static string GetEndpointName(this EndpointDefinition endpointDefinition)
    {
        return endpointDefinition.EndpointSummary?.Summary ?? endpointDefinition.EndpointType.FullName;
    }
}