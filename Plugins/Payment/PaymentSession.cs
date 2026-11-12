namespace Hostpanel.Sdk.Plugins.Payment;

/// <summary>Result of a successful payment registration at the gateway.</summary>
/// <param name="GatewayOrderId">The gateway-side order id; store it: status and refund calls key off it.</param>
/// <param name="RedirectUrl">
/// The hosted payment page URL to redirect the payer to. When <paramref name="FormPost"/> is set the host ignores it,
/// and the plugin passes the same address or an empty string.
/// </param>
/// <param name="FormPost">
/// Describes a hosted payment page that is opened with a form POST. Optional and <see langword="null"/> by default,
/// which is the plain redirect to <paramref name="RedirectUrl"/>, so existing plugins are unchanged. See
/// <see cref="PaymentFormPost"/> for the rules the host applies.
/// </param>
public sealed record PaymentSession(string GatewayOrderId, string RedirectUrl, PaymentFormPost? FormPost = null);
