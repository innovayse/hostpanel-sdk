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
public sealed partial record TaxParty(TaxAddress Address, bool IsBusiness, [property: SecretMember] string? TaxId);
