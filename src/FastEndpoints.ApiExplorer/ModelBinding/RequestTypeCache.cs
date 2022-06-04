using System.Reflection;

namespace FastEndpoints.ApiExplorer.ModelBinding;

public class RequestTypeCache: IRequestTypeCache
{
    private readonly Dictionary<Type, RequestParameter> _requestTypes;

    public RequestTypeCache()
    {
        _requestTypes = new Dictionary<Type, RequestParameter>();
    }

    public RequestParameter GetRequestParameter<T>() where T : class
    {
        return GetRequestParameter(typeof(T));
    }
    
    public RequestParameter GetRequestParameter(Type type)
    {
        if (!_requestTypes.TryGetValue(type, out var requestType))
        {
            var fastEndpointPropertyInfos = new List<FastEndpointPropertyInfo>();
            
            var properties = type.GetProperties(
                BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy);

            foreach (var property in properties)
            {
                if (!property.CanRead || !property.CanWrite)
                    continue;

                var fastEndpointPropertyInfo = new FastEndpointPropertyInfo();

                fastEndpointPropertyInfo.PropertyInfo = property;
                fastEndpointPropertyInfo.FromAttribute = property.GetCustomAttribute<FromAttribute>(false);
                fastEndpointPropertyInfo.FromClaimAttribute = property.GetCustomAttribute<FromClaimAttribute>(false);
                fastEndpointPropertyInfo.FromHeaderAttribute = property.GetCustomAttribute<FromHeaderAttribute>(false);
                fastEndpointPropertyInfo.BindFromAttribute = property.GetCustomAttribute<BindFromAttribute>(false);
                fastEndpointPropertyInfo.QueryParamAttribute = property.GetCustomAttribute<QueryParamAttribute>(false);
                
                fastEndpointPropertyInfos.Add(fastEndpointPropertyInfo);
            }

            requestType = new RequestParameter(fastEndpointPropertyInfos);
        }

        return requestType;
    }
}