namespace Hostpanel.Sdk.Plugins.Registrar;

/// <summary>Input for <see cref="IRegistrarSuggestionsPlugin.GetSuggestionsAsync"/>.</summary>
/// <param name="Connection">The registrar account to ask.</param>
/// <param name="Query">The word or name the user searched for, without a top level domain.</param>
/// <param name="Tlds">The top level domains to suggest, without a leading dot, in lower case, or an empty list for the registrar's own choice.</param>
/// <param name="Limit">The most suggestions the host wants; at least 1. A plugin answers no more.</param>
public sealed record RegistrarSuggestionsContext(RegistrarConnection Connection, string Query, IReadOnlyList<string> Tlds, int Limit);
