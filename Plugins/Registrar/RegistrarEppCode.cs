using System.Text;
using Hostpanel.Sdk.Plugins.Diagnostics;

namespace Hostpanel.Sdk.Plugins.Registrar;

/// <summary>
/// The authorization (EPP) code that lets another registrar take a domain over. Whoever holds it can move the domain, so
/// it is a secret: the generated <c>ToString</c> prints it masked, and neither the plugin nor the host logs it.
/// </summary>
/// <param name="Code">The authorization code. Not empty. Secret.</param>
public sealed record RegistrarEppCode(string Code)
{
    /// <summary>Prints the record without its secret member, so the generated <c>ToString</c> never reveals it.</summary>
    /// <param name="builder">The builder the generated <c>ToString</c> is assembling.</param>
    /// <returns>Always <see langword="true"/>: members are written.</returns>
    private bool PrintMembers(StringBuilder builder)
    {
        builder.Append("Code = ").Append(SecretMask.Text);
        return true;
    }
}
