using Hostpanel.Sdk.Plugins.Enums;

namespace Hostpanel.Sdk.Plugins.Provisioning;

/// <summary>
/// Result of <see cref="IProvisioningPanelPlugin.TestConnectionAsync"/>. It carries no panel error text: why a test failed
/// is for the plugin's log, and the host shows a fixed sentence for the <see cref="PanelConnectionOutcome"/>.
/// </summary>
/// <param name="Outcome">How the test ended.</param>
/// <param name="Version">The panel's version string, for display, or <see langword="null"/> when the panel did not say or the test failed.</param>
/// <param name="AccountCount">How many accounts the panel holds, or <see langword="null"/> when the panel did not say or the test failed. Zero or more.</param>
public sealed record PanelConnectionTestResult(PanelConnectionOutcome Outcome, string? Version, int? AccountCount)
{
    /// <summary>Builds the answer for a panel that accepted the credentials.</summary>
    /// <param name="version">The panel's version, or <see langword="null"/>.</param>
    /// <param name="accountCount">The number of accounts, or <see langword="null"/>.</param>
    /// <returns>A <see cref="PanelConnectionOutcome.Connected"/> result.</returns>
    public static PanelConnectionTestResult Connected(string? version, int? accountCount)
    {
        return new PanelConnectionTestResult(PanelConnectionOutcome.Connected, version, accountCount);
    }

    /// <summary>Builds the answer for a test that did not connect.</summary>
    /// <param name="outcome">Any outcome except <see cref="PanelConnectionOutcome.Connected"/>.</param>
    /// <returns>The result, with no version and no count.</returns>
    /// <exception cref="ArgumentException"><paramref name="outcome"/> is <see cref="PanelConnectionOutcome.Connected"/>.</exception>
    public static PanelConnectionTestResult Failed(PanelConnectionOutcome outcome)
    {
        if (outcome == PanelConnectionOutcome.Connected)
        {
            throw new ArgumentException("A failed test cannot have the Connected outcome.", nameof(outcome));
        }

        return new PanelConnectionTestResult(outcome, null, null);
    }
}
