namespace Hostpanel.Sdk.Packaging.Manifest;

/// <summary>What a host decides about the manifest rules: the few values the rules depend on that the SDK cannot know.</summary>
public sealed class PluginManifestOptions
{
    /// <summary>The manifest versions every host reads today: <c>1</c> (fields only) and <c>2</c> (settings, locales, api, ui).</summary>
    public static readonly IReadOnlySet<int> DefaultManifestVersions = new HashSet<int> { 1, 2 };

    /// <summary>The manifest versions the host reads. Defaults to <see cref="DefaultManifestVersions"/>.</summary>
    public IReadOnlySet<int> SupportedManifestVersions { get; init; } = DefaultManifestVersions;

    /// <summary>
    /// The permission names a client route, page or slot entry may name besides the any-permission marker, compared
    /// exactly (case-sensitive). The set is the host's: it is the host that defines what a client may be permitted to do.
    /// </summary>
    public required IReadOnlySet<string> ClientPermissionNames { get; init; }
}
