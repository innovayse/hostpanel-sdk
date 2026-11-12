using System.Text;
using Hostpanel.Sdk.Plugins.Diagnostics;

namespace Hostpanel.Sdk.Plugins.Tax;

/// <summary>
/// A postal address as a tax service places it: the country and region decide the jurisdiction, the rest narrows it. It
/// is personal data when it is the buyer's: the generated <c>ToString</c> prints the country and the region and masks
/// the rest, so a request that holds an address never leaks it into a log line or an exception message, and a plugin
/// must not undo that.
/// </summary>
/// <param name="Country">The ISO 3166-1 alpha-2 country code, in upper case.</param>
/// <param name="Region">
/// The state, province or other first level region, or <see langword="null"/> when the country has none or it is
/// unknown.
/// </param>
/// <param name="PostalCode">The postal code, or <see langword="null"/>.</param>
/// <param name="City">The city, or <see langword="null"/>.</param>
/// <param name="Line1">The first address line, or <see langword="null"/>.</param>
public sealed record TaxAddress(string Country, string? Region, string? PostalCode, string? City, string? Line1)
{
    /// <summary>
    /// Prints the record without its personal data, so the generated <c>ToString</c> never reveals it.
    /// </summary>
    /// <param name="builder">The builder the generated <c>ToString</c> is assembling.</param>
    /// <returns>Always <see langword="true"/>: members are written.</returns>
    private bool PrintMembers(StringBuilder builder)
    {
        builder.Append("Country = ").Append(Country);
        builder.Append(", Region = ").Append(Region);
        builder.Append(", PostalCode = ").Append(PostalCode is null ? "null" : SecretMask.Text);
        builder.Append(", City = ").Append(City is null ? "null" : SecretMask.Text);
        builder.Append(", Line1 = ").Append(Line1 is null ? "null" : SecretMask.Text);
        return true;
    }
}
