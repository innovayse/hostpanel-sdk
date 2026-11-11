using Hostpanel.Sdk.Plugins.Enums;
using Hostpanel.Sdk.Plugins.Provisioning;
using Hostpanel.Sdk.Plugins.Registrar;

namespace Hostpanel.Sdk.Tests.Fakes;

/// <summary>
/// A registrar plugin the test scripts, so the contract helper can be shown to accept a well behaved plugin and to reject
/// each way of breaking a promise. By default it behaves like a registrar that holds one domain: the first order is done,
/// every repeat finds it, the changes succeed and then are already applied, and the reads answer fixed values. Setting
/// <see cref="HoldsDomain"/> to <see langword="false"/> makes it a registrar that holds nothing. A script receives the call
/// number of its operation, which starts at 1, and may throw.
/// </summary>
internal sealed class ScriptedRegistrarPlugin : IRegistrarOperationsPlugin
{
    /// <summary>The order every default answer carries.</summary>
    private static readonly RegistrarOrder DefaultOrder = new("dom-1", DateTimeOffset.UnixEpoch, false);

    /// <summary>How many times each operation was called, by method name.</summary>
    private readonly Dictionary<string, int> _calls = new(StringComparer.Ordinal);

    /// <summary>Gets or sets a value telling whether the registrar holds the domain of the call; <see langword="false"/> makes every read and change answer not found.</summary>
    public bool HoldsDomain { get; set; } = true;

    /// <summary>Gets or sets what <c>RegisterAsync</c> answers on each call; <see langword="null"/> keeps the default.</summary>
    public Func<int, RegistrarDataResult<RegistrarOrder>>? Register { get; set; }

    /// <summary>Gets or sets what <c>TransferAsync</c> answers on each call; <see langword="null"/> keeps the default.</summary>
    public Func<int, RegistrarDataResult<RegistrarOrder>>? Transfer { get; set; }

    /// <summary>Gets or sets what <c>RenewAsync</c> answers on each call; <see langword="null"/> keeps the default.</summary>
    public Func<int, RegistrarDataResult<RegistrarOrder>>? Renew { get; set; }

    /// <summary>Gets or sets what <c>SetNameserversAsync</c> answers on each call; <see langword="null"/> keeps the default.</summary>
    public Func<int, RegistrarActionResult>? SetNameservers { get; set; }

    /// <summary>Gets or sets what <c>SetAutoRenewAsync</c> answers on each call; <see langword="null"/> keeps the default.</summary>
    public Func<int, RegistrarActionResult>? SetAutoRenew { get; set; }

    /// <summary>Gets or sets what <c>SetWhoisPrivacyAsync</c> answers on each call; <see langword="null"/> keeps the default.</summary>
    public Func<int, RegistrarActionResult>? SetWhoisPrivacy { get; set; }

    /// <summary>Gets or sets what <c>SetRegistrarLockAsync</c> answers on each call; <see langword="null"/> keeps the default.</summary>
    public Func<int, RegistrarActionResult>? SetRegistrarLock { get; set; }

    /// <summary>Gets or sets what <c>SetContactAsync</c> answers on each call; <see langword="null"/> keeps the default.</summary>
    public Func<int, RegistrarActionResult>? SetContact { get; set; }

    /// <summary>Gets or sets what <c>DeleteDnsRecordAsync</c> answers on each call; <see langword="null"/> keeps the default.</summary>
    public Func<int, RegistrarActionResult>? DeleteDnsRecord { get; set; }

    /// <summary>Gets or sets what <c>GetEppCodeAsync</c> answers on each call; <see langword="null"/> keeps the default.</summary>
    public Func<int, RegistrarDataResult<RegistrarEppCode>>? GetEppCode { get; set; }

    /// <summary>Gets or sets what <c>GetDnsRecordsAsync</c> answers on each call; <see langword="null"/> keeps the default.</summary>
    public Func<int, RegistrarDataResult<IReadOnlyList<RegistrarDnsRecord>>>? GetDnsRecords { get; set; }

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
    public Task<RegistrarDataResult<RegistrarAvailability>> CheckAvailabilityAsync(RegistrarAvailabilityContext context, CancellationToken ct)
    {
        Count(nameof(CheckAvailabilityAsync));
        return Task.FromResult(RegistrarDataResult<RegistrarAvailability>.Done(new RegistrarAvailability(true)));
    }

    /// <inheritdoc />
    public Task<RegistrarDataResult<RegistrarOrder>> RegisterAsync(RegistrarRegisterContext context, CancellationToken ct)
    {
        return Task.FromResult(Order(Register, nameof(RegisterAsync), true));
    }

    /// <inheritdoc />
    public Task<RegistrarDataResult<RegistrarOrder>> TransferAsync(RegistrarTransferContext context, CancellationToken ct)
    {
        return Task.FromResult(Order(Transfer, nameof(TransferAsync), true));
    }

    /// <inheritdoc />
    public Task<RegistrarDataResult<RegistrarOrder>> RenewAsync(RegistrarRenewContext context, CancellationToken ct)
    {
        return Task.FromResult(Order(Renew, nameof(RenewAsync), false));
    }

    /// <inheritdoc />
    public Task<RegistrarDataResult<RegistrarOrder>> CheckDomainActiveAsync(RegistrarDomainContext context, CancellationToken ct)
    {
        return Task.FromResult(Read(nameof(CheckDomainActiveAsync), DefaultOrder));
    }

    /// <inheritdoc />
    public Task<RegistrarActionResult> CancelTransferAsync(RegistrarDomainContext context, CancellationToken ct)
    {
        return Task.FromResult(Change(null, nameof(CancelTransferAsync)));
    }

    /// <inheritdoc />
    public Task<RegistrarActionResult> InitiateOutgoingTransferAsync(RegistrarDomainContext context, CancellationToken ct)
    {
        return Task.FromResult(Change(null, nameof(InitiateOutgoingTransferAsync)));
    }

    /// <inheritdoc />
    public Task<RegistrarDataResult<RegistrarEppCode>> GetEppCodeAsync(RegistrarDomainContext context, CancellationToken ct)
    {
        var call = Count(nameof(GetEppCodeAsync));
        return Task.FromResult(GetEppCode is null ? DataOrNotFound(new RegistrarEppCode(RegistrarFixtures.EppCode)) : GetEppCode(call));
    }

    /// <inheritdoc />
    public Task<RegistrarDataResult<IReadOnlyList<string>>> GetNameserversAsync(RegistrarDomainContext context, CancellationToken ct)
    {
        Count(nameof(GetNameserversAsync));
        return Task.FromResult(DataOrNotFound<IReadOnlyList<string>>(["ns1.example.test", "ns2.example.test"]));
    }

    /// <inheritdoc />
    public Task<RegistrarActionResult> SetNameserversAsync(RegistrarNameserversContext context, CancellationToken ct)
    {
        return Task.FromResult(Change(SetNameservers, nameof(SetNameserversAsync)));
    }

    /// <inheritdoc />
    public Task<RegistrarActionResult> SetAutoRenewAsync(RegistrarFlagContext context, CancellationToken ct)
    {
        return Task.FromResult(Change(SetAutoRenew, nameof(SetAutoRenewAsync)));
    }

    /// <inheritdoc />
    public Task<RegistrarActionResult> SetWhoisPrivacyAsync(RegistrarFlagContext context, CancellationToken ct)
    {
        return Task.FromResult(Change(SetWhoisPrivacy, nameof(SetWhoisPrivacyAsync)));
    }

    /// <inheritdoc />
    public Task<RegistrarActionResult> SetRegistrarLockAsync(RegistrarFlagContext context, CancellationToken ct)
    {
        return Task.FromResult(Change(SetRegistrarLock, nameof(SetRegistrarLockAsync)));
    }

    /// <inheritdoc />
    public Task<RegistrarDataResult<RegistrarContact>> GetContactAsync(RegistrarDomainContext context, CancellationToken ct)
    {
        Count(nameof(GetContactAsync));
        return Task.FromResult(DataOrNotFound(RegistrarFixtures.Contact));
    }

    /// <inheritdoc />
    public Task<RegistrarActionResult> SetContactAsync(RegistrarContactContext context, CancellationToken ct)
    {
        return Task.FromResult(Change(SetContact, nameof(SetContactAsync)));
    }

    /// <inheritdoc />
    public Task<RegistrarDataResult<IReadOnlyList<RegistrarDnsRecord>>> GetDnsRecordsAsync(RegistrarDomainContext context, CancellationToken ct)
    {
        var call = Count(nameof(GetDnsRecordsAsync));
        return Task.FromResult(GetDnsRecords is null ? DataOrNotFound<IReadOnlyList<RegistrarDnsRecord>>([]) : GetDnsRecords(call));
    }

    /// <inheritdoc />
    public Task<RegistrarActionResult> AddDnsRecordAsync(RegistrarDnsRecordContext context, CancellationToken ct)
    {
        return Task.FromResult(Change(null, nameof(AddDnsRecordAsync)));
    }

    /// <inheritdoc />
    public Task<RegistrarActionResult> UpdateDnsRecordAsync(RegistrarDnsRecordContext context, CancellationToken ct)
    {
        return Task.FromResult(Change(null, nameof(UpdateDnsRecordAsync)));
    }

    /// <inheritdoc />
    public Task<RegistrarActionResult> DeleteDnsRecordAsync(RegistrarDnsRecordRemovalContext context, CancellationToken ct)
    {
        var call = Count(nameof(DeleteDnsRecordAsync));
        return Task.FromResult(DeleteDnsRecord is null ? RepeatOf(call) : DeleteDnsRecord(call));
    }

    /// <inheritdoc />
    public Task<RegistrarDataResult<IReadOnlyList<RegistrarTldPricing>>> GetTldPricingAsync(RegistrarPricingContext context, CancellationToken ct)
    {
        Count(nameof(GetTldPricingAsync));
        return Task.FromResult(RegistrarDataResult<IReadOnlyList<RegistrarTldPricing>>.Done([]));
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
    private RegistrarDataResult<RegistrarOrder> Order(Func<int, RegistrarDataResult<RegistrarOrder>>? script, string operation, bool createsDomain)
    {
        var call = Count(operation);
        if (script is not null)
        {
            return script(call);
        }

        if (!createsDomain && !HoldsDomain)
        {
            return RegistrarDataResult<RegistrarOrder>.NotFound();
        }

        return call == 1 ? RegistrarDataResult<RegistrarOrder>.Done(DefaultOrder) : RegistrarDataResult<RegistrarOrder>.AlreadyDone(DefaultOrder);
    }

    /// <summary>Answers a read of the domain.</summary>
    /// <typeparam name="T">The value read.</typeparam>
    /// <param name="operation">The method name.</param>
    /// <param name="value">The value the registrar holds.</param>
    /// <returns>The value, or not found when the registrar holds no domain.</returns>
    private RegistrarDataResult<T> Read<T>(string operation, T value)
        where T : class
    {
        Count(operation);
        return DataOrNotFound(value);
    }

    /// <summary>Answers a change of the domain: the script when there is one, otherwise done on the first call and already done after.</summary>
    /// <param name="script">The test's script, or <see langword="null"/>.</param>
    /// <param name="operation">The method name.</param>
    /// <returns>The answer.</returns>
    private RegistrarActionResult Change(Func<int, RegistrarActionResult>? script, string operation)
    {
        var call = Count(operation);
        if (script is not null)
        {
            return script(call);
        }

        return HoldsDomain ? RepeatOf(call) : RegistrarActionResult.NotFound();
    }

    /// <summary>Gets the answer of an idempotent change: done the first time and already done after.</summary>
    /// <param name="call">The call number, starting at 1.</param>
    /// <returns>The answer.</returns>
    private static RegistrarActionResult RepeatOf(int call)
    {
        return call == 1 ? RegistrarActionResult.Done() : RegistrarActionResult.AlreadyDone();
    }

    /// <summary>Gets a successful answer when the registrar holds the domain, otherwise not found.</summary>
    /// <typeparam name="T">The value read.</typeparam>
    /// <param name="value">The value the registrar holds.</param>
    /// <returns>The answer.</returns>
    private RegistrarDataResult<T> DataOrNotFound<T>(T value)
        where T : class
    {
        return HoldsDomain ? RegistrarDataResult<T>.Done(value) : RegistrarDataResult<T>.NotFound();
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
