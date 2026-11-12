using Hostpanel.Sdk.Plugins.Diagnostics;
using Hostpanel.Sdk.Plugins.Enums;

namespace Hostpanel.Sdk.Plugins.Provisioning;

/// <summary>
/// Result of <see cref="IProvisioningPanelPlugin.CreateAccountAsync"/>. Build it with <see cref="Created"/>,
/// <see cref="AlreadyExists"/> or <see cref="Refused"/>. It carries no panel text and no secret.
/// </summary>
/// <param name="Outcome">
/// <see cref="PanelOperationOutcome.Succeeded"/> (created now), <see cref="PanelOperationOutcome.AlreadyApplied"/> (the account
/// already existed under the requested user name) or <see cref="PanelOperationOutcome.Refused"/>. A create has no other outcome.
/// </param>
/// <param name="ProvisioningRef">The panel's reference for the account; set for <c>Succeeded</c> and <c>AlreadyApplied</c>, <see langword="null"/> when refused. Stable: a retry answers the same value.</param>
/// <param name="ExistingDomain">The primary domain of the account that already exists, for <c>AlreadyApplied</c> when the panel can tell; otherwise <see langword="null"/>. The host uses it to tell its own earlier attempt from an account that belongs to someone else.</param>
/// <param name="ControlPanelUrl">The address of the account's own control panel login page when the panel can tell, otherwise <see langword="null"/>. A plain address, not a sign-in link: it carries no session or token.</param>
/// <param name="Reason">Why the panel refused; set exactly when <paramref name="Outcome"/> is <c>Refused</c>.</param>
public sealed record PanelCreateResult(
    PanelOperationOutcome Outcome,
    string? ProvisioningRef,
    string? ExistingDomain,
    string? ControlPanelUrl,
    PanelRefusalReason? Reason)
    : IOperationAnswer<PanelRefusalReason>
{
    /// <summary>Builds the answer for an account the panel created now.</summary>
    /// <param name="provisioningRef">The panel's reference for the new account.</param>
    /// <param name="controlPanelUrl">The address of the account's control panel login page, or <see langword="null"/>.</param>
    /// <returns>A <see cref="PanelOperationOutcome.Succeeded"/> result.</returns>
    public static PanelCreateResult Created(string provisioningRef, string? controlPanelUrl = null)
    {
        return new PanelCreateResult(PanelOperationOutcome.Succeeded, provisioningRef, null, controlPanelUrl, null);
    }

    /// <summary>Builds the answer for an account the panel already holds under the requested user name.</summary>
    /// <param name="provisioningRef">The panel's reference for the existing account; the same value the first create answered.</param>
    /// <param name="existingDomain">The existing account's primary domain, or <see langword="null"/> when the panel cannot tell.</param>
    /// <param name="controlPanelUrl">The address of the account's control panel login page, or <see langword="null"/>.</param>
    /// <returns>An <see cref="PanelOperationOutcome.AlreadyApplied"/> result.</returns>
    public static PanelCreateResult AlreadyExists(string provisioningRef, string? existingDomain, string? controlPanelUrl = null)
    {
        return new PanelCreateResult(PanelOperationOutcome.AlreadyApplied, provisioningRef, existingDomain, controlPanelUrl, null);
    }

    /// <summary>Builds the answer for a create the panel refused.</summary>
    /// <param name="reason">Why, in the host's terms.</param>
    /// <returns>A <see cref="PanelOperationOutcome.Refused"/> result.</returns>
    public static PanelCreateResult Refused(PanelRefusalReason reason)
    {
        return new PanelCreateResult(PanelOperationOutcome.Refused, null, null, null, reason);
    }
}
