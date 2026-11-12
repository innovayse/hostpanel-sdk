namespace Hostpanel.Sdk.Plugins.Payment;

/// <summary>
/// A hosted payment page that is opened with a form POST instead of a plain redirect, for gateways that take a token or
/// other fields in the body of the request. The plugin only describes the form; the host renders it, so the plugin
/// never writes markup or script. The host applies these rules before it renders the page, and a session that breaks
/// one is refused as a failed payment:
/// <list type="bullet">
/// <item><description> <see cref="Action"/> must be an absolute <c>https</c> URL without user info or line breaks, and
/// within <see cref="PaymentFormPostLimits.MaxActionLength"/> characters.
/// </description></item>
/// <item><description> Field names must not be empty or contain line breaks. Names and values are encoded as attribute
/// text, never as markup.
/// </description></item>
/// <item><description> The page carries no script from the plugin and has a button that submits the form for a payer
/// whose browser runs no script.
/// </description></item>
/// </list>
/// </summary>
/// <param name="Action">The absolute <c>https</c> URL the form is posted to.</param>
/// <param name="Fields">
/// The names and values of the form fields, in the order they are rendered. May be empty. A name may repeat. The list
/// is compared by reference, as every list in an SDK record is, so two forms with equal fields in different list
/// objects are not equal. Keep within <see cref="PaymentFormPostLimits"/>.
/// </param>
public sealed record PaymentFormPost(string Action, IReadOnlyList<KeyValuePair<string, string>> Fields);
