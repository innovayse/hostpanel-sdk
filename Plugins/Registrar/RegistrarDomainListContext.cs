namespace Hostpanel.Sdk.Plugins.Registrar;

/// <summary>Input for <see cref="IRegistrarDomainListPlugin.ListDomainsAsync"/>.</summary>
/// <param name="Connection">The registrar account whose domains are wanted.</param>
/// <param name="Cursor">The cursor of the page wanted, as the previous page returned it, or <see langword="null"/> for the first page.</param>
public sealed record RegistrarDomainListContext(RegistrarConnection Connection, string? Cursor);
