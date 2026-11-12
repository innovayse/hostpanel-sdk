namespace Hostpanel.Sdk.Plugins.Enums;

/// <summary>Mapped state of a gateway payment.</summary>
public enum GatewayPaymentState
{
    /// <summary>The payment is not finished yet (session open, or authorization in progress).</summary>
    Pending = 0,

    /// <summary>Funds were captured; the payment is complete.</summary>
    Paid = 1,

    /// <summary>The payment was declined, cancelled, refunded, or the session is unknown to the gateway.</summary>
    Declined = 2,
}
