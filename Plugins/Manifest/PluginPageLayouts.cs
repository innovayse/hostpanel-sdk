namespace Hostpanel.Sdk.Plugins.Manifest;

/// <summary>The layouts a client page may declare in its <c>layout</c> member (since 3.2.0). Compared exactly.</summary>
public static class PluginPageLayouts
{
    /// <summary>The page sits in the panel's usual frame; also what an absent <c>layout</c> means.</summary>
    public const string Standard = "standard";

    /// <summary>
    /// The page takes the full width with no service side menu and a slim link back, for a working tool. Client panel
    /// only.
    /// </summary>
    public const string Workspace = "workspace";

    /// <summary>Gets every layout.</summary>
    public static IReadOnlyList<string> All { get; } = [Standard, Workspace];
}
