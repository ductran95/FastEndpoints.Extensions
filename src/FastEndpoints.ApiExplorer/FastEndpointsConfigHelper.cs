using System.Reflection;
using System.Text.Json;

namespace FastEndpoints.ApiExplorer;

public static class FastEndpointsConfigHelper
{
    private static readonly PropertyInfo[] PropertyInfos;
    
    static FastEndpointsConfigHelper()
    {
        var searchPropertyFlags =  BindingFlags.Static | BindingFlags.Public |
                              BindingFlags.NonPublic |
                              BindingFlags.FlattenHierarchy;
        PropertyInfos = typeof(Config).GetProperties(searchPropertyFlags);
    }

    public static JsonSerializerOptions GetSerializerOpts()
    {
        return GetPropertyValue<JsonSerializerOptions>("JsonSerializerOptions");
    }
    
    public static ThrottleOptions GetThrottleOpts()
    {
        return GetPropertyValue<ThrottleOptions>("ThrottleOpts");
    }
    
    public static RoutingOptions GetRoutingOpts()
    {
        return GetPropertyValue<RoutingOptions>("RoutingOpts");
    }
    
    public static VersioningOptions GetVersioningOpts()
    {
        return GetPropertyValue<VersioningOptions>("VersioningOpts");
    }

    public static T GetPropertyValue<T>(string propertyName)
    {
        var property = PropertyInfos.FirstOrDefault(x=>x.Name.Equals(propertyName));
        if (property == null)
        {
            throw new ArgumentException($"{propertyName} not found");
        }

        return (T) property.GetValue(null);
    }
}