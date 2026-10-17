using Innovayse.SDK.Plugins.Fulfilment;
using Innovayse.SDK.Plugins.Hosting;
using Innovayse.SDK.Plugins.Identity;

namespace Innovayse.SDK.Plugins.Enums;
/// <summary>
/// Categorises the service a plugin provides.
/// </summary>
public enum PluginType
{
    /// <summary>Hosting / server provisioning (cPanel, Plesk, CWP, etc.).</summary>
    Provisioning,

    /// <summary>Payment gateway (Stripe, PayPal, etc.).</summary>
    Payment,

    /// <summary>Domain registrar (Namecheap, ENOM, etc.).</summary>
    Registrar,

    /// <summary>
    /// Decides which client a signed-in principal acts for and what it may do there
    /// (<see cref="IClientResolver"/>). At most one identity plugin is consulted.
    /// </summary>
    Identity,

    /// <summary>
    /// Fulfils digital products after money moved — and is asked before a refund moves it back
    /// (<see cref="IFulfilmentPlugin"/>). Every loaded fulfilment plugin sees every digital invoice.
    /// </summary>
    Fulfilment,

    /// <summary>Runs a background worker for the life of the host (<see cref="IHostedPlugin"/>).</summary>
    Hosted,
}
