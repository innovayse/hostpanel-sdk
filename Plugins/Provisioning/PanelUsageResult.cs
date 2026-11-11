using Hostpanel.Sdk.Plugins.Diagnostics;
using Hostpanel.Sdk.Plugins.Enums;

namespace Hostpanel.Sdk.Plugins.Provisioning;

/// <summary>
/// Result of <see cref="IProvisioningPanelPlugin.GetUsageAsync"/>. Build it with <see cref="Read"/>, <see cref="NotFound"/>,
/// <see cref="NotSupported"/> or <see cref="Refused"/>. Reading is safe to repeat, so there is no <c>AlreadyApplied</c>.
/// </summary>
/// <param name="Outcome">How the panel answered.</param>
/// <param name="Usage">The figures; set exactly when <paramref name="Outcome"/> is <see cref="PanelOperationOutcome.Succeeded"/>.</param>
/// <param name="Reason">Why the panel refused; set exactly when <paramref name="Outcome"/> is <see cref="PanelOperationOutcome.Refused"/>.</param>
public sealed record PanelUsageResult(PanelOperationOutcome Outcome, PanelUsage? Usage, PanelRefusalReason? Reason)
    : IOperationAnswer<PanelRefusalReason>
{
    /// <summary>Builds the answer for figures the plugin read.</summary>
    /// <param name="usage">The figures.</param>
    /// <returns>A <see cref="PanelOperationOutcome.Succeeded"/> result.</returns>
    public static PanelUsageResult Read(PanelUsage usage)
    {
        return new PanelUsageResult(PanelOperationOutcome.Succeeded, usage, null);
    }

    /// <summary>Builds the answer for an account the panel does not hold.</summary>
    /// <returns>A <see cref="PanelOperationOutcome.NotFound"/> result.</returns>
    public static PanelUsageResult NotFound()
    {
        return new PanelUsageResult(PanelOperationOutcome.NotFound, null, null);
    }

    /// <summary>Builds the answer for a panel that reports no usage.</summary>
    /// <returns>A <see cref="PanelOperationOutcome.NotSupported"/> result.</returns>
    public static PanelUsageResult NotSupported()
    {
        return new PanelUsageResult(PanelOperationOutcome.NotSupported, null, null);
    }

    /// <summary>Builds the answer for a read the panel refused.</summary>
    /// <param name="reason">Why, in the host's terms.</param>
    /// <returns>A <see cref="PanelOperationOutcome.Refused"/> result.</returns>
    public static PanelUsageResult Refused(PanelRefusalReason reason)
    {
        return new PanelUsageResult(PanelOperationOutcome.Refused, null, reason);
    }
}
