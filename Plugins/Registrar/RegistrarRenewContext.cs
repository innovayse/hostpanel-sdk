namespace Hostpanel.Sdk.Plugins.Registrar;

/// <summary>
/// Input for <see cref="IRegistrarOperationsPlugin.RenewAsync"/>. A renewal charges the host's registrar account each time,
/// so a repeat after a lost answer must not renew again: <see cref="CurrentExpiresAt"/> is what lets the plugin tell.
/// </summary>
/// <param name="Connection">The registrar account to renew on.</param>
/// <param name="Domain">The domain to renew.</param>
/// <param name="Years">The number of years to add; at least 1.</param>
/// <param name="CurrentExpiresAt">
/// The expiry the host holds, stored before the first call and sent unchanged on every retry, or <see langword="null"/> when
/// the host has none. A plugin that can read the registrar's expiry answers
/// <see cref="Enums.PanelOperationOutcome.AlreadyApplied"/> when it is already at or past this date plus
/// <paramref name="Years"/>.
/// </param>
public sealed record RegistrarRenewContext(
    RegistrarConnection Connection,
    RegistrarDomain Domain,
    int Years,
    DateTimeOffset? CurrentExpiresAt);
