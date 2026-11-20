namespace Hostpanel.Sdk.Plugins.Registrar;

/// <summary>Input for <see cref="IRegistrarDnssecPlugin.SetDnssecAsync"/>.</summary>
/// <param name="Connection">The registrar account the domain is held on.</param>
/// <param name="Domain">The domain.</param>
/// <param name="Records">The complete list of delegation signer records the domain should have. It replaces the current list, and an empty list turns DNSSEC off.</param>
public sealed record RegistrarDnssecContext(RegistrarConnection Connection, RegistrarDomain Domain, IReadOnlyList<RegistrarDsRecord> Records);
