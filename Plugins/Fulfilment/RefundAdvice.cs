namespace Innovayse.SDK.Plugins.Fulfilment;

/// <summary>A fulfilment plugin's answer to "how much of this invoice may still be refunded?".</summary>
/// <param name="MaxRefundableAmount">
/// Upper bound in the invoice's currency; the host caps the refund at the smallest advice it
/// receives. Return the requested amount to allow it in full.
/// </param>
/// <param name="Reason">A short sentence for the admin explaining a cap, or null when nothing was capped.</param>
public sealed record RefundAdvice(decimal MaxRefundableAmount, string? Reason);
