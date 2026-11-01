namespace Innovayse.SDK.Plugins.Manifest;

/// <summary>One piece of content a plugin puts into a slot the host offers, an entry of <c>slots</c> in <c>plugin.json</c>.</summary>
public sealed class PluginSlotEntry
{
    /// <summary>Gets the identifier of the slot the host offers, for example <c>client.dashboard.cards</c>.</summary>
    public required string Slot { get; init; }

    /// <summary>Gets the path, inside the plugin package, of the document that describes the content.</summary>
    public required string View { get; init; }

    /// <summary>Gets the title shown with the content: a key of the manifest locales, or plain text; <see langword="null"/> for none.</summary>
    public string? Title { get; init; }
}
