namespace Hostpanel.Sdk.Plugins.Enums;

/// <summary>
/// How a tax service answered one <see cref="Tax.ITaxPlugin.CalculateTaxAsync"/> call. The values start at 1 and 0 is
/// not valid: a default-initialised value means the plugin set nothing, and the host refuses it. Whatever the outcome,
/// a rate is never guessed and a service that could not answer is never turned into zero tax. New values are only ever
/// appended.
/// </summary>
public enum TaxOutcome
{
    /// <summary>
    /// Every line of the invoice was calculated. The result carries one <see cref="Tax.TaxLineResult"/> per line.
    /// </summary>
    Calculated = 1,

    /// <summary>
    /// The service answered and declined to calculate, for example because it cannot tax the buyer's address. The
    /// result carries a <see cref="TaxRefusalReason"/>. Asking again with the same request gives the same answer.
    /// </summary>
    Refused = 2,

    /// <summary>
    /// The service could not answer now: it could not be reached, timed out or is rate limited. Nothing is known about
    /// the tax, so the host must not issue the invoice with a tax of zero; it tries again later. The result carries no
    /// reason.
    /// </summary>
    Unavailable = 3,
}
