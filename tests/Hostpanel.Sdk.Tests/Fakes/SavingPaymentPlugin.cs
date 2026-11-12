using Hostpanel.Sdk.Plugins.Enums;
using Hostpanel.Sdk.Plugins.Payment;
using Hostpanel.Sdk.Plugins.StoredCredentials;

namespace Hostpanel.Sdk.Tests.Fakes;

/// <summary>
/// A payment plugin that honours <see cref="PaymentInstruction.SavePaymentMethod"/>: it remembers the flag and the customer
/// per gateway order and, once the payment is paid, reports the stored method in
/// <see cref="GatewayPaymentStatus.SavedMethod"/>
/// and attaches it to the customer the instruction named in <see cref="PaymentInstruction.CustomerRef"/>.
/// </summary>
internal sealed class SavingPaymentPlugin : IPaymentPlugin
{
    /// <summary>The method this fake gateway stores when asked to.</summary>
    public static readonly SavedMethod StoredMethod = new("tok_1", "visa", "4242", 12, 2030, false);

    /// <summary>Holds the customer each gateway order asked to save the method for; null when none was given.</summary>
    private readonly Dictionary<string, string?> _saving = [];

    /// <summary>Holds the methods attached to each gateway customer, the way a gateway keeps them.</summary>
    private readonly Dictionary<string, List<SavedMethod>> _methodsByCustomer = [];

    /// <inheritdoc />
    public string CurrencyCode => "051";

    /// <summary>Reads the methods the gateway holds for a customer.</summary>
    /// <param name="customerRef">The gateway-side customer reference.</param>
    /// <returns>The methods attached to the customer; empty when there are none.</returns>
    public IReadOnlyList<SavedMethod> MethodsOf(string customerRef)
    {
        return _methodsByCustomer.TryGetValue(customerRef, out var methods) ? methods : [];
    }

    /// <inheritdoc />
    public Task<PaymentSession> CreatePaymentAsync(PaymentInstruction instruction, CancellationToken ct)
    {
        var gatewayOrderId = "gw-" + instruction.OrderNumber;
        if (instruction.SavePaymentMethod)
        {
            _saving[gatewayOrderId] = instruction.CustomerRef;
        }

        return Task.FromResult(new PaymentSession(gatewayOrderId, "https://pay.example/" + gatewayOrderId));
    }

    /// <inheritdoc />
    public Task<GatewayPaymentStatus> GetStatusAsync(string gatewayOrderId, CancellationToken ct)
    {
        SavedMethod? method = null;
        if (_saving.TryGetValue(gatewayOrderId, out var customerRef))
        {
            method = StoredMethod;
            if (customerRef is not null)
            {
                Attach(customerRef, method);
            }
        }

        return Task.FromResult(new GatewayPaymentStatus(GatewayPaymentState.Paid, "tx-1", "PAID", SavedMethod: method));
    }

    /// <inheritdoc />
    public Task<string> RefundAsync(string gatewayOrderId, long amountMinor, CancellationToken ct)
    {
        throw new NotSupportedException();
    }

    /// <summary>Attaches a method to a customer once; a repeated status read does not duplicate it.</summary>
    /// <param name="customerRef">The gateway-side customer reference.</param>
    /// <param name="method">The method to attach.</param>
    private void Attach(string customerRef, SavedMethod method)
    {
        if (!_methodsByCustomer.TryGetValue(customerRef, out var methods))
        {
            methods = [];
            _methodsByCustomer[customerRef] = methods;
        }

        if (!methods.Contains(method))
        {
            methods.Add(method);
        }
    }
}
