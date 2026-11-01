namespace Innovayse.SDK.Plugins.Manifest;

/// <summary>What a plugin adds to one panel: the <c>client</c> or the <c>admin</c> member of <c>ui</c>.</summary>
public sealed class PluginUiArea
{
    /// <summary>Backing field of <see cref="Menus"/>; empty unless the manifest lists some.</summary>
    private readonly IReadOnlyList<PluginMenuItem> _menus = [];

    /// <summary>Backing field of <see cref="Pages"/>; empty unless the manifest lists some.</summary>
    private readonly IReadOnlyList<PluginPage> _pages = [];

    /// <summary>Gets the menu entries. A missing or <c>null</c> <c>menus</c> reads as an empty list.</summary>
    public IReadOnlyList<PluginMenuItem> Menus
    {
        get => _menus;
        init => _menus = value ?? [];
    }

    /// <summary>Gets the pages. A missing or <c>null</c> <c>pages</c> reads as an empty list.</summary>
    public IReadOnlyList<PluginPage> Pages
    {
        get => _pages;
        init => _pages = value ?? [];
    }
}
