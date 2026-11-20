namespace Hostpanel.Sdk.Plugins.Registrar;

/// <summary>One domain in the list a registrar answers for an import.</summary>
/// <param name="Domain">The domain as the host addresses it later: the name and the registrar's reference for it.</param>
/// <param name="ExpiresAt">When the registration ends, or <see langword="null"/> when the registrar does not say.</param>
public sealed record RegistrarListedDomain(RegistrarDomain Domain, DateTimeOffset? ExpiresAt);
