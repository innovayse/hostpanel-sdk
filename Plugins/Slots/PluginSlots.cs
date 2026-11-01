using Innovayse.SDK.Plugins.Enums;

namespace Innovayse.SDK.Plugins.Slots;

/// <summary>
/// The stable, versioned list of slots the host offers (the <c>slot</c> of a <c>PluginSlotEntry</c>) and the context
/// each one passes. A manifest may name only a slot listed here and only the context keys that slot offers. The list
/// follows semantic versioning through <see cref="Version"/>: adding a slot or a context key is a minor change,
/// removing or changing one is a major change.
/// </summary>
public static class PluginSlots
{
    /// <summary>The version of the slot list: the major part changes when a slot or a context key is removed or changed.</summary>
    public const string Version = "1.0";

    /// <summary>The slot id for cards on the client dashboard. It offers no context: the call's client is implied.</summary>
    public const string ClientDashboardCards = "client.dashboard.cards";

    /// <summary>The slot id for tabs on a client's service page. It offers <see cref="PluginSlotContextKeys.ServiceId"/>.</summary>
    public const string ClientServiceTabs = "client.service.tabs";

    /// <summary>The slot id for panels on an admin's client page. It offers <see cref="PluginSlotContextKeys.ClientId"/>.</summary>
    public const string AdminClientPanels = "admin.client.panels";

    /// <summary>
    /// The slot id for panels on an admin's service page. It offers <see cref="PluginSlotContextKeys.ServiceId"/> and
    /// <see cref="PluginSlotContextKeys.ClientId"/>, the service's own client.
    /// </summary>
    public const string AdminServicePanels = "admin.service.panels";

    /// <summary>Gets every slot of this version, client slots first.</summary>
    public static IReadOnlyList<PluginSlot> All { get; } =
    [
        new PluginSlot(ClientDashboardCards, PluginArea.Client, []),
        new PluginSlot(ClientServiceTabs, PluginArea.Client, [PluginSlotContextKeys.ServiceId]),
        new PluginSlot(AdminClientPanels, PluginArea.Admin, [PluginSlotContextKeys.ClientId]),
        new PluginSlot(AdminServicePanels, PluginArea.Admin, [PluginSlotContextKeys.ServiceId, PluginSlotContextKeys.ClientId]),
    ];

    /// <summary>Finds a slot by its id.</summary>
    /// <param name="id">The slot id; compared exactly, the ids are lower case.</param>
    /// <returns>The slot, or <see langword="null"/> when this version has no such slot.</returns>
    public static PluginSlot? Find(string? id)
    {
        return All.FirstOrDefault(slot => string.Equals(slot.Id, id, StringComparison.Ordinal));
    }
}
