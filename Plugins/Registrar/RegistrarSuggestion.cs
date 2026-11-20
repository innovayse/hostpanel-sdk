namespace Hostpanel.Sdk.Plugins.Registrar;

/// <summary>One domain name a registrar suggests for a search term.</summary>
/// <param name="DomainName">The fully qualified domain name, in lower case. The registrar offers it as available.</param>
/// <param name="PremiumPrice">The premium price of the name, or <see langword="null"/> when it costs the standard price of its top level domain.</param>
public sealed record RegistrarSuggestion(string DomainName, RegistrarPremiumPrice? PremiumPrice);
