namespace Hostpanel.Sdk.Plugins.Tax;

/// <summary>The place a tax is due to: a country and, where the tax is regional, the region.</summary>
/// <param name="Country">The ISO 3166-1 alpha-2 country code, in upper case.</param>
/// <param name="Region">
/// The state, province or other first level region, or <see langword="null"/> when the tax is national.
/// </param>
public sealed record TaxJurisdiction(string Country, string? Region);
