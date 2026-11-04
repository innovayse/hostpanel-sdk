namespace Hostpanel.Sdk.Packaging.Naming;

/// <summary>
/// What a host decides about the assembly names a plugin package may not carry: how its own assemblies are named, which
/// namespace under that belongs to plugins, and any extra exact names it keeps out. The SDK holds no host's name, so the
/// host supplies these, like <see cref="Manifest.PluginManifestOptions"/>.
/// </summary>
/// <param name="HostPrefix">
/// The prefix of every host assembly, with the trailing dot (for example <c>Acme.Host.</c>). It is the boundary of the
/// reservation: a name that merely starts with the same letters but not the dot is not covered by it. It must not be empty.
/// </param>
/// <param name="PluginNamespacePrefix">
/// The one namespace under <paramref name="HostPrefix"/> that belongs to plugins, with the trailing dot (for example
/// <c>Acme.Host.Plugins.</c>). An official plugin is the assembly <c>&lt;prefix&gt;&lt;Name&gt;</c>; no host assembly may be named
/// under it, so a plugin can never shadow one. The trailing dot is the boundary: the same text with extra letters after
/// the last segment is not a plugin name and stays reserved.
/// </param>
public sealed record PluginAssemblyOptions(string HostPrefix, string PluginNamespacePrefix)
{
    /// <summary>
    /// Gets the exact assembly names the host keeps out besides its prefix, the SDK and the framework: for example a
    /// former name of the SDK that a plugin may still ship. Compared ignoring case. Empty by default.
    /// </summary>
    public IReadOnlySet<string> ExtraReservedNames { get; init; } = new HashSet<string>();
}
