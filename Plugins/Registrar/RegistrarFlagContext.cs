namespace Hostpanel.Sdk.Plugins.Registrar;

/// <summary>
/// Input for a switch on a domain: <see cref="IRegistrarOperationsPlugin.SetAutoRenewAsync"/>,
/// <see cref="IRegistrarOperationsPlugin.SetWhoisPrivacyAsync"/> and <see cref="IRegistrarOperationsPlugin.SetRegistrarLockAsync"/>.
/// </summary>
/// <param name="Connection">The registrar account the domain is held on.</param>
/// <param name="Domain">The domain.</param>
/// <param name="Enabled">The wanted state: <see langword="true"/> to switch the feature on, <see langword="false"/> to switch it off.</param>
public sealed record RegistrarFlagContext(RegistrarConnection Connection, RegistrarDomain Domain, bool Enabled);
