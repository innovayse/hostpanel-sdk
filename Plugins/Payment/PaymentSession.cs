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
/// Describes a hosted payment page that is opened with a form POST. It is required when <paramref name="Kind"/> is
/// <see cref="PaymentSessionKind.FormPost"/> and must be <see langword="null"/> for every other kind. See
/// <see cref="PaymentFormPost"/> for the rules the host applies.
/// </param>
/// <param name="Kind">
/// How the payer is taken through the hosted step: <see cref="PaymentSessionKind.Redirect"/> by default. The kind and
/// the form post always agree, and a pair that contradicts is refused, whatever the order it is set in: a form post is
/// required for <see cref="PaymentSessionKind.FormPost"/> and allowed for nothing else, and an undefined value is
/// refused. The constructors of earlier versions derive the kind from the form.
/// </param>
public sealed record PaymentSession(
    string GatewayOrderId,
    string RedirectUrl,
    PaymentFormPost? FormPost = null,
    PaymentSessionKind Kind = PaymentSessionKind.Redirect)
{
    /// <summary>The address to redirect to, which is never empty.</summary>
    private readonly string _redirectUrl = RequireAddress(RedirectUrl);

    /// <summary>
    /// The form post and the kind, kept together in one value so that the pair is checked every time either changes.
    /// </summary>
    private readonly (PaymentFormPost? Form, PaymentSessionKind Kind) _step = CheckPair(FormPost, Kind);

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
    /// Initializes a session that is a redirect, or a form post when a form is given: the kind is derived from the
    /// form. It keeps the three argument constructor of the form post version, which a plugin built against it calls,
    /// so such a plugin loads and runs without a rebuild.
    /// </summary>
    /// <param name="gatewayOrderId">The gateway-side order id.</param>
    /// <param name="redirectUrl">The hosted payment page URL to redirect the payer to.</param>
    /// <param name="formPost">The form to post, or <see langword="null"/> for a plain redirect.</param>
    public PaymentSession(string gatewayOrderId, string redirectUrl, PaymentFormPost? formPost)
        : this(
            gatewayOrderId,
            redirectUrl,
            formPost,
            formPost is null ? PaymentSessionKind.Redirect : PaymentSessionKind.FormPost)
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
    /// Gets the form post, which is set exactly when <see cref="Kind"/> is <see cref="PaymentSessionKind.FormPost"/>.
    /// Setting it with a <c>with</c> expression is checked against the kind as it is at that moment, so a form can be
    /// replaced on a form post session and cannot be added to another kind.
    /// </summary>
    public PaymentFormPost? FormPost
    {
        get => _step.Form;
        init => _step = CheckPair(value, _step.Kind);
    }

    /// <summary>
    /// Gets how the payer is taken through the hosted step. Setting it with a <c>with</c> expression is checked against
    /// the form post as it is at that moment, so a session can change between <see cref="PaymentSessionKind.Redirect"/>
    /// and <see cref="PaymentSessionKind.BrowserStep"/>, and a change to or from
    /// <see cref="PaymentSessionKind.FormPost"/> needs a new session.
    /// </summary>
    public PaymentSessionKind Kind
    {
        get => _step.Kind;
        init => _step = CheckPair(_step.Form, value);
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

    /// <summary>Checks that a form post and a kind agree.</summary>
    /// <param name="formPost">The form post, or <see langword="null"/>.</param>
    /// <param name="kind">The kind.</param>
    /// <returns>The pair, unchanged.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The kind is not a defined value.</exception>
    /// <exception cref="ArgumentException">
    /// The form post kind has no form post, or another kind has one.
    /// </exception>
    private static (PaymentFormPost? Form, PaymentSessionKind Kind) CheckPair(
        PaymentFormPost? formPost,
        PaymentSessionKind kind)
    {
        if (!Enum.IsDefined(kind))
        {
            throw new ArgumentOutOfRangeException(nameof(Kind), kind, "The session kind is not a defined value.");
        }

        if (kind == PaymentSessionKind.FormPost && formPost is null)
        {
            throw new ArgumentException("The form post kind needs a form post.", nameof(Kind));
        }

        if (kind != PaymentSessionKind.FormPost && formPost is not null)
        {
            throw new ArgumentException("Only the form post kind carries a form post.", nameof(FormPost));
        }

        return (formPost, kind);
    }
}
