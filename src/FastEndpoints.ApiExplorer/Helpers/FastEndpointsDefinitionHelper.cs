using System.Reflection;
using System.Text.Json;
using JetBrains.Annotations;

namespace FastEndpoints.ApiExplorer.Helpers;

public static class FastEndpointsDefinitionHelper
{
    private static readonly PropertyInfo[] PropertyInfos;
    private static readonly FieldInfo[] FieldInfos;

    static FastEndpointsDefinitionHelper()
    {
        var searchPropertyFlags = BindingFlags.Static | BindingFlags.Instance | 
                                  BindingFlags.Public | BindingFlags.NonPublic |
                                  BindingFlags.FlattenHierarchy;
        PropertyInfos = typeof(EndpointDefinition).GetProperties(searchPropertyFlags);
        FieldInfos = typeof(EndpointDefinition).GetFields(searchPropertyFlags);
    }

    [CanBeNull]
    public static IPreProcessor<TRequest>[] GetPreProcessors<TRequest>(EndpointDefinition endpointDefinition)
    {
        return GetPropertyValue<IPreProcessor<TRequest>[]>(endpointDefinition, "PreProcessors");
    }
    
    [CanBeNull]
    public static object[] GetPreProcessors(EndpointDefinition endpointDefinition)
    {
        return GetPropertyValue<object[]>(endpointDefinition, "PreProcessors");
    }

    [CanBeNull]
    public static IPostProcessor<TRequest, TResponse>[] GetPostProcessors<TRequest, TResponse>(EndpointDefinition endpointDefinition)
    {
        return GetPropertyValue<IPostProcessor<TRequest, TResponse>[]>(endpointDefinition, "PostProcessors");
    }
    
    [CanBeNull]
    public static object[] GetPostProcessors(EndpointDefinition endpointDefinition)
    {
        return GetPropertyValue<object[]>(endpointDefinition, "PostProcessors");
    }

    public static T GetPropertyValue<T>(EndpointDefinition endpointDefinition, string propertyName)
    {
        var property = PropertyInfos.FirstOrDefault(x => x.Name.Equals(propertyName));
        if (property != null)
        {
            return (T)property.GetValue(endpointDefinition);
            
        }
        
        var field = FieldInfos.FirstOrDefault(x => x.Name.Equals(propertyName));
        if (field != null)
        {
            return (T)field.GetValue(endpointDefinition);
        }

        throw new ArgumentException($"{propertyName} not found");
    }
}