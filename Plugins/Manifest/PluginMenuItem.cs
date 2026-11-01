namespace Innovayse.SDK.Plugins.Manifest;

/// <summary>One entry a plugin adds to a panel menu, an entry of <c>menus</c> in <c>plugin.json</c>.</summary>
public sealed class PluginMenuItem
{
    /// <summary>Gets the identifier of the entry, unique within the plugin.</summary>
    public required string Id { get; init; }

    /// <summary>Gets the text shown: a key of the manifest locales, or plain text.</summary>
    public required string Label { get; init; }

    /// <summary>Gets the <see cref="PluginPage.Id"/> the entry opens.</summary>
    public required string Page { get; init; }

    /// <summary>Gets the icon name, or <see langword="null"/> for the default icon of the panel.</summary>
    public string? Icon { get; init; }

    /// <summary>Gets the position among the entries of the menu, lowest first. Absent reads as <c>0</c>.</summary>
    public int Order { get; init; }
}
