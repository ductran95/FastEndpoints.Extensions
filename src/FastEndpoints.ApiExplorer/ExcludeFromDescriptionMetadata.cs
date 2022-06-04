using Microsoft.AspNetCore.Routing;

namespace FastEndpoints.ApiExplorer;

internal class ExcludeFromDescriptionMetadata: IExcludeFromDescriptionMetadata
{
    public bool ExcludeFromDescription { get; set; }

    
    public ExcludeFromDescriptionMetadata(bool excludeFromDescription)
    {
        ExcludeFromDescription = excludeFromDescription;
    }
}