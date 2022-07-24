using System.Reflection;
using JetBrains.Annotations;

namespace FastEndpoints.Reflection.Extensions;

public static class FastEndpointsDefinitionExtensions
{
    private static readonly PropertyInfo[] PropertyInfos;
    private static readonly FieldInfo[] FieldInfos;

    static FastEndpointsDefinitionExtensions()
    {
        var searchPropertyFlags = BindingFlags.Static | BindingFlags.Instance | 
                                  BindingFlags.Public | BindingFlags.NonPublic |
                                  BindingFlags.FlattenHierarchy;
        PropertyInfos = typeof(EndpointDefinition).GetProperties(searchPropertyFlags);
        FieldInfos = typeof(EndpointDefinition).GetFields(searchPropertyFlags);
    }

    [CanBeNull]
    public static IPreProcessor<TRequest>[] GetPreProcessors<TRequest>(this EndpointDefinition endpointDefinition)
    {
        return GetPropertyValue<IPreProcessor<TRequest>[]>(endpointDefinition, "PreProcessors");
    }
    
    [CanBeNull]
    public static object[] GetPreProcessors(this EndpointDefinition endpointDefinition)
    {
        return GetPropertyValue<object[]>(endpointDefinition, "PreProcessors");
    }

    [CanBeNull]
    public static IPostProcessor<TRequest, TResponse>[] GetPostProcessors<TRequest, TResponse>(this EndpointDefinition endpointDefinition)
    {
        return GetPropertyValue<IPostProcessor<TRequest, TResponse>[]>(endpointDefinition, "PostProcessors");
    }
    
    [CanBeNull]
    public static object[] GetPostProcessors(this EndpointDefinition endpointDefinition)
    {
        return GetPropertyValue<object[]>(endpointDefinition, "PostProcessors");
    }
    
    public static bool GetExecuteAsyncImplemented(this EndpointDefinition endpointDefinition)
    {
        return GetPropertyValue<bool>(endpointDefinition, "ExecuteAsyncImplemented");
    }
    
    public static void SetExecuteAsyncImplemented(this EndpointDefinition endpointDefinition, bool executeAsyncImplemented)
    {
        SetPropertyValue(endpointDefinition, "ExecuteAsyncImplemented", executeAsyncImplemented);
    }

    public static T GetPropertyValue<T>(this EndpointDefinition endpointDefinition, string propertyName)
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
    
    public static void SetPropertyValue<T>(this EndpointDefinition endpointDefinition, string propertyName, T value)
    {
        var property = PropertyInfos.FirstOrDefault(x => x.Name.Equals(propertyName));
        if (property != null)
        {
            if (property.CanWrite)
            {
                property.SetValue(endpointDefinition, value);
            }
            else
            {
                throw new ArgumentException($"{propertyName} can not be set");
            }
        }
        
        var field = FieldInfos.FirstOrDefault(x => x.Name.Equals(propertyName));
        if (field != null)
        {
            if (!field.IsInitOnly)
            {
                field.SetValue(endpointDefinition, value);
            }
            else
            {
                throw new ArgumentException($"{propertyName} can not be set");
            }
        }

        throw new ArgumentException($"{propertyName} not found");
    }
}