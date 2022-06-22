using System.Reflection;
using System.Text.Json.Serialization;
using FastEndpoints.ApiExplorer.Extensions;
using FastEndpoints.ApiExplorer.Helpers;
using FastEndpoints.ApiExplorer.ModelBinding;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Patterns;
using Microsoft.Extensions.Hosting;

namespace FastEndpoints.ApiExplorer.ApiDescriptionProvider;

public class FastEndpointMetadataApiDescriptionProvider : IApiDescriptionProvider
{
    private readonly EndpointDataSource _endpointDataSource;
    private readonly IRequestTypeCache _requestTypeCache;

    // Executes before MVC's DefaultApiDescriptionProvider and after EndpointMetadataApiDescriptionProvider
    public int Order => -1150;

    public FastEndpointMetadataApiDescriptionProvider(
        EndpointDataSource endpointDataSource,
        IRequestTypeCache requestTypeCache
    )
    {
        _endpointDataSource = endpointDataSource;
        _requestTypeCache = requestTypeCache;
    }

    public virtual void OnProvidersExecuting(ApiDescriptionProviderContext context)
    {
    }

    public virtual void OnProvidersExecuted(ApiDescriptionProviderContext context)
    {
        foreach (var apiDescription in context.Results)
        {
            var endpointDefinition =
                apiDescription.ActionDescriptor.EndpointMetadata.OfType<EndpointDefinition>().FirstOrDefault();

            if (endpointDefinition != null)
            {
                UpdateParameters(apiDescription, endpointDefinition);
            }
        }
    }

    private void
        UpdateParameters(ApiDescription apiDescription,
            EndpointDefinition endpointDefinition)
    {
        var requestBody = apiDescription.ParameterDescriptions.FirstOrDefault();

        if (requestBody is not null)
        {
            var apiParamDescriptions = new List<ApiParameterDescription>();
            var isGetOrDeleteRequest = apiDescription.HttpMethod is "GET" or "DELETE";
            var routeParam = RoutePatternFactory.Parse(apiDescription.RelativePath).Parameters;

            var requestType = requestBody.Type;
            var requestParameter = _requestTypeCache.GetRequestParameter(requestType);

            CreateMetadataForRequestBody(requestBody, requestParameter.Properties);

            foreach (var requestParameterProperty in requestParameter.Properties)
            {
                var propertyName = requestParameterProperty.PropertyInfo.Name;

                if (requestParameterProperty.FromHeaderAttribute != null)
                {
                    var apiParam = CreateParameterDescription(requestType,
                        requestParameterProperty,
                        requestParameterProperty.FromHeaderAttribute.HeaderName ?? propertyName,
                        requestParameterProperty.FromHeaderAttribute.IsRequired ||
                        !requestParameterProperty.PropertyInfo.IsNullable(),
                        BindingSource.Header);
                    apiParamDescriptions.Add(apiParam);
                    RemovePropertyFromBody(requestBody, requestParameterProperty.PropertyInfo);

                    continue;
                }

                if (requestParameterProperty.FromClaimAttribute != null ||
                    requestParameterProperty.FromAttribute != null)
                {
                    var apiParam = CreateParameterDescription(requestType,
                        requestParameterProperty,
                        requestParameterProperty.FromClaimAttribute?.ClaimType ??
                        requestParameterProperty.FromAttribute?.ClaimType ?? propertyName,
                        (requestParameterProperty.FromClaimAttribute?.IsRequired
                         ?? requestParameterProperty.FromAttribute?.IsRequired
                         ?? false) ||
                        !requestParameterProperty.PropertyInfo.IsNullable(),
                        BindingSource.Custom);
                    apiParamDescriptions.Add(apiParam);
                    RemovePropertyFromBody(requestBody, requestParameterProperty.PropertyInfo);

                    continue;
                }

                if (requestParameterProperty.QueryParamAttribute != null)
                {
                    var apiParam = CreateParameterDescription(requestType,
                        requestParameterProperty,
                        propertyName,
                        !requestParameterProperty.PropertyInfo.IsNullable(),
                        BindingSource.Query);
                    apiParamDescriptions.Add(apiParam);
                    RemovePropertyFromBody(requestBody, requestParameterProperty.PropertyInfo);

                    continue;
                }

                var isRouteParam = routeParam.Any(x =>
                    x.Name.Equals(propertyName, StringComparison.CurrentCultureIgnoreCase));
                if (isRouteParam)
                {
                    var apiParam = CreateParameterDescription(requestType,
                        requestParameterProperty,
                        propertyName,
                        !requestParameterProperty.PropertyInfo.IsNullable(),
                        BindingSource.Path);
                    apiParamDescriptions.Add(apiParam);
                    RemovePropertyFromBody(requestBody, requestParameterProperty.PropertyInfo);

                    continue;
                }

                if (isGetOrDeleteRequest)
                {
                    var apiParam = CreateParameterDescription(requestType,
                        requestParameterProperty,
                        propertyName,
                        !requestParameterProperty.PropertyInfo.IsNullable(),
                        BindingSource.Query);
                    apiParamDescriptions.Add(apiParam);
                    RemovePropertyFromBody(requestBody, requestParameterProperty.PropertyInfo);
                }
            }

            var isNoBody = requestBody.ModelMetadata.Properties.All(x =>
                x.AdditionalValues.TryGetValue(nameof(JsonIgnoreAttribute), out var jsonIgnore) &&
                jsonIgnore is true);
            if (!isNoBody)
            {
                apiParamDescriptions.Insert(0, requestBody);
            }

            apiDescription.ParameterDescriptions.Clear();
            foreach (var apiParameterDescription in apiParamDescriptions.Where(x => x.Source != BindingSource.Custom))
            {
                apiDescription.ParameterDescriptions.Add(apiParameterDescription);
            }
        }
    }

    private void RemovePropertyFromBody(ApiParameterDescription requestBody, PropertyInfo propertyToRemove)
    {
        var propertyMetadata =
            requestBody.ModelMetadata.Properties.FirstOrDefault(x => x.Name == propertyToRemove.Name);
        if (propertyMetadata != null && propertyMetadata is FastEndpointModelMetadata fastEndpointModelMetadata)
        {
            fastEndpointModelMetadata.AddAdditionalValue(nameof(JsonIgnoreAttribute), true);
        }
    }

    private ApiParameterDescription CreateParameterDescription(Type requestType,
        FastEndpointPropertyInfo requestParameterProperty,
        string name, bool isRequired, BindingSource bindingSource)
    {
        var propertyInfo = requestParameterProperty.PropertyInfo;
        var propertyType = propertyInfo.PropertyType;

        var apiParam = new ApiParameterDescription()
        {
            Name = name,
            IsRequired = isRequired,
            Source = bindingSource,
            Type = propertyType,
            DefaultValue = TypeHelper.CreateDefaultValue(propertyType),
        };
        var bindingInfo = new BindingInfo()
        {
            BindingSource = apiParam.Source,
        };
        var paramDescriptor = new FastEndpointParameterDescriptor()
        {
            Name = apiParam.Name,
            BindingInfo = bindingInfo,
            PropertyInfo = propertyInfo,
            ParameterType = propertyType
        };
        var metaData =
            new FastEndpointModelMetadata(ModelMetadataIdentity.ForProperty(propertyInfo, propertyType, requestType));

        apiParam.ParameterDescriptor = paramDescriptor;
        apiParam.BindingInfo = bindingInfo;
        apiParam.ModelMetadata = metaData;

        return apiParam;
    }

    private void CreateMetadataForRequestBody(ApiParameterDescription apiParam,
        IEnumerable<FastEndpointPropertyInfo> properties)
    {
        var requestType = apiParam.Type;

        var bindingInfo = new BindingInfo()
        {
            BindingSource = apiParam.Source,
        };
        var paramDescriptor = new FastEndpointParameterDescriptor()
        {
            Name = apiParam.Name,
            BindingInfo = bindingInfo,
            ParameterType = requestType
        };
        var propertyMetadatas = properties
            .Select(x =>
                new FastEndpointModelMetadata(ModelMetadataIdentity.ForProperty(x.PropertyInfo,
                    x.PropertyInfo.PropertyType, requestType)));
        var metaData = new FastEndpointModelMetadata(ModelMetadataIdentity.ForType(requestType), propertyMetadatas);

        apiParam.ParameterDescriptor = paramDescriptor;
        apiParam.BindingInfo = bindingInfo;
        apiParam.ModelMetadata = metaData;
    }
}