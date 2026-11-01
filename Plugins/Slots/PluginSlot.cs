using Innovayse.SDK.Plugins.Enums;

namespace Innovayse.SDK.Plugins.Slots;

/// <summary>
/// One place in a host page where plugins may add content, and the context the host offers there. A host validates the
/// <c>contextKeys</c> of a manifest widget with <see cref="Offers"/>.
/// </summary>
/// <param name="Id">The stable slot id, for example <c>client.service.tabs</c>.</param>
/// <param name="Area">The panel the slot is in.</param>
/// <param name="ContextKeys">The context keys the slot offers, in the order they are documented; may be empty.</param>
public sealed record PluginSlot(string Id, PluginArea Area, IReadOnlyList<string> ContextKeys)
{
    /// <summary>Tells whether the slot offers a context key.</summary>
    /// <param name="key">The context key; compared exactly, the keys are camelCase.</param>
    /// <returns><see langword="true"/> when <paramref name="key"/> is one of <see cref="ContextKeys"/>.</returns>
    public bool Offers(string key)
    {
        return ContextKeys.Contains(key, StringComparer.Ordinal);
    }
}
