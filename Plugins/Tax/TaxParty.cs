using System.Text;
using Hostpanel.Sdk.Plugins.Diagnostics;

namespace Hostpanel.Sdk.Plugins.Tax;

/// <summary>
/// One side of a sale, the seller or the buyer, as a tax service needs it. A tax number identifies a person or a
/// company, so the generated <c>ToString</c> masks it, and the address masks its own details.
/// </summary>
/// <param name="Address">Where the party is.</param>
/// <param name="IsBusiness">
/// <see langword="true"/> for a business, <see langword="false"/> for a private person. It decides, for example,
/// whether a reverse charge can apply.
/// </param>
/// <param name="TaxId">
/// The party's tax or VAT number as the host stores it, or <see langword="null"/> when there is none.
/// </param>
public sealed record TaxParty(TaxAddress Address, bool IsBusiness, string? TaxId)
{
    /// <summary>Prints the record without the tax number, so the generated <c>ToString</c> never reveals it.</summary>
    /// <param name="builder">The builder the generated <c>ToString</c> is assembling.</param>
    /// <returns>Always <see langword="true"/>: members are written.</returns>
    private bool PrintMembers(StringBuilder builder)
    {
        builder.Append("Address = ").Append(Address);
        builder.Append(", IsBusiness = ").Append(IsBusiness);
        builder.Append(", TaxId = ").Append(TaxId is null ? "null" : SecretMask.Text);
        return true;
    }
}
