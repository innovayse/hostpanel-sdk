using Hostpanel.Sdk.Plugins.Enums;
using Hostpanel.Sdk.Plugins.Manifest;
using Hostpanel.Sdk.Plugins.Results;

namespace Hostpanel.Sdk.Plugins.Registrar;

/// <summary>
/// The DNSSEC delegation signer records of a domain, as a separate opt in: a registrar plugin implements it next to
/// <see cref="IRegistrarOperationsPlugin"/> when its registrar publishes them to the registry, and lists
/// <see cref="PluginCapabilities.RegistrarDnssec"/> in its manifest. A host treats a plugin that does not implement it as
/// <see cref="PanelOperationOutcome.NotSupported"/>. It is a separate interface because a new operation always arrives as a
/// new interface, never as a member of an existing one.
/// </summary>
public interface IRegistrarDnssecPlugin : IRegistrarOperationsPlugin
{
    /// <summary>Reads the delegation signer records a domain has now. Read only; safe to repeat.</summary>
    /// <param name="context">The registrar account and the domain.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns><c>Succeeded</c> with the records (an empty list when DNSSEC is off), <c>NotFound</c> for a domain the registrar does not hold, <c>NotSupported</c> or <c>Refused</c>.</returns>
    Task<ValueResult<IReadOnlyList<RegistrarDsRecord>, RegistrarRefusalReason>> GetDnssecAsync(RegistrarDomainContext context, CancellationToken ct);

    /// <summary>Replaces the delegation signer records of a domain. Idempotent: the same list is a success, and an empty list turns DNSSEC off.</summary>
    /// <param name="context">The registrar account, the domain and the complete list of records.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns><c>Succeeded</c>, <c>AlreadyApplied</c>, <c>NotFound</c> for a domain the registrar does not hold, <c>NotSupported</c> or <c>Refused</c>.</returns>
    Task<OperationResult<RegistrarRefusalReason>> SetDnssecAsync(RegistrarDnssecContext context, CancellationToken ct);
}
