using Hostpanel.Sdk.Plugins.Enums;
using Hostpanel.Sdk.Plugins.Manifest;
using Hostpanel.Sdk.Plugins.Results;

namespace Hostpanel.Sdk.Plugins.Registrar;

/// <summary>
/// E-mail forwarding for the domains of a registrar, as a separate opt in: a registrar plugin implements it next to
/// <see cref="IRegistrarOperationsPlugin"/> when its registrar forwards mail, and lists
/// <see cref="PluginCapabilities.RegistrarEmailForwarding"/> in its manifest. A host treats a plugin that does not implement
/// it as <see cref="PanelOperationOutcome.NotSupported"/> for every operation here and stops offering the feature. It is a
/// separate interface, and not part of <see cref="IRegistrarOperationsPlugin"/>, because a new operation always arrives as a
/// new interface: adding a member to an existing interface would break every plugin that already implements it. Everything
/// in <see cref="IRegistrarOperationsPlugin"/> about outcomes, idempotency, secrets and cancellation applies here too.
/// </summary>
public interface IRegistrarEmailForwardingPlugin : IRegistrarOperationsPlugin
{
    /// <summary>Switches e-mail forwarding of a domain on or off. Idempotent: a domain already in the wanted state is a success.</summary>
    /// <param name="context">The registrar account, the domain and the wanted state.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns><c>Succeeded</c>, <c>AlreadyApplied</c>, <c>NotFound</c> for a domain the registrar does not hold, <c>NotSupported</c> or <c>Refused</c>.</returns>
    Task<OperationResult<RegistrarRefusalReason>> SetEmailForwardingAsync(RegistrarFlagContext context, CancellationToken ct);

    /// <summary>Adds a forwarding rule. Idempotent: a rule with the same source and destination that is already there is <see cref="PanelOperationOutcome.AlreadyApplied"/>.</summary>
    /// <param name="context">The registrar account, the domain and the rule.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns><c>Succeeded</c>, <c>AlreadyApplied</c>, <c>NotFound</c>, <c>NotSupported</c> or <c>Refused</c>.</returns>
    Task<OperationResult<RegistrarRefusalReason>> AddEmailForwardingRuleAsync(RegistrarEmailForwardingRuleContext context, CancellationToken ct);

    /// <summary>Changes the destination of the rule that has the given source. Idempotent: a rule that already has this destination is a success.</summary>
    /// <param name="context">The registrar account, the domain and the rule with its new destination.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns><c>Succeeded</c>, <c>AlreadyApplied</c>, <c>NotFound</c> for a domain or a rule the registrar does not hold, <c>NotSupported</c> or <c>Refused</c>.</returns>
    Task<OperationResult<RegistrarRefusalReason>> UpdateEmailForwardingRuleAsync(RegistrarEmailForwardingRuleContext context, CancellationToken ct);

    /// <summary>Removes the rule that has the given source. Idempotent: a rule that is already gone is <see cref="PanelOperationOutcome.AlreadyApplied"/>, never <c>NotFound</c>; an unknown domain is <c>NotFound</c>.</summary>
    /// <param name="context">The registrar account, the domain and the source of the rule.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns><c>Succeeded</c>, <c>AlreadyApplied</c>, <c>NotFound</c> for a domain the registrar does not hold, <c>NotSupported</c> or <c>Refused</c>.</returns>
    Task<OperationResult<RegistrarRefusalReason>> DeleteEmailForwardingRuleAsync(RegistrarEmailForwardingRemovalContext context, CancellationToken ct);
}
