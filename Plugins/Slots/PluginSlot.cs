using Innovayse.SDK.Plugins.Enums;

namespace Innovayse.SDK.Plugins.Slots;

/// <summary>One place in a host page where plugins may add content, and the context the host offers there.</summary>
/// <param name="Id">The stable slot id, for example <c>client.service.tabs</c>.</param>
/// <param name="Area">The panel the slot is in.</param>
/// <param name="ContextKeys">The context keys the slot offers, in the order they are documented; may be empty.</param>
public sealed record PluginSlot(string Id, PluginArea Area, IReadOnlyList<string> ContextKeys);
