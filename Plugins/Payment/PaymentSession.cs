namespace Hostpanel.Sdk.Plugins.Payment;

/// <summary>Result of a successful payment registration at the gateway.</summary>
/// <param name="GatewayOrderId">The gateway-side order id; store it: status and refund calls key off it.</param>
/// <param name="RedirectUrl">
/// The hosted payment page URL to redirect the payer to; never empty. When <paramref name="FormPost"/> is set the host
/// ignores it, and the plugin passes the same address as <see cref="PaymentFormPost.Action"/>.
/// </param>
/// <param name="FormPost">
/// Describes a hosted payment page that is opened with a form POST. Optional and <see langword="null"/> by default,
/// which is the plain redirect to <paramref name="RedirectUrl"/>, so existing plugins are unchanged. See
/// <see cref="PaymentFormPost"/> for the rules the host applies.
/// </param>
public sealed record PaymentSession(string GatewayOrderId, string RedirectUrl, PaymentFormPost? FormPost = null)
{
    /// <summary>The address to redirect to, which is never empty.</summary>
    private readonly string _redirectUrl = RequireAddress(RedirectUrl);

    /// <summary>
    /// Gets the hosted payment page URL to redirect the payer to. It is never empty or white space, also when it is
    /// set with a <c>with</c> expression.
    /// </summary>
    public string RedirectUrl
    {
        get => _redirectUrl;
        init => _redirectUrl = RequireAddress(value);
    }

    /// <summary>
    /// Initializes a session that is a plain redirect. It keeps the two argument constructor of earlier versions, which
    /// a plugin built against them calls, so such a plugin loads and runs without a rebuild.
    /// </summary>
    /// <param name="gatewayOrderId">The gateway-side order id.</param>
    /// <param name="redirectUrl">The hosted payment page URL to redirect the payer to.</param>
    public PaymentSession(string gatewayOrderId, string redirectUrl)
        : this(gatewayOrderId, redirectUrl, null)
    {
    }

    /// <summary>Refuses an empty address, because the host has nowhere to send the payer.</summary>
    /// <param name="address">The address to check.</param>
    /// <returns>The address.</returns>
    /// <exception cref="ArgumentException">The address is null, empty or white space.</exception>
    private static string RequireAddress(string address)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(address, nameof(RedirectUrl));
        return address;
    }
}
