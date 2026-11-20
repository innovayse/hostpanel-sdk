namespace Hostpanel.Sdk.Plugins.Host;

/// <summary>
/// Optional capability of the host API for a payment plugin's callback: ask the host to read one gateway order now. It
/// extends <see cref="IPluginHostApi"/> and that interface does not change, so a plugin checks with a type test
/// (<c>host is IPluginHostPaymentApi</c>) and does without when the host lacks it. The interface is new, so the change
/// is source and binary compatible with every plugin and host built on an earlier version.
/// </summary>
/// <remarks>
/// A callback (a webhook or a return from the gateway's page) is a hint, never proof: the plugin does not mark anything
/// paid. It tells the host that an order probably changed, and the host then calls
/// <see cref="Payment.IPaymentPlugin.GetStatusAsync"/> and records what that answers. The host may ignore the
/// request, and the plugin must work without it: the host also reads pending orders on its own schedule.
/// </remarks>
public interface IPluginHostPaymentApi : IPluginHostApi
{
    /// <summary>
    /// Asks the host to read one gateway order of this plugin now, by calling the plugin's
    /// <see cref="Payment.IPaymentPlugin.GetStatusAsync"/> for it. The host may ignore the request, for example when the
    /// order is unknown, belongs to another plugin, is already settled, or the request comes too often; none of that
    /// is reported, so the call never tells the plugin whether a read happened or what it found. A repeat is safe.
    /// </summary>
    /// <param name="gatewayOrderId">The gateway-side order id, as <see cref="Payment.PaymentSession"/> reported it.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A task that completes when the host has taken the request, not when the order has been read.</returns>
    Task RequestOrderReadAsync(string gatewayOrderId, CancellationToken ct);
}
