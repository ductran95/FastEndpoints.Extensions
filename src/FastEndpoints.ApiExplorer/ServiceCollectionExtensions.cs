using FastEndpoints.ApiExplorer.ApiDescriptionProvider;
using FastEndpoints.ApiExplorer.ModelBinding;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace FastEndpoints.ApiExplorer;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddFastEndpointsApiExplorer(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.TryAddSingleton<IRequestTypeCache, RequestTypeCache>();

        services.TryAddEnumerable(
            ServiceDescriptor.Transient<IApiDescriptionProvider, FastEndpointMetadataApiDescriptionProvider>());

        return services;
    }
}