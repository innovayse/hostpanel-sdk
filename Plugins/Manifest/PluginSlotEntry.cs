namespace Innovayse.SDK.Plugins.Manifest;

/// <summary>One piece of content a plugin puts into a slot the host offers, an entry of <c>slots</c> in <c>plugin.json</c>.</summary>
public sealed class PluginSlotEntry
{
    /// <summary>Backing field of <see cref="ProductIds"/>; empty unless the manifest lists some.</summary>
    private readonly IReadOnlyList<int> _productIds = [];

    /// <summary>Backing field of <see cref="ContextKeys"/>; empty unless the manifest lists some.</summary>
    private readonly IReadOnlyList<string> _contextKeys = [];

    /// <summary>Gets the identifier of this widget, unique within the plugin; it breaks a tie when two widgets share an <see cref="Order"/>.</summary>
    public required string Id { get; init; }

    /// <summary>Gets the identifier of the slot the host offers, for example <c>client.dashboard.cards</c>.</summary>
    public required string Slot { get; init; }

    /// <summary>Gets the path, inside the plugin package, of the document that describes the content.</summary>
    public required string View { get; init; }

    /// <summary>Gets the title shown with the content: a key of the manifest locales, or plain text; <see langword="null"/> for none.</summary>
    public string? Title { get; init; }

    /// <summary>
    /// Gets the position among the widgets of the slot, lowest first; equal values fall back to the plugin id and then
    /// <see cref="Id"/>. Absent reads as <c>0</c>.
    /// </summary>
    public int Order { get; init; }

    /// <summary>
    /// Gets the permission the viewer needs on a client slot: a client permission name, or <see cref="PluginPermissions.Any"/>.
    /// <see langword="null"/> when none is declared, and always so on an admin slot.
    /// </summary>
    public string? Permission { get; init; }

    /// <summary>
    /// Gets the ids of the products the widget is bound to. The host shows it only where the slot's service belongs
    /// to one of them. An empty list (also when <c>productIds</c> is missing or <c>null</c>) binds it to no product,
    /// so it shows for every one.
    /// </summary>
    public IReadOnlyList<int> ProductIds
    {
        get => _productIds;
        init => _productIds = value ?? [];
    }

    /// <summary>
    /// Gets the context keys the widget needs from the slot, for example <c>serviceId</c> or <c>clientId</c>. The host
    /// checks that the slot offers each of them, verifies them for the caller, and passes only these to the plugin. A
    /// missing or <c>null</c> <c>contextKeys</c> reads as an empty list.
    /// </summary>
    public IReadOnlyList<string> ContextKeys
    {
        get => _contextKeys;
        init => _contextKeys = value ?? [];
    }
}
