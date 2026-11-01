namespace Innovayse.SDK.Plugins.Manifest;

/// <summary>One page a plugin adds to a panel, an entry of <c>pages</c> in <c>plugin.json</c>.</summary>
public sealed class PluginPage
{
    /// <summary>Gets the identifier of the page, unique within the plugin.</summary>
    public required string Id { get; init; }

    /// <summary>Gets the path of the page below the plugin's own prefix in the panel.</summary>
    public required string Path { get; init; }

    /// <summary>Gets the page title: a key of the manifest locales, or plain text.</summary>
    public required string Title { get; init; }

    /// <summary>Gets the path, inside the plugin package, of the document that describes the content of the page.</summary>
    public required string View { get; init; }

    /// <summary>
    /// Gets the permission the viewer needs on the client panel: a client permission name, or <c>Any</c>.
    /// <see langword="null"/> when none is declared, and always so on the admin panel.
    /// </summary>
    public string? Permission { get; init; }
}
