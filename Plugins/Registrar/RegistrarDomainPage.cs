namespace Hostpanel.Sdk.Plugins.Registrar;

/// <summary>One page of the domains an account holds.</summary>
/// <param name="Domains">The domains of this page; empty only on the last page.</param>
/// <param name="NextCursor">The cursor of the next page, or <see langword="null"/> on the last one. Opaque to the host, which sends it back unchanged.</param>
public sealed record RegistrarDomainPage(IReadOnlyList<RegistrarListedDomain> Domains, string? NextCursor);
