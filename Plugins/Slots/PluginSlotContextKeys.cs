namespace Innovayse.SDK.Plugins.Slots;

/// <summary>The context key names a slot may offer, as listed on <see cref="PluginSlots"/>.</summary>
public static class PluginSlotContextKeys
{
    /// <summary>The id of the client's service the page shows, as in <see cref="Host.PluginClientService.Id"/>.</summary>
    public const string ServiceId = "serviceId";

    /// <summary>The id of the client the admin page shows.</summary>
    public const string ClientId = "clientId";
}
