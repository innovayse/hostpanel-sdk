using Innovayse.SDK.Plugins.Fulfilment;
using Innovayse.SDK.Plugins.Hosting;
using Innovayse.SDK.Plugins.Identity;

namespace Innovayse.SDK.Plugins.Enums;

/// <summary>
/// Categorises the service a plugin provides.
/// </summary>
public enum PluginType
{
    /// <summary>Hosting and server provisioning through a hosting panel.</summary>
    Provisioning,

    /// <summary>Payment gateway.</summary>
    Payment,

    /// <summary>Domain registrar.</summary>
    Registrar,

    /// <summary>
    /// Decides which client a signed-in principal acts for and what it may do there
    /// (<see cref="IClientResolver"/>). At most one identity plugin is consulted.
    /// </summary>
    Identity,

    /// <summary>
    /// Fulfils digital products after money moved, and is asked before a refund moves it back
    /// (<see cref="IFulfilmentPlugin"/>). Every loaded fulfilment plugin sees every digital invoice.
    /// </summary>
    Fulfilment,

    /// <summary>Runs a background worker for the life of the host (<see cref="IHostedPlugin"/>).</summary>
    Hosted,

    /// <summary>
    /// Answers requests the host forwards to it and may add pages and slot content (<see cref="Endpoints.IPluginEndpoints"/>). The
    /// value is explicit and appended, so the members above keep theirs.
    /// </summary>
    App = 6,
}
