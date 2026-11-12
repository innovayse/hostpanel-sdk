using Hostpanel.Sdk.Plugins.Enums;

namespace Hostpanel.Sdk.Plugins.Payment;

/// <summary>Result of a successful payment registration at the gateway.</summary>
/// <param name="GatewayOrderId">The gateway-side order id; store it: status and refund calls key off it.</param>
/// <param name="RedirectUrl">
/// The hosted payment page URL to redirect the payer to; never empty. When <paramref name="FormPost"/> is set the host
/// ignores it, and the plugin passes the same address as <see cref="PaymentFormPost.Action"/>. For
/// <see cref="PaymentSessionKind.BrowserStep"/> it is the page where the payer completes the step.
/// </param>
/// <param name="FormPost">
/// Describes a hosted payment page that is opened with a form POST. Optional and <see langword="null"/> by default,
/// which is the plain redirect to <paramref name="RedirectUrl"/>, so existing plugins are unchanged. See
/// <see cref="PaymentFormPost"/> for the rules the host applies.
/// </param>
/// <param name="Kind">
/// How the payer is taken through the hosted step: <see cref="PaymentSessionKind.Redirect"/> by default. A session is
/// exactly one kind. A session with <paramref name="FormPost"/> is always of kind
/// <see cref="PaymentSessionKind.FormPost"/>, so a session built with only a form post keeps that meaning, and the kind
/// can be given as <see cref="PaymentSessionKind.FormPost"/> only together with a form post. Only
/// <see cref="PaymentSessionKind.BrowserStep"/> needs to be given, and not together with a form post. An undefined
/// value is refused.
/// </param>
public sealed record PaymentSession(
    string GatewayOrderId,
    string RedirectUrl,
    PaymentFormPost? FormPost = null,
    PaymentSessionKind Kind = PaymentSessionKind.Redirect)
{
    /// <summary>The address to redirect to, which is never empty.</summary>
    private readonly string _redirectUrl = RequireAddress(RedirectUrl);

    /// <summary>Whether the payer completes a step in the browser; checked against the form post when it is
    /// set.</summary>
    private readonly bool _awaitsBrowserStep = ResolveBrowserStep(Kind, FormPost);

    /// <summary>
    /// Initializes a session that is a plain redirect. It keeps the two argument constructor of earlier versions, which
    /// a plugin built against them calls, so such a plugin loads and runs without a rebuild.
    /// </summary>
    /// <param name="gatewayOrderId">The gateway-side order id.</param>
    /// <param name="redirectUrl">The hosted payment page URL to redirect the payer to.</param>
    public PaymentSession(string gatewayOrderId, string redirectUrl)
        : this(gatewayOrderId, redirectUrl, null, PaymentSessionKind.Redirect)
    {
    }

    /// <summary>
    /// Initializes a session that is a redirect, or a form post when a form is given. It keeps the three argument
    /// constructor of the form post version, which a plugin built against it calls, so such a plugin loads and runs
    /// without a rebuild.
    /// </summary>
    /// <param name="gatewayOrderId">The gateway-side order id.</param>
    /// <param name="redirectUrl">The hosted payment page URL to redirect the payer to.</param>
    /// <param name="formPost">The form to post, or <see langword="null"/> for a plain redirect.</param>
    public PaymentSession(string gatewayOrderId, string redirectUrl, PaymentFormPost? formPost)
        : this(gatewayOrderId, redirectUrl, formPost, PaymentSessionKind.Redirect)
    {
    }

    /// <summary>
    /// Gets the hosted payment page URL to redirect the payer to. It is never empty or white space, also when it is set
    /// with a <c>with</c> expression.
    /// </summary>
    public string RedirectUrl
    {
        get => _redirectUrl;
        init => _redirectUrl = RequireAddress(value);
    }

    /// <summary>
    /// Gets how the payer is taken through the hosted step. A session with a form post is of kind
    /// <see cref="PaymentSessionKind.FormPost"/>; otherwise it is <see cref="PaymentSessionKind.BrowserStep"/> when
    /// that was asked for, and <see cref="PaymentSessionKind.Redirect"/> when not. Setting it with a <c>with</c>
    /// expression follows the rules of the constructor.
    /// </summary>
    public PaymentSessionKind Kind
    {
        get => FormPost is not null
            ? PaymentSessionKind.FormPost
            : _awaitsBrowserStep ? PaymentSessionKind.BrowserStep : PaymentSessionKind.Redirect;
        init => _awaitsBrowserStep = ResolveBrowserStep(value, FormPost);
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

    /// <summary>Checks a kind against the form post and tells whether it is the browser step.</summary>
    /// <param name="kind">The kind that was asked for.</param>
    /// <param name="formPost">The form post of the session, or <see langword="null"/>.</param>
    /// <returns><see langword="true"/> for <see cref="PaymentSessionKind.BrowserStep"/>.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The kind is not a defined value.</exception>
    /// <exception cref="ArgumentException">
    /// The form post kind is given without a form post, or the browser step together with one.
    /// </exception>
    private static bool ResolveBrowserStep(PaymentSessionKind kind, PaymentFormPost? formPost)
    {
        if (!Enum.IsDefined(kind))
        {
            throw new ArgumentOutOfRangeException(nameof(Kind), kind, "The session kind is not a defined value.");
        }

        if (kind == PaymentSessionKind.FormPost && formPost is null)
        {
            throw new ArgumentException("The form post kind needs a form post.", nameof(Kind));
        }

        if (kind == PaymentSessionKind.BrowserStep && formPost is not null)
        {
            throw new ArgumentException("A session is a form post or a browser step, not both.", nameof(Kind));
        }

        return kind == PaymentSessionKind.BrowserStep;
    }
}
