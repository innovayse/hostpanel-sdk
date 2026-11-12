using System.Text;

namespace Hostpanel.Sdk.Tests.Diagnostics;

/// <summary>
/// Records that break the rules of <see cref="SecretPrintingRules"/> on purpose. The guard tests are run over them as well,
/// to prove that each rule fails when it should and not only passes on a clean SDK.
/// </summary>
internal static class PlantedRecords
{
    /// <summary>A record with a typed out <c>PrintMembers</c>.</summary>
    /// <param name="Name">A member the hand written method lists.</param>
    internal sealed record HandWrittenPrintMembers(string Name)
    {
        /// <summary>Lists the member by hand, the shape the generator replaced.</summary>
        /// <param name="builder">The builder the generated <c>ToString</c> is assembling.</param>
        /// <returns>Always <see langword="true"/>.</returns>
        private bool PrintMembers(StringBuilder builder)
        {
            builder.Append("Name = ").Append(Name);
            return true;
        }
    }

    /// <summary>A record with a typed out <c>ToString</c>.</summary>
    /// <param name="Name">A member the override prints.</param>
    internal sealed record HandWrittenToString(string Name)
    {
        /// <summary>Prints the record by hand.</summary>
        /// <returns>The name.</returns>
        public override string ToString()
        {
            return Name;
        }
    }

    /// <summary>A record with credentials that are not marked: a text, bytes and a wrapper, besides a flag that only describes one.</summary>
    /// <param name="Password">Text named after a credential.</param>
    /// <param name="PrivateKey">Bytes named after a credential.</param>
    /// <param name="ApiToken">A wrapper named after a credential.</param>
    /// <param name="HasPassword">A flag, which is not a credential.</param>
    internal sealed record UnmarkedCredentials(string Password, byte[] PrivateKey, Lazy<string> ApiToken, bool HasPassword);
}
