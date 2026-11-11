using System.Text;
using Hostpanel.Sdk.Plugins.Diagnostics;

namespace Hostpanel.Sdk.Plugins.Registrar;

/// <summary>Input for <see cref="IRegistrarEmailForwardingPlugin.DeleteEmailForwardingRuleAsync"/>. The source can be a personal e-mail address, so the generated <c>ToString</c> prints it masked.</summary>
/// <param name="Connection">The registrar account the domain is held on.</param>
/// <param name="Domain">The domain whose forwarding changes.</param>
/// <param name="Source">The source of the rule to remove, as the host stores it. Personal data.</param>
public sealed record RegistrarEmailForwardingRemovalContext(RegistrarConnection Connection, RegistrarDomain Domain, string Source)
{
    /// <summary>Prints the record without the source, so the generated <c>ToString</c> never reveals it.</summary>
    /// <param name="builder">The builder the generated <c>ToString</c> is assembling.</param>
    /// <returns>Always <see langword="true"/>: members are written.</returns>
    private bool PrintMembers(StringBuilder builder)
    {
        builder.Append("Connection = ").Append(Connection);
        builder.Append(", Domain = ").Append(Domain);
        builder.Append(", Source = ").Append(SecretMask.Text);
        return true;
    }
}
