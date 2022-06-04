using System.Reflection;

namespace FastEndpoints.ApiExplorer.Extensions;

public static class PropertyInfoExtensions
{
    private static readonly NullabilityInfoContext nullCtx = new();
    public static bool IsNullable(this PropertyInfo p) => nullCtx.Create(p).WriteState == NullabilityState.Nullable || p.PropertyType.IsNullable();   
    // public static bool IsNullable(this PropertyInfo p) => p.PropertyType.IsNullable();   
}