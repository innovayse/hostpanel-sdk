namespace Innovayse.SDK.Plugins.Manifest;

/// <summary>The <c>ui</c> block of <c>plugin.json</c>: what the plugin adds to the client and admin panels.</summary>
public sealed class PluginUi
{
    /// <summary>Backing field of <see cref="Client"/>; an empty area unless the manifest declares one.</summary>
    private readonly PluginUiArea _client = new();

    /// <summary>Backing field of <see cref="Admin"/>; an empty area unless the manifest declares one.</summary>
    private readonly PluginUiArea _admin = new();

    /// <summary>Backing field of <see cref="Slots"/>; empty unless the manifest lists some.</summary>
    private readonly IReadOnlyList<PluginSlotEntry> _slots = [];

    /// <summary>Gets what the plugin adds to the client panel. A missing or <c>null</c> <c>client</c> reads as an empty area.</summary>
    public PluginUiArea Client
    {
        get => _client;
        init => _client = value ?? new PluginUiArea();
    }

    /// <summary>Gets what the plugin adds to the admin panel. A missing or <c>null</c> <c>admin</c> reads as an empty area.</summary>
    public PluginUiArea Admin
    {
        get => _admin;
        init => _admin = value ?? new PluginUiArea();
    }

    /// <summary>Gets the content put into the host slots. A missing or <c>null</c> <c>slots</c> reads as an empty list.</summary>
    public IReadOnlyList<PluginSlotEntry> Slots
    {
        get => _slots;
        init => _slots = value ?? [];
    }
}
