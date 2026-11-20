using Hostpanel.Sdk.Plugins.Enums;
using Hostpanel.Sdk.Plugins.Manifest;
using Hostpanel.Sdk.Plugins.Results;

namespace Hostpanel.Sdk.Plugins.Registrar;

/// <summary>
/// Domain name suggestions, as a separate opt in: a registrar plugin implements it next to
/// <see cref="IRegistrarOperationsPlugin"/> when its registrar proposes names for a search term, and lists
/// <see cref="PluginCapabilities.RegistrarSuggestions"/> in its manifest. A host treats a plugin that does not implement it
/// as <see cref="PanelOperationOutcome.NotSupported"/>. It is a separate interface because a new operation always arrives as
/// a new interface, never as a member of an existing one.
/// </summary>
public interface IRegistrarSuggestionsPlugin : IRegistrarOperationsPlugin
{
    /// <summary>Asks the registrar for names that match a search term. Read only; safe to repeat.</summary>
    /// <param name="context">The registrar account, the search term, the top level domains and the limit.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns><c>Succeeded</c> with at most <see cref="RegistrarSuggestionsContext.Limit"/> suggestions (an empty list when there are none), <c>NotSupported</c> or <c>Refused</c>.</returns>
    Task<ValueResult<IReadOnlyList<RegistrarSuggestion>, RegistrarRefusalReason>> GetSuggestionsAsync(RegistrarSuggestionsContext context, CancellationToken ct);
}
