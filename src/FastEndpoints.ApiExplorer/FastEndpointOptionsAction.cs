using FastEndpoints.Reflection.Extensions;
using Microsoft.AspNetCore.Builder;

namespace FastEndpoints.ApiExplorer;

public static class FastEndpointOptionsAction
{
    public static void AddApiExplorerGroupName(this EndpointDefinition epDef)
    {
        var config = new Config();
        var routingOpts = config.Versioning;

        var version = epDef.Version.Current > 0 ? epDef.Version.Current : routingOpts.GetDefaultVersion();
        var versionPrefix = routingOpts.GetPrefix() ?? "v";
        if (version > 0)
        {
            epDef.Options(x => x.WithGroupName($"{versionPrefix}{version}"));
        }

        if (epDef.EndpointTags != null)
        {
            epDef.Options(x => x.WithMetadata(epDef.EndpointTags));
        }
    }

    public static void StopDefaultApiExplorerMetadata(this EndpointDefinition epDef)
    {
        var excludeFromDescriptionMetadata = new ExcludeFromDescriptionMetadata(true);
        epDef.Options(x => x.WithMetadata(excludeFromDescriptionMetadata));
    }

    public static bool RemoveDeprecatedEndpoints(this EndpointDefinition ep)
    {
        if (ep.EndpointTags != null && (ep.EndpointTags.Contains("Deprecated") || ep.EndpointTags.Contains("Excluded")))
            return false; // don't register this endpoint

        return true;
    }
}