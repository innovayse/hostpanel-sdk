using Hostpanel.Sdk.Plugins.Payment;
using Hostpanel.Sdk.Plugins.StoredCredentials;

namespace Hostpanel.Sdk.Tests.Fakes;

/// <summary>
/// A stored credential plugin whose <c>CompleteSaveMethodAsync</c> is scripted by the test, so the contract helper
/// can be shown to accept a well behaved plugin and to reject each way of breaking the promise. It is a payment plugin
/// too, as the interface requires.
/// </summary>
internal sealed class ScriptedStoredCredentialPlugin : IStoredCredentialPlugin
{
    /// <summary>What <c>CompleteSaveMethodAsync</c> does on each call; the call number starts at 1.</summary>
    private readonly Func<int, SavedMethodResult> _complete;

    /// <summary>Initializes a new instance of the <see cref="ScriptedStoredCredentialPlugin"/> class.</summary>
    /// <param name="complete">The answer for each call number, or a throw.</param>
    public ScriptedStoredCredentialPlugin(Func<int, SavedMethodResult> complete)
    {
        _complete = complete;
    }

    /// <summary>Gets how many times <c>CompleteSaveMethodAsync</c> was called.</summary>
    public int Calls { get; private set; }

    /// <inheritdoc />
    public string CurrencyCode => "051";

    /// <inheritdoc />
    public Task<SavedMethodResult> CompleteSaveMethodAsync(string sessionId, CancellationToken ct)
    {
        Calls++;
        return Task.FromResult(_complete(Calls));
    }

    /// <inheritdoc />
    public Task<PaymentSession> CreatePaymentAsync(PaymentInstruction instruction, CancellationToken ct)
    {
        throw new NotSupportedException();
    }

    /// <inheritdoc />
    public Task<GatewayPaymentStatus> GetStatusAsync(string gatewayOrderId, CancellationToken ct)
    {
        throw new NotSupportedException();
    }

    /// <inheritdoc />
    public Task<string> RefundAsync(string gatewayOrderId, long amountMinor, CancellationToken ct)
    {
        throw new NotSupportedException();
    }

    /// <inheritdoc />
    public Task<CredentialCustomerResult> EnsureCustomerAsync(CredentialCustomerContext context, CancellationToken ct)
    {
        throw new NotSupportedException();
    }

    /// <inheritdoc />
    public Task<SaveMethodSession> BeginSaveMethodAsync(SaveMethodContext context, CancellationToken ct)
    {
        throw new NotSupportedException();
    }

    /// <inheritdoc />
    public Task<StoredChargeResult> ChargeAsync(StoredChargeInstruction instruction, CancellationToken ct)
    {
        throw new NotSupportedException();
    }

    /// <inheritdoc />
    public Task<StoredChargeLookup> FindChargeAsync(string customerRef, string idempotencyKey, CancellationToken ct)
    {
        throw new NotSupportedException();
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<SavedMethod>> ListMethodsAsync(string customerRef, CancellationToken ct)
    {
        throw new NotSupportedException();
    }

    /// <inheritdoc />
    public Task SetDefaultMethodAsync(string customerRef, string methodRef, CancellationToken ct)
    {
        throw new NotSupportedException();
    }

    /// <inheritdoc />
    public Task RemoveMethodAsync(string customerRef, string methodRef, CancellationToken ct)
    {
        throw new NotSupportedException();
    }
}
