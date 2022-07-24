using FastEndpoints.ApiExplorer.Helpers;
using FastEndpoints.Reflection.Helper;
using Microsoft.AspNetCore.Builder;

namespace FastEndpoints.ApiExplorer;

public static class FastEndpointOptionsAction
{
    public static void AddApiExplorerGroupName(EndpointDefinition epDef, RouteHandlerBuilder builder)
    {
        var routingOpts = FastEndpointsConfigHelper.GetVersioningOpts();
        
        var version = epDef.Version.Current > 0 ? epDef.Version.Current : routingOpts?.DefaultVersion;
        var versionPrefix = routingOpts?.Prefix ?? "v";
        if (version != null && version > 0)
        {
            builder.WithGroupName($"{versionPrefix}{version}");
        }
        
        if (epDef.Tags != null)
        {
            builder.WithMetadata(epDef.Tags);
        }
    }
    
    public static void StopDefaultApiExplorerMetadata(EndpointDefinition epDef, RouteHandlerBuilder builder)
    {
        var excludeFromDescriptionMetadata = new ExcludeFromDescriptionMetadata(true);
        builder.WithMetadata(excludeFromDescriptionMetadata);
    }
    
    public static bool RemoveDeprecatedEndpoints(EndpointDefinition ep)
    {
        if (ep.Tags != null && (ep.Tags.Contains("Deprecated") || ep.Tags.Contains("Excluded")))
            return false; // don't register this endpoint

        return true;
    }
}