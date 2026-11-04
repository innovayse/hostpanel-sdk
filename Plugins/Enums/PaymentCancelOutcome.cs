namespace Hostpanel.Sdk.Plugins.Enums;

/// <summary>How a request to cancel an unsettled payment ended. Values start at 1; 0 is not an outcome.</summary>
public enum PaymentCancelOutcome
{
    /// <summary>The payment was open and is now cancelled or voided at the gateway.</summary>
    Cancelled = 1,

    /// <summary>The payment was already cancelled, so a repeated request changed nothing.</summary>
    AlreadyCancelled = 2,

    /// <summary>The payment can no longer be cancelled, for example because funds were captured; a refund is the way back.</summary>
    NotCancellable = 3,
}
