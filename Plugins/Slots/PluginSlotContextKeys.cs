namespace Innovayse.SDK.Plugins.Slots;

/// <summary>
/// The context key names a slot may offer, as listed on <see cref="PluginSlots"/>. A host checks the keys a manifest
/// widget declares with <see cref="PluginSlot.Offers"/>. The values are host ids: in the client area they name only
/// the caller's own records, in the admin area the record the admin page shows.
/// </summary>
public static class PluginSlotContextKeys
{
    /// <summary>
    /// The host id of the service the page shows, the same id as <see cref="Host.PluginClientService.Id"/>. In the
    /// client area it is one of the caller's own services; in the admin area it is any service the admin opened.
    /// </summary>
    public const string ServiceId = "serviceId";

    /// <summary>The host id of the client the admin page shows. Offered in the admin area only.</summary>
    public const string ClientId = "clientId";
}
