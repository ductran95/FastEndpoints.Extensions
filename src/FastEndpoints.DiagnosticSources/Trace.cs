using System.Diagnostics;
using System.Reflection;

namespace FastEndpoints.DiagnosticSources;

public static class Trace
{
    internal static readonly AssemblyName AssemblyName = typeof(Trace).Assembly.GetName();
    public static readonly string ActivitySourceName = AssemblyName.Name;
    internal static readonly Version Version = AssemblyName.Version;
    internal static readonly ActivitySource ActivitySource = new(ActivitySourceName, Version.ToString());
}