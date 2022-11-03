using System.Reflection;
using JetBrains.Annotations;

namespace FastEndpoints.Reflection.Extensions;

public static class VersioningOptionsExtensions
{
    private static readonly PropertyInfo[] PropertyInfos;
    private static readonly FieldInfo[] FieldInfos;

    static VersioningOptionsExtensions()
    {
        var searchPropertyFlags = BindingFlags.Static | BindingFlags.Instance | 
                                  BindingFlags.Public | BindingFlags.NonPublic |
                                  BindingFlags.FlattenHierarchy;
        PropertyInfos = typeof(VersioningOptions).GetProperties(searchPropertyFlags);
        FieldInfos = typeof(VersioningOptions).GetFields(searchPropertyFlags);
    }
    
    public static int GetDefaultVersion(this VersioningOptions versioningOptions)
    {
        return GetPropertyValue<int>(versioningOptions, "DefaultVersion");
    }
    
    public static string GetPrefix(this VersioningOptions versioningOptions)
    {
        return GetPropertyValue<string>(versioningOptions, "Prefix");
    }
    
    public static T GetPropertyValue<T>(this VersioningOptions versioningOptions, string propertyName)
    {
        var property = PropertyInfos.FirstOrDefault(x => x.Name.Equals(propertyName));
        if (property != null)
        {
            return (T)property.GetValue(versioningOptions);
            
        }
        
        var field = FieldInfos.FirstOrDefault(x => x.Name.Equals(propertyName));
        if (field != null)
        {
            return (T)field.GetValue(versioningOptions);
        }

        throw new ArgumentException($"{propertyName} not found");
    }
    
    public static void SetPropertyValue<T>(this VersioningOptions versioningOptions, string propertyName, T value)
    {
        var property = PropertyInfos.FirstOrDefault(x => x.Name.Equals(propertyName));
        if (property != null)
        {
            if (property.CanWrite)
            {
                property.SetValue(versioningOptions, value);
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
                field.SetValue(versioningOptions, value);
            }
            else
            {
                throw new ArgumentException($"{propertyName} can not be set");
            }
        }

        throw new ArgumentException($"{propertyName} not found");
    }
}