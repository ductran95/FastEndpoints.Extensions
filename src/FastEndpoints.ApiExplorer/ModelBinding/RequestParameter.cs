using System.Reflection;
using Microsoft.AspNetCore.Routing.Patterns;

namespace FastEndpoints.ApiExplorer.ModelBinding;

public class RequestParameter
{
    public List<FastEndpointPropertyInfo> Properties { get; set; }
    public List<FromPropertyInfo> FromProperties { get; set; }
    public List<FromClaimPropertyInfo> FromClaimProperties { get; set; }
    public List<FromHeaderPropertyInfo> FromHeaderProperties { get; set; }
    public List<BindFromPropertyInfo> BindFromProperties { get; set; }
    public List<QueryParamPropertyInfo> QueryParamProperties { get; set; }

    public RequestParameter()
    {
    }

    public RequestParameter(List<FastEndpointPropertyInfo> properties)
    {
        Properties = properties;
        ParseProperties();
    }

    public void ParseProperties()
    {
        FromProperties = Properties
            .Where(x => x.FromAttribute != null)
            .Select(x => new FromPropertyInfo
            {
                PropertyInfo = x.PropertyInfo,
                FromAttribute = x.FromAttribute
            })
            .ToList();

        FromClaimProperties = Properties
            .Where(x => x.FromClaimAttribute != null)
            .Select(x => new FromClaimPropertyInfo
            {
                PropertyInfo = x.PropertyInfo,
                FromClaimAttribute = x.FromClaimAttribute
            })
            .ToList();

        FromHeaderProperties = Properties
            .Where(x => x.FromHeaderAttribute != null)
            .Select(x => new FromHeaderPropertyInfo
            {
                PropertyInfo = x.PropertyInfo,
                FromHeaderAttribute = x.FromHeaderAttribute
            })
            .ToList();

        BindFromProperties = Properties
            .Where(x => x.BindFromAttribute != null)
            .Select(x => new BindFromPropertyInfo
            {
                PropertyInfo = x.PropertyInfo,
                BindFromAttribute = x.BindFromAttribute
            })
            .ToList();

        QueryParamProperties = Properties
            .Where(x => x.QueryParamAttribute != null)
            .Select(x => new QueryParamPropertyInfo
            {
                PropertyInfo = x.PropertyInfo,
                QueryParamAttribute = x.QueryParamAttribute
            })
            .ToList();
    }

    public List<PropertyInfo> GetRouteParamProperties(IEnumerable<RoutePatternParameterPart> routePatternParameterParts)
    {
        var routeParamNames = routePatternParameterParts.Select(x => x.Name);
        return Properties
            .Where(x => x.CanMapFromRoute && routeParamNames.Contains(x.MappedName))
            .Select(x => x.PropertyInfo)
            .ToList();
    }
}

public class FastEndpointPropertyInfo
{
    public PropertyInfo PropertyInfo { get; set; }
    public FromAttribute FromAttribute { get; set; }
    public FromClaimAttribute FromClaimAttribute { get; set; }
    public FromHeaderAttribute FromHeaderAttribute { get; set; }
    public BindFromAttribute BindFromAttribute { get; set; }
    public QueryParamAttribute QueryParamAttribute { get; set; }

    public string MappedName
    {
        get
        {
            var name = PropertyInfo.Name;

            if (FromAttribute != null)
            {
                name = FromAttribute.ClaimType;
            }

            if (FromClaimAttribute != null)
            {
                name = FromClaimAttribute.ClaimType;
            }

            if (FromHeaderAttribute != null)
            {
                name = FromHeaderAttribute.HeaderName;
            }

            if (BindFromAttribute != null)
            {
                name = BindFromAttribute.Name;
            }

            return name;
        }
    }

    public bool CanMapFromRoute
    {
        get { return true; }
    }
}

public class FromPropertyInfo
{
    public PropertyInfo PropertyInfo { get; set; }
    public FromAttribute FromAttribute { get; set; }
}

public class FromClaimPropertyInfo
{
    public PropertyInfo PropertyInfo { get; set; }
    public FromClaimAttribute FromClaimAttribute { get; set; }
}

public class FromHeaderPropertyInfo
{
    public PropertyInfo PropertyInfo { get; set; }
    public FromHeaderAttribute FromHeaderAttribute { get; set; }
}

public class BindFromPropertyInfo
{
    public PropertyInfo PropertyInfo { get; set; }
    public BindFromAttribute BindFromAttribute { get; set; }
}

public class QueryParamPropertyInfo
{
    public PropertyInfo PropertyInfo { get; set; }
    public QueryParamAttribute QueryParamAttribute { get; set; }
}