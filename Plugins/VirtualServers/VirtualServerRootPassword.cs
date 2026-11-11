using System.Text;
using Hostpanel.Sdk.Plugins.Diagnostics;

namespace Hostpanel.Sdk.Plugins.VirtualServers;

/// <summary>
/// What a rebuild or a root password reset answers. A platform that applied the password the host sent has nothing to add,
/// and one that can only generate a password answers it here. <see cref="GeneratedPassword"/> is a secret: the generated
/// <c>ToString</c> prints it masked.
/// </summary>
/// <param name="GeneratedPassword">The root password the platform generated, or <see langword="null"/> when the requested password was applied. Secret.</param>
public sealed record VirtualServerRootPassword(string? GeneratedPassword)
{
    /// <summary>Prints the record without its secret member, so the generated <c>ToString</c> never reveals it.</summary>
    /// <param name="builder">The builder the generated <c>ToString</c> is assembling.</param>
    /// <returns>Always <see langword="true"/>: members are written.</returns>
    private bool PrintMembers(StringBuilder builder)
    {
        builder.Append("GeneratedPassword = ").Append(GeneratedPassword is null ? "null" : SecretMask.Text);
        return true;
    }
}
