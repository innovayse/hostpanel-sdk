using System.Collections.Frozen;

namespace Hostpanel.Sdk.Packaging.Naming;

/// <summary>
/// The assembly names a plugin package may not carry. A plugin folder is loaded with <c>Assembly.LoadFrom</c> over
/// every assembly in it, so one named like an assembly the host already uses (the plugin SDK, a host assembly, or a
/// framework or shipped dependency) would give the process a second copy of those types and break every cast between
/// host and plugin.
/// </summary>
public static class PluginAssemblyNames
{
    /// <summary>The extension of a library.</summary>
    private const string LibraryExtension = ".dll";

    /// <summary>The extension of an executable.</summary>
    private const string ExecutableExtension = ".exe";

    /// <summary>Name prefixes the framework owns, reserved whatever host this is.</summary>
    private static readonly string[] FrameworkPrefixes = ["System.", "Microsoft."];

    /// <summary>
    /// Exact names reserved for the plugin SDK (and this package) and the framework's core assemblies, whatever host
    /// this is. The set compares ignoring case.
    /// </summary>
    private static readonly FrozenSet<string> ReservedNames = new[]
    {
        "Hostpanel.Sdk", "Hostpanel.Sdk.Packaging", "System", "mscorlib", "netstandard",
    }.ToFrozenSet(StringComparer.OrdinalIgnoreCase);

    /// <summary>Whether an archive entry is an assembly the host already provides.</summary>
    /// <param name="entryFileName">The entry's file name, without its folder.</param>
    /// <param name="platformAssemblyNames">The simple names of the assemblies the host resolves without help, compared case-insensitively.</param>
    /// <param name="options">How the host names its own assemblies.</param>
    /// <returns><see langword="true"/> for a <c>.dll</c> or <c>.exe</c> named like a host, SDK or framework assembly.</returns>
    public static bool IsReserved(string entryFileName, IReadOnlySet<string> platformAssemblyNames, PluginAssemblyOptions options)
    {
        ArgumentNullException.ThrowIfNull(entryFileName);
        ArgumentNullException.ThrowIfNull(platformAssemblyNames);
        ArgumentNullException.ThrowIfNull(options);

        var extension = Path.GetExtension(entryFileName);
        if (!extension.Equals(LibraryExtension, StringComparison.OrdinalIgnoreCase)
            && !extension.Equals(ExecutableExtension, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var name = Path.GetFileNameWithoutExtension(entryFileName);
        if (IsPluginNamespaceName(name, options))
        {
            // Only the reserved prefixes are lifted for this namespace; a name the runtime already provides is still refused.
            return Contains(platformAssemblyNames, name);
        }

        return ReservedNames.Contains(name)
            || options.ExtraReservedNames.Any(reserved => string.Equals(reserved, name, StringComparison.OrdinalIgnoreCase))
            || name.StartsWith(options.HostPrefix, StringComparison.OrdinalIgnoreCase)
            || FrameworkPrefixes.Any(prefix => name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            || Contains(platformAssemblyNames, name);
    }

    /// <summary>Whether a simple assembly name sits under the plugin namespace, with at least one segment after it.</summary>
    /// <param name="name">The assembly's simple name, without extension.</param>
    /// <param name="options">How the host names its own assemblies.</param>
    /// <returns><see langword="true"/> for the plugin namespace followed by a name.</returns>
    public static bool IsPluginNamespaceName(string name, PluginAssemblyOptions options)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(options);

        return name.Length > options.PluginNamespacePrefix.Length
            && name.StartsWith(options.PluginNamespacePrefix, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Whether a set holds a name, whatever comparer the set was built with.</summary>
    /// <param name="names">The simple assembly names.</param>
    /// <param name="name">The name to look for.</param>
    /// <returns><see langword="true"/> when some entry equals the name ignoring case.</returns>
    private static bool Contains(IReadOnlySet<string> names, string name)
    {
        // A caller's set may be case-sensitive; the file system the plugin was packed on may not be.
        return names.Contains(name) || names.Any(candidate => string.Equals(candidate, name, StringComparison.OrdinalIgnoreCase));
    }
}
