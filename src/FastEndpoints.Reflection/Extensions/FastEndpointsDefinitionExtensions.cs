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
    public static List<object> GetPreProcessorList(this EndpointDefinition endpointDefinition)
    {
        return GetPropertyValue<List<object>>(endpointDefinition, "PreProcessorList");
    }
    
    [CanBeNull]
    public static List<object> GetPostProcessorList(this EndpointDefinition endpointDefinition)
    {
        return GetPropertyValue<List<object>>(endpointDefinition, "PostProcessorList");
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