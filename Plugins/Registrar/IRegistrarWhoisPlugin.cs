using Hostpanel.Sdk.Plugins.Enums;
using Hostpanel.Sdk.Plugins.Manifest;
using Hostpanel.Sdk.Plugins.Results;

namespace Hostpanel.Sdk.Plugins.Registrar;

/// <summary>
/// The WHOIS lookup of a registrar, as a separate opt in: a registrar plugin implements it next to
/// <see cref="IRegistrarOperationsPlugin"/> when its registrar answers WHOIS queries, and lists
/// <see cref="PluginCapabilities.RegistrarWhois"/> in its manifest. A host treats a plugin that does not implement it as
/// <see cref="PanelOperationOutcome.NotSupported"/>. It is a separate interface because a new operation always arrives as a
/// new interface, never as a member of an existing one.
/// </summary>
public interface IRegistrarWhoisPlugin : IRegistrarOperationsPlugin
{
    /// <summary>Looks up the WHOIS record of a domain name. Read only; safe to repeat. The name need not be a domain the registrar holds.</summary>
    /// <param name="context">The registrar account and the name.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns><c>Succeeded</c> with the record, <c>NotFound</c> when the name is not registered, <c>NotSupported</c> or <c>Refused</c>.</returns>
    Task<ValueResult<RegistrarWhois, RegistrarRefusalReason>> GetWhoisAsync(RegistrarWhoisContext context, CancellationToken ct);
}
