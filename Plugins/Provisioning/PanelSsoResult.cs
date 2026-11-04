using System.Text;
using Hostpanel.Sdk.Plugins.Diagnostics;
using Hostpanel.Sdk.Plugins.Enums;

namespace Hostpanel.Sdk.Plugins.Provisioning;

/// <summary>
/// Result of <see cref="IProvisioningPanelPlugin.CreateSsoLinkAsync"/>. The link signs whoever opens it in to the account,
/// so it is a credential: the generated <c>ToString</c> prints it masked, and neither the plugin nor the host logs it.
/// Build it with <see cref="Issued"/>, <see cref="NotFound"/>, <see cref="NotSupported"/> or <see cref="Refused"/>.
/// </summary>
/// <param name="Outcome">
/// <see cref="PanelOperationOutcome.Succeeded"/> when a link is issued, <see cref="PanelOperationOutcome.NotFound"/>,
/// <see cref="PanelOperationOutcome.NotSupported"/> or <see cref="PanelOperationOutcome.Refused"/>. A link is never
/// <c>AlreadyApplied</c>: every call issues a new one.
/// </param>
/// <param name="Url">The absolute, time limited sign-in address; set exactly when <paramref name="Outcome"/> is <c>Succeeded</c>. Secret.</param>
/// <param name="Reason">Why the panel refused; set exactly when <paramref name="Outcome"/> is <c>Refused</c>.</param>
public sealed record PanelSsoResult(PanelOperationOutcome Outcome, string? Url, PanelRefusalReason? Reason)
{
    /// <summary>Builds the answer for a link the panel issued.</summary>
    /// <param name="url">The absolute sign-in address.</param>
    /// <returns>A <see cref="PanelOperationOutcome.Succeeded"/> result.</returns>
    public static PanelSsoResult Issued(string url)
    {
        return new PanelSsoResult(PanelOperationOutcome.Succeeded, url, null);
    }

    /// <summary>Builds the answer for an account the panel does not hold.</summary>
    /// <returns>A <see cref="PanelOperationOutcome.NotFound"/> result.</returns>
    public static PanelSsoResult NotFound()
    {
        return new PanelSsoResult(PanelOperationOutcome.NotFound, null, null);
    }

    /// <summary>Builds the answer for a panel with no single sign on.</summary>
    /// <returns>A <see cref="PanelOperationOutcome.NotSupported"/> result.</returns>
    public static PanelSsoResult NotSupported()
    {
        return new PanelSsoResult(PanelOperationOutcome.NotSupported, null, null);
    }

    /// <summary>Builds the answer for a link the panel refused to issue.</summary>
    /// <param name="reason">Why, in the host's terms.</param>
    /// <returns>A <see cref="PanelOperationOutcome.Refused"/> result.</returns>
    public static PanelSsoResult Refused(PanelRefusalReason reason)
    {
        return new PanelSsoResult(PanelOperationOutcome.Refused, null, reason);
    }

    /// <summary>Prints the record without its secret members, so the generated <c>ToString</c> never reveals them.</summary>
    /// <param name="builder">The builder the generated <c>ToString</c> is assembling.</param>
    /// <returns>Always <see langword="true"/>: members are written.</returns>
    private bool PrintMembers(StringBuilder builder)
    {
        builder.Append("Outcome = ").Append(Outcome);
        builder.Append(", Url = ").Append(Url is null ? "null" : SecretMask.Text);
        builder.Append(", Reason = ").Append(Reason);
        return true;
    }
}
