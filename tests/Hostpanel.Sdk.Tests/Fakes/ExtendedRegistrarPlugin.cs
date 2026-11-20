using Hostpanel.Sdk.Plugins.Enums;
using Hostpanel.Sdk.Plugins.Registrar;
using Hostpanel.Sdk.Plugins.Results;

namespace Hostpanel.Sdk.Tests.Fakes;

/// <summary>
/// A <see cref="ScriptedRegistrarPlugin"/> that also implements the newer optional interfaces: child nameservers, DNSSEC,
/// restore, the domain list, name suggestions and the balance. It answers like its base: done on the first call and already
/// done after for a change, fixed values for a read, and not found when <see cref="ScriptedRegistrarPlugin.HoldsDomain"/> is
/// <see langword="false"/>. A script receives the call number of its operation, which starts at 1, and may throw.
/// </summary>
internal sealed class ExtendedRegistrarPlugin : ScriptedRegistrarPlugin, IRegistrarChildNameserversPlugin, IRegistrarDnssecPlugin, IRegistrarRestorePlugin, IRegistrarDomainListPlugin, IRegistrarSuggestionsPlugin, IRegistrarBalancePlugin
{
    /// <summary>Gets or sets what <c>CreateChildNameserverAsync</c> answers on each call; <see langword="null"/> keeps the default.</summary>
    public Func<int, OperationResult<RegistrarRefusalReason>>? CreateChildNameserver { get; set; }

    /// <summary>Gets or sets what <c>UpdateChildNameserverAsync</c> answers on each call; <see langword="null"/> keeps the default.</summary>
    public Func<int, OperationResult<RegistrarRefusalReason>>? UpdateChildNameserver { get; set; }

    /// <summary>Gets or sets what <c>DeleteChildNameserverAsync</c> answers on each call; <see langword="null"/> keeps the default.</summary>
    public Func<int, OperationResult<RegistrarRefusalReason>>? DeleteChildNameserver { get; set; }

    /// <summary>Gets or sets what <c>GetDnssecAsync</c> answers on each call; <see langword="null"/> keeps the default.</summary>
    public Func<int, ValueResult<IReadOnlyList<RegistrarDsRecord>, RegistrarRefusalReason>>? GetDnssec { get; set; }

    /// <summary>Gets or sets what <c>SetDnssecAsync</c> answers on each call; <see langword="null"/> keeps the default.</summary>
    public Func<int, OperationResult<RegistrarRefusalReason>>? SetDnssec { get; set; }

    /// <summary>Gets or sets what <c>RestoreDomainAsync</c> answers on each call; <see langword="null"/> keeps the default.</summary>
    public Func<int, ValueResult<RegistrarOrder, RegistrarRefusalReason>>? RestoreDomain { get; set; }

    /// <summary>Gets or sets what <c>ListDomainsAsync</c> answers on each call; <see langword="null"/> keeps the default.</summary>
    public Func<int, ValueResult<RegistrarDomainPage, RegistrarRefusalReason>>? ListDomains { get; set; }

    /// <summary>Gets or sets what <c>GetSuggestionsAsync</c> answers on each call; <see langword="null"/> keeps the default.</summary>
    public Func<int, ValueResult<IReadOnlyList<RegistrarSuggestion>, RegistrarRefusalReason>>? GetSuggestions { get; set; }

    /// <summary>Gets or sets what <c>GetBalanceAsync</c> answers on each call; <see langword="null"/> keeps the default.</summary>
    public Func<int, ValueResult<RegistrarBalance, RegistrarRefusalReason>>? GetBalance { get; set; }

    /// <inheritdoc />
    public Task<OperationResult<RegistrarRefusalReason>> CreateChildNameserverAsync(RegistrarChildNameserverContext context, CancellationToken ct)
    {
        return Task.FromResult(Change(CreateChildNameserver, nameof(CreateChildNameserverAsync)));
    }

    /// <inheritdoc />
    public Task<OperationResult<RegistrarRefusalReason>> UpdateChildNameserverAsync(RegistrarChildNameserverContext context, CancellationToken ct)
    {
        return Task.FromResult(Change(UpdateChildNameserver, nameof(UpdateChildNameserverAsync)));
    }

    /// <inheritdoc />
    public Task<OperationResult<RegistrarRefusalReason>> DeleteChildNameserverAsync(RegistrarChildNameserverRemovalContext context, CancellationToken ct)
    {
        return Task.FromResult(Change(DeleteChildNameserver, nameof(DeleteChildNameserverAsync)));
    }

    /// <inheritdoc />
    public Task<ValueResult<IReadOnlyList<RegistrarDsRecord>, RegistrarRefusalReason>> GetDnssecAsync(RegistrarDomainContext context, CancellationToken ct)
    {
        var call = Calls.Count(nameof(GetDnssecAsync));
        return Task.FromResult(GetDnssec is null ? DataOrNotFound<IReadOnlyList<RegistrarDsRecord>>([RegistrarFixtures.DsRecord]) : GetDnssec(call));
    }

    /// <inheritdoc />
    public Task<OperationResult<RegistrarRefusalReason>> SetDnssecAsync(RegistrarDnssecContext context, CancellationToken ct)
    {
        return Task.FromResult(Change(SetDnssec, nameof(SetDnssecAsync)));
    }

    /// <inheritdoc />
    public Task<ValueResult<RegistrarOrder, RegistrarRefusalReason>> RestoreDomainAsync(RegistrarDomainContext context, CancellationToken ct)
    {
        return Task.FromResult(Order(RestoreDomain, nameof(RestoreDomainAsync), false));
    }

    /// <inheritdoc />
    public Task<ValueResult<RegistrarDomainPage, RegistrarRefusalReason>> ListDomainsAsync(RegistrarDomainListContext context, CancellationToken ct)
    {
        var call = Calls.Count(nameof(ListDomainsAsync));
        var page = new RegistrarDomainPage([new RegistrarListedDomain(RegistrarFixtures.Domain, DateTimeOffset.UnixEpoch)], null);
        return Task.FromResult(ListDomains is null ? ValueResult<RegistrarDomainPage, RegistrarRefusalReason>.Done(page) : ListDomains(call));
    }

    /// <inheritdoc />
    public Task<ValueResult<IReadOnlyList<RegistrarSuggestion>, RegistrarRefusalReason>> GetSuggestionsAsync(RegistrarSuggestionsContext context, CancellationToken ct)
    {
        var call = Calls.Count(nameof(GetSuggestionsAsync));
        IReadOnlyList<RegistrarSuggestion> suggestions = [new RegistrarSuggestion("example.test", null)];
        return Task.FromResult(GetSuggestions is null ? ValueResult<IReadOnlyList<RegistrarSuggestion>, RegistrarRefusalReason>.Done(suggestions) : GetSuggestions(call));
    }

    /// <inheritdoc />
    public Task<ValueResult<RegistrarBalance, RegistrarRefusalReason>> GetBalanceAsync(RegistrarBalanceContext context, CancellationToken ct)
    {
        var call = Calls.Count(nameof(GetBalanceAsync));
        return Task.FromResult(GetBalance is null ? ValueResult<RegistrarBalance, RegistrarRefusalReason>.Done(new RegistrarBalance("USD", 100m)) : GetBalance(call));
    }
}
