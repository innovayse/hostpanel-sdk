using Hostpanel.Sdk.Plugins.Enums;

namespace Hostpanel.Sdk.Plugins.Provisioning;

/// <summary>
/// Result of <see cref="IProvisioningPanelPlugin.ListAccountsAsync"/>: one page of the panel's accounts. Build it with
/// <see cref="Page"/>, <see cref="NotSupported"/> or <see cref="Refused"/>.
/// </summary>
/// <param name="Outcome">
/// <see cref="PanelOperationOutcome.Succeeded"/> for a page (an empty one included), <see cref="PanelOperationOutcome.NotSupported"/>
/// or <see cref="PanelOperationOutcome.Refused"/>. Never <c>NotFound</c> or <c>AlreadyApplied</c>.
/// </param>
/// <param name="Accounts">The accounts on this page; empty unless <paramref name="Outcome"/> is <c>Succeeded</c>.</param>
/// <param name="NextCursor">An opaque value that asks for the next page, or <see langword="null"/> on the last page. A panel that returns everything at once always answers <see langword="null"/>.</param>
/// <param name="Reason">Why the panel refused; set exactly when <paramref name="Outcome"/> is <c>Refused</c>.</param>
public sealed record PanelAccountListResult(
    PanelOperationOutcome Outcome,
    IReadOnlyList<PanelAccount> Accounts,
    string? NextCursor,
    PanelRefusalReason? Reason)
{
    /// <summary>Builds the answer for a page of accounts.</summary>
    /// <param name="accounts">The accounts on the page.</param>
    /// <param name="nextCursor">The cursor of the next page, or <see langword="null"/> on the last page.</param>
    /// <returns>A <see cref="PanelOperationOutcome.Succeeded"/> result.</returns>
    public static PanelAccountListResult Page(IReadOnlyList<PanelAccount> accounts, string? nextCursor)
    {
        return new PanelAccountListResult(PanelOperationOutcome.Succeeded, accounts, nextCursor, null);
    }

    /// <summary>Builds the answer for a panel that cannot list its accounts.</summary>
    /// <returns>A <see cref="PanelOperationOutcome.NotSupported"/> result with no accounts.</returns>
    public static PanelAccountListResult NotSupported()
    {
        return new PanelAccountListResult(PanelOperationOutcome.NotSupported, [], null, null);
    }

    /// <summary>Builds the answer for a listing the panel refused.</summary>
    /// <param name="reason">Why, in the host's terms.</param>
    /// <returns>A <see cref="PanelOperationOutcome.Refused"/> result with no accounts.</returns>
    public static PanelAccountListResult Refused(PanelRefusalReason reason)
    {
        return new PanelAccountListResult(PanelOperationOutcome.Refused, [], null, reason);
    }
}
