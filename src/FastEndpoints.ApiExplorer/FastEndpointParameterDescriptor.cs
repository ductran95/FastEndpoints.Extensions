using System.Reflection;
using Microsoft.AspNetCore.Mvc.Abstractions;

namespace FastEndpoints.ApiExplorer;

internal sealed class FastEndpointParameterDescriptor : ParameterDescriptor
{
    public PropertyInfo PropertyInfo { get; set; } = default!;
}