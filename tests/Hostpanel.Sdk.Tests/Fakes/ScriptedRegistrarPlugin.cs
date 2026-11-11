using Hostpanel.Sdk.Plugins.Enums;
using Hostpanel.Sdk.Plugins.Provisioning;
using Hostpanel.Sdk.Plugins.Registrar;
using Hostpanel.Sdk.Plugins.Results;

namespace Hostpanel.Sdk.Tests.Fakes;

/// <summary>
/// A registrar plugin the test scripts, so the contract helper can be shown to accept a well behaved plugin and to reject
/// each way of breaking a promise. It implements the operations and all three optional interfaces. By default it behaves
/// like a registrar that holds one domain: the first order is done, every repeat finds it, the changes succeed and then are
/// already applied, and the reads answer fixed values. Setting <see cref="HoldsDomain"/> to <see langword="false"/> makes it
/// a registrar that holds nothing. A script receives the call number of its operation, which starts at 1, and may throw.
/// </summary>
internal sealed class ScriptedRegistrarPlugin : IRegistrarEmailForwardingPlugin, IRegistrarDnsManagementPlugin, IRegistrarWhoisPlugin
{
    /// <summary>The order every default answer carries.</summary>
    private static readonly RegistrarOrder DefaultOrder = new("dom-1", DateTimeOffset.UnixEpoch, false);

    /// <summary>How many times each operation was called, by method name.</summary>
    private readonly Dictionary<string, int> _calls = new(StringComparer.Ordinal);

    /// <summary>Gets or sets a value telling whether the registrar holds the domain of the call; <see langword="false"/> makes every read and change answer not found.</summary>
    public bool HoldsDomain { get; set; } = true;

    /// <summary>Gets or sets what <c>RegisterAsync</c> answers on each call; <see langword="null"/> keeps the default.</summary>
    public Func<int, ValueResult<RegistrarOrder, RegistrarRefusalReason>>? Register { get; set; }

    /// <summary>Gets or sets what <c>TransferAsync</c> answers on each call; <see langword="null"/> keeps the default.</summary>
    public Func<int, ValueResult<RegistrarOrder, RegistrarRefusalReason>>? Transfer { get; set; }

    /// <summary>Gets or sets what <c>RenewAsync</c> answers on each call; <see langword="null"/> keeps the default.</summary>
    public Func<int, ValueResult<RegistrarOrder, RegistrarRefusalReason>>? Renew { get; set; }

    /// <summary>Gets or sets what <c>CancelTransferAsync</c> answers on each call; <see langword="null"/> keeps the default.</summary>
    public Func<int, OperationResult<RegistrarRefusalReason>>? CancelTransfer { get; set; }

    /// <summary>Gets or sets what <c>InitiateOutgoingTransferAsync</c> answers on each call; <see langword="null"/> keeps the default.</summary>
    public Func<int, OperationResult<RegistrarRefusalReason>>? InitiateOutgoingTransfer { get; set; }

    /// <summary>Gets or sets what <c>SetNameserversAsync</c> answers on each call; <see langword="null"/> keeps the default.</summary>
    public Func<int, OperationResult<RegistrarRefusalReason>>? SetNameservers { get; set; }

    /// <summary>Gets or sets what <c>SetAutoRenewAsync</c> answers on each call; <see langword="null"/> keeps the default.</summary>
    public Func<int, OperationResult<RegistrarRefusalReason>>? SetAutoRenew { get; set; }

    /// <summary>Gets or sets what <c>SetWhoisPrivacyAsync</c> answers on each call; <see langword="null"/> keeps the default.</summary>
    public Func<int, OperationResult<RegistrarRefusalReason>>? SetWhoisPrivacy { get; set; }

    /// <summary>Gets or sets what <c>SetRegistrarLockAsync</c> answers on each call; <see langword="null"/> keeps the default.</summary>
    public Func<int, OperationResult<RegistrarRefusalReason>>? SetRegistrarLock { get; set; }

    /// <summary>Gets or sets what <c>SetContactAsync</c> answers on each call; <see langword="null"/> keeps the default.</summary>
    public Func<int, OperationResult<RegistrarRefusalReason>>? SetContact { get; set; }

    /// <summary>Gets or sets what <c>AddDnsRecordAsync</c> answers on each call; <see langword="null"/> keeps the default.</summary>
    public Func<int, OperationResult<RegistrarRefusalReason>>? AddDnsRecord { get; set; }

    /// <summary>Gets or sets what <c>UpdateDnsRecordAsync</c> answers on each call; <see langword="null"/> keeps the default.</summary>
    public Func<int, OperationResult<RegistrarRefusalReason>>? UpdateDnsRecord { get; set; }

    /// <summary>Gets or sets what <c>DeleteDnsRecordAsync</c> answers on each call; <see langword="null"/> keeps the default.</summary>
    public Func<int, OperationResult<RegistrarRefusalReason>>? DeleteDnsRecord { get; set; }

    /// <summary>Gets or sets what <c>SetEmailForwardingAsync</c> answers on each call; <see langword="null"/> keeps the default.</summary>
    public Func<int, OperationResult<RegistrarRefusalReason>>? SetEmailForwarding { get; set; }

    /// <summary>Gets or sets what <c>AddEmailForwardingRuleAsync</c> answers on each call; <see langword="null"/> keeps the default.</summary>
    public Func<int, OperationResult<RegistrarRefusalReason>>? AddEmailForwardingRule { get; set; }

    /// <summary>Gets or sets what <c>UpdateEmailForwardingRuleAsync</c> answers on each call; <see langword="null"/> keeps the default.</summary>
    public Func<int, OperationResult<RegistrarRefusalReason>>? UpdateEmailForwardingRule { get; set; }

    /// <summary>Gets or sets what <c>DeleteEmailForwardingRuleAsync</c> answers on each call; <see langword="null"/> keeps the default.</summary>
    public Func<int, OperationResult<RegistrarRefusalReason>>? DeleteEmailForwardingRule { get; set; }

    /// <summary>Gets or sets what <c>SetDnsManagementAsync</c> answers on each call; <see langword="null"/> keeps the default.</summary>
    public Func<int, OperationResult<RegistrarRefusalReason>>? SetDnsManagement { get; set; }

    /// <summary>Gets or sets what <c>GetEppCodeAsync</c> answers on each call; <see langword="null"/> keeps the default.</summary>
    public Func<int, ValueResult<RegistrarEppCode, RegistrarRefusalReason>>? GetEppCode { get; set; }

    /// <summary>Gets or sets what <c>GetContactAsync</c> answers on each call; <see langword="null"/> keeps the default.</summary>
    public Func<int, ValueResult<RegistrarContact, RegistrarRefusalReason>>? GetContact { get; set; }

    /// <summary>Gets or sets what <c>CheckAvailabilityAsync</c> answers on each call; <see langword="null"/> keeps the default.</summary>
    public Func<int, ValueResult<RegistrarAvailability, RegistrarRefusalReason>>? CheckAvailability { get; set; }

    /// <summary>Gets or sets what <c>GetTldPricingAsync</c> answers on each call; <see langword="null"/> keeps the default.</summary>
    public Func<int, ValueResult<IReadOnlyList<RegistrarTldPricing>, RegistrarRefusalReason>>? GetTldPricing { get; set; }

    /// <summary>Gets or sets what <c>CheckDomainActiveAsync</c> answers on each call; <see langword="null"/> keeps the default.</summary>
    public Func<int, ValueResult<RegistrarOrder, RegistrarRefusalReason>>? CheckDomainActive { get; set; }

    /// <summary>Gets or sets what <c>GetDnsRecordsAsync</c> answers on each call; <see langword="null"/> keeps the default.</summary>
    public Func<int, ValueResult<IReadOnlyList<RegistrarDnsRecord>, RegistrarRefusalReason>>? GetDnsRecords { get; set; }

    /// <summary>Gets or sets what <c>GetWhoisAsync</c> answers on each call; <see langword="null"/> keeps the default.</summary>
    public Func<int, ValueResult<RegistrarWhois, RegistrarRefusalReason>>? GetWhois { get; set; }

    /// <summary>Gets or sets what <c>TestConnectionAsync</c> answers on each call; <see langword="null"/> keeps the default.</summary>
    public Func<int, PanelConnectionTestResult>? Test { get; set; }

    /// <summary>Gets how many times an operation was called.</summary>
    /// <param name="operation">The method name, for example <c>RegisterAsync</c>.</param>
    /// <returns>The number of calls, zero when it was never called.</returns>
    public int CallsOf(string operation)
    {
        return _calls.GetValueOrDefault(operation);
    }

    /// <inheritdoc />
    public Task<ValueResult<RegistrarAvailability, RegistrarRefusalReason>> CheckAvailabilityAsync(RegistrarAvailabilityContext context, CancellationToken ct)
    {
        var call = Count(nameof(CheckAvailabilityAsync));
        return Task.FromResult(CheckAvailability is null ? ValueResult<RegistrarAvailability, RegistrarRefusalReason>.Done(new RegistrarAvailability(true)) : CheckAvailability(call));
    }

    /// <inheritdoc />
    public Task<ValueResult<RegistrarOrder, RegistrarRefusalReason>> RegisterAsync(RegistrarRegisterContext context, CancellationToken ct)
    {
        return Task.FromResult(Order(Register, nameof(RegisterAsync), true));
    }

    /// <inheritdoc />
    public Task<ValueResult<RegistrarOrder, RegistrarRefusalReason>> TransferAsync(RegistrarTransferContext context, CancellationToken ct)
    {
        return Task.FromResult(Order(Transfer, nameof(TransferAsync), true));
    }

    /// <inheritdoc />
    public Task<ValueResult<RegistrarOrder, RegistrarRefusalReason>> RenewAsync(RegistrarRenewContext context, CancellationToken ct)
    {
        return Task.FromResult(Order(Renew, nameof(RenewAsync), false));
    }

    /// <inheritdoc />
    public Task<ValueResult<RegistrarOrder, RegistrarRefusalReason>> CheckDomainActiveAsync(RegistrarDomainContext context, CancellationToken ct)
    {
        var call = Count(nameof(CheckDomainActiveAsync));
        return Task.FromResult(CheckDomainActive is null ? DataOrNotFound(DefaultOrder) : CheckDomainActive(call));
    }

    /// <inheritdoc />
    public Task<OperationResult<RegistrarRefusalReason>> CancelTransferAsync(RegistrarDomainContext context, CancellationToken ct)
    {
        return Task.FromResult(Change(CancelTransfer, nameof(CancelTransferAsync)));
    }

    /// <inheritdoc />
    public Task<OperationResult<RegistrarRefusalReason>> InitiateOutgoingTransferAsync(RegistrarDomainContext context, CancellationToken ct)
    {
        return Task.FromResult(Change(InitiateOutgoingTransfer, nameof(InitiateOutgoingTransferAsync)));
    }

    /// <inheritdoc />
    public Task<ValueResult<RegistrarEppCode, RegistrarRefusalReason>> GetEppCodeAsync(RegistrarDomainContext context, CancellationToken ct)
    {
        var call = Count(nameof(GetEppCodeAsync));
        return Task.FromResult(GetEppCode is null ? DataOrNotFound(new RegistrarEppCode(RegistrarFixtures.EppCode)) : GetEppCode(call));
    }

    /// <inheritdoc />
    public Task<OperationResult<RegistrarRefusalReason>> SetNameserversAsync(RegistrarNameserversContext context, CancellationToken ct)
    {
        return Task.FromResult(Change(SetNameservers, nameof(SetNameserversAsync)));
    }

    /// <inheritdoc />
    public Task<OperationResult<RegistrarRefusalReason>> SetAutoRenewAsync(RegistrarFlagContext context, CancellationToken ct)
    {
        return Task.FromResult(Change(SetAutoRenew, nameof(SetAutoRenewAsync)));
    }

    /// <inheritdoc />
    public Task<OperationResult<RegistrarRefusalReason>> SetWhoisPrivacyAsync(RegistrarFlagContext context, CancellationToken ct)
    {
        return Task.FromResult(Change(SetWhoisPrivacy, nameof(SetWhoisPrivacyAsync)));
    }

    /// <inheritdoc />
    public Task<OperationResult<RegistrarRefusalReason>> SetRegistrarLockAsync(RegistrarFlagContext context, CancellationToken ct)
    {
        return Task.FromResult(Change(SetRegistrarLock, nameof(SetRegistrarLockAsync)));
    }

    /// <inheritdoc />
    public Task<ValueResult<RegistrarContact, RegistrarRefusalReason>> GetContactAsync(RegistrarDomainContext context, CancellationToken ct)
    {
        var call = Count(nameof(GetContactAsync));
        return Task.FromResult(GetContact is null ? DataOrNotFound(RegistrarFixtures.Contact) : GetContact(call));
    }

    /// <inheritdoc />
    public Task<OperationResult<RegistrarRefusalReason>> SetContactAsync(RegistrarContactContext context, CancellationToken ct)
    {
        return Task.FromResult(Change(SetContact, nameof(SetContactAsync)));
    }

    /// <inheritdoc />
    public Task<ValueResult<IReadOnlyList<RegistrarDnsRecord>, RegistrarRefusalReason>> GetDnsRecordsAsync(RegistrarDomainContext context, CancellationToken ct)
    {
        var call = Count(nameof(GetDnsRecordsAsync));
        return Task.FromResult(GetDnsRecords is null ? DataOrNotFound<IReadOnlyList<RegistrarDnsRecord>>([]) : GetDnsRecords(call));
    }

    /// <inheritdoc />
    public Task<OperationResult<RegistrarRefusalReason>> AddDnsRecordAsync(RegistrarDnsRecordContext context, CancellationToken ct)
    {
        return Task.FromResult(Change(AddDnsRecord, nameof(AddDnsRecordAsync)));
    }

    /// <inheritdoc />
    public Task<OperationResult<RegistrarRefusalReason>> UpdateDnsRecordAsync(RegistrarDnsRecordUpdateContext context, CancellationToken ct)
    {
        return Task.FromResult(Change(UpdateDnsRecord, nameof(UpdateDnsRecordAsync)));
    }

    /// <inheritdoc />
    public Task<OperationResult<RegistrarRefusalReason>> DeleteDnsRecordAsync(RegistrarDnsRecordRemovalContext context, CancellationToken ct)
    {
        return Task.FromResult(Change(DeleteDnsRecord, nameof(DeleteDnsRecordAsync)));
    }

    /// <inheritdoc />
    public Task<ValueResult<IReadOnlyList<RegistrarTldPricing>, RegistrarRefusalReason>> GetTldPricingAsync(RegistrarPricingContext context, CancellationToken ct)
    {
        var call = Count(nameof(GetTldPricingAsync));
        return Task.FromResult(GetTldPricing is null ? ValueResult<IReadOnlyList<RegistrarTldPricing>, RegistrarRefusalReason>.Done([]) : GetTldPricing(call));
    }

    /// <inheritdoc />
    public Task<OperationResult<RegistrarRefusalReason>> SetEmailForwardingAsync(RegistrarFlagContext context, CancellationToken ct)
    {
        return Task.FromResult(Change(SetEmailForwarding, nameof(SetEmailForwardingAsync)));
    }

    /// <inheritdoc />
    public Task<OperationResult<RegistrarRefusalReason>> AddEmailForwardingRuleAsync(RegistrarEmailForwardingRuleContext context, CancellationToken ct)
    {
        return Task.FromResult(Change(AddEmailForwardingRule, nameof(AddEmailForwardingRuleAsync)));
    }

    /// <inheritdoc />
    public Task<OperationResult<RegistrarRefusalReason>> UpdateEmailForwardingRuleAsync(RegistrarEmailForwardingRuleContext context, CancellationToken ct)
    {
        return Task.FromResult(Change(UpdateEmailForwardingRule, nameof(UpdateEmailForwardingRuleAsync)));
    }

    /// <inheritdoc />
    public Task<OperationResult<RegistrarRefusalReason>> DeleteEmailForwardingRuleAsync(RegistrarEmailForwardingRemovalContext context, CancellationToken ct)
    {
        return Task.FromResult(Change(DeleteEmailForwardingRule, nameof(DeleteEmailForwardingRuleAsync)));
    }

    /// <inheritdoc />
    public Task<OperationResult<RegistrarRefusalReason>> SetDnsManagementAsync(RegistrarFlagContext context, CancellationToken ct)
    {
        return Task.FromResult(Change(SetDnsManagement, nameof(SetDnsManagementAsync)));
    }

    /// <inheritdoc />
    public Task<ValueResult<RegistrarWhois, RegistrarRefusalReason>> GetWhoisAsync(RegistrarWhoisContext context, CancellationToken ct)
    {
        var call = Count(nameof(GetWhoisAsync));
        return Task.FromResult(GetWhois is null ? DataOrNotFound(new RegistrarWhois("Example Registrar", "Registrant Name", DateTimeOffset.UnixEpoch, DateTimeOffset.UnixEpoch)) : GetWhois(call));
    }

    /// <inheritdoc />
    public Task<PanelConnectionTestResult> TestConnectionAsync(RegistrarTestConnectionContext context, CancellationToken ct)
    {
        var call = Count(nameof(TestConnectionAsync));
        return Task.FromResult(Test is null ? PanelConnectionTestResult.Connected(null, 3) : Test(call));
    }

    /// <summary>Answers an operation that orders something: the script when there is one, otherwise done on the first call and already done after.</summary>
    /// <param name="script">The test's script, or <see langword="null"/>.</param>
    /// <param name="operation">The method name.</param>
    /// <param name="createsDomain"><see langword="true"/> for an order that needs no domain yet (register, transfer).</param>
    /// <returns>The answer.</returns>
    private ValueResult<RegistrarOrder, RegistrarRefusalReason> Order(Func<int, ValueResult<RegistrarOrder, RegistrarRefusalReason>>? script, string operation, bool createsDomain)
    {
        var call = Count(operation);
        if (script is not null)
        {
            return script(call);
        }

        if (!createsDomain && !HoldsDomain)
        {
            return ValueResult<RegistrarOrder, RegistrarRefusalReason>.NotFound();
        }

        return call == 1
            ? ValueResult<RegistrarOrder, RegistrarRefusalReason>.Done(DefaultOrder)
            : ValueResult<RegistrarOrder, RegistrarRefusalReason>.AlreadyDone(DefaultOrder);
    }

    /// <summary>Answers a change of the domain: the script when there is one, otherwise done on the first call and already done after, or not found when the registrar holds no domain.</summary>
    /// <param name="script">The test's script, or <see langword="null"/>.</param>
    /// <param name="operation">The method name.</param>
    /// <returns>The answer.</returns>
    private OperationResult<RegistrarRefusalReason> Change(Func<int, OperationResult<RegistrarRefusalReason>>? script, string operation)
    {
        var call = Count(operation);
        if (script is not null)
        {
            return script(call);
        }

        if (!HoldsDomain)
        {
            return OperationResult<RegistrarRefusalReason>.NotFound();
        }

        return call == 1 ? OperationResult<RegistrarRefusalReason>.Done() : OperationResult<RegistrarRefusalReason>.AlreadyDone();
    }

    /// <summary>Gets a successful answer when the registrar holds the domain, otherwise not found.</summary>
    /// <typeparam name="T">The value read.</typeparam>
    /// <param name="value">The value the registrar holds.</param>
    /// <returns>The answer.</returns>
    private ValueResult<T, RegistrarRefusalReason> DataOrNotFound<T>(T value)
        where T : class
    {
        return HoldsDomain ? ValueResult<T, RegistrarRefusalReason>.Done(value) : ValueResult<T, RegistrarRefusalReason>.NotFound();
    }

    /// <summary>Counts one call of an operation.</summary>
    /// <param name="operation">The method name.</param>
    /// <returns>The call number of this call, starting at 1.</returns>
    private int Count(string operation)
    {
        var next = _calls.GetValueOrDefault(operation) + 1;
        _calls[operation] = next;
        return next;
    }
}
