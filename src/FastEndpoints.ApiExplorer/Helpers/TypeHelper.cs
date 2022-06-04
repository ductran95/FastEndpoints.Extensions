using System.Collections;

namespace FastEndpoints.ApiExplorer.Helpers;

public static class TypeHelper
{
    public static object CreateDefaultValue(Type type)
    {
        try
        {
            return Activator.CreateInstance(type);
        }
        catch (Exception)
        {
            if (type == typeof(string))
            {
                return string.Empty;
            }

            if (Nullable.GetUnderlyingType(type) != null)
            {
                return null;
            }
            
            if (type.GetInterfaces().Contains(typeof(IEnumerable)))
            {
                return null;
            }
            
            throw;
        }
    }
}