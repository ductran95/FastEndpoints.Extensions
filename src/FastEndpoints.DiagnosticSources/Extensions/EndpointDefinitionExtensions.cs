using FastEndpoints.Reflection.Extensions;

namespace FastEndpoints.DiagnosticSources.Extensions;

public static class EndpointDefinitionExtensions
{
    public static string GetActivityName(this BaseEndpoint endpoint)
    {
        return $"{endpoint.GetEndpointName()}.{(endpoint.Definition.GetExecuteAsyncImplemented() ? "ExecuteAsync" : "HandleAsync")}";
    }
    
    public static string GetActivityName(this EndpointDefinition endpointDefinition)
    {
        return $"{endpointDefinition.GetEndpointName()}.{(endpointDefinition.GetExecuteAsyncImplemented() ? "ExecuteAsync" : "HandleAsync")}";
    }

    public static string GetEndpointName(this BaseEndpoint endpoint)
    {
        return endpoint.Definition.Summary?.Summary ?? endpoint.GetType().FullName;
    }
    
    public static string GetEndpointName(this EndpointDefinition endpointDefinition)
    {
        return endpointDefinition.Summary?.Summary ?? endpointDefinition.EndpointType.FullName;
    }
}