using System.Diagnostics;
using System.Reflection;

namespace FastEndpoints.DiagnosticSources;

internal static class Tracing
{
    internal static readonly AssemblyName AssemblyName = typeof(Tracing).Assembly.GetName();
    internal static readonly string ActivitySourceName = AssemblyName.Name;
    internal static readonly Version Version = AssemblyName.Version;
    internal static readonly ActivitySource ActivitySource = new(ActivitySourceName, Version.ToString());
}