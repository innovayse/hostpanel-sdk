using Hostpanel.Sdk.Plugins.Fulfilment;

namespace Hostpanel.Sdk.Tests.Fakes;

/// <summary>A fulfilment plugin built as it was before 3.12.0: it implements only the four original members.</summary>
internal sealed class QuietFulfilmentPlugin : IFulfilmentPlugin
{
    /// <inheritdoc />
    public Task OnInvoicePaidAsync(InvoicePaidContext context, CancellationToken ct)
    {
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<RefundAdvice> AdviseRefundAsync(RefundAdviceContext context, CancellationToken ct)
    {
        throw new NotSupportedException();
    }

    /// <inheritdoc />
    public Task OnInvoiceRefundedAsync(InvoiceRefundedContext context, CancellationToken ct)
    {
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task OnSubscriptionLapsedAsync(SubscriptionLapsedContext context, CancellationToken ct)
    {
        return Task.CompletedTask;
    }
}
