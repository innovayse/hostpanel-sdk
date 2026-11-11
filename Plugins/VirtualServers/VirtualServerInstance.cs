using System.Text;
using Hostpanel.Sdk.Plugins.Diagnostics;

namespace Hostpanel.Sdk.Plugins.VirtualServers;

/// <summary>
/// A virtual server the platform created. The reference is stable, so a retried create answers the same value.
/// <see cref="GeneratedPassword"/> is a secret: the generated <c>ToString</c> prints it masked, and neither the plugin nor
/// the host logs it.
/// </summary>
/// <param name="ProvisioningRef">The platform's reference for the server. Not empty. The host keeps it and sends it back in <see cref="Provisioning.PanelService.ProvisioningRef"/> on every later operation.</param>
/// <param name="IpAddress">The server's primary IPv4 address when the platform already knows it, otherwise <see langword="null"/>.</param>
/// <param name="Ipv6Address">The server's primary IPv6 address when the platform already knows it, otherwise <see langword="null"/>.</param>
/// <param name="GeneratedPassword">The root password the platform generated because it cannot set the requested one, or <see langword="null"/> when the requested password was applied. Secret.</param>
public sealed record VirtualServerInstance(string ProvisioningRef, string? IpAddress, string? Ipv6Address, string? GeneratedPassword)
{
    /// <summary>Prints the record without its secret member, so the generated <c>ToString</c> never reveals it.</summary>
    /// <param name="builder">The builder the generated <c>ToString</c> is assembling.</param>
    /// <returns>Always <see langword="true"/>: members are written.</returns>
    private bool PrintMembers(StringBuilder builder)
    {
        builder.Append("ProvisioningRef = ").Append(ProvisioningRef);
        builder.Append(", IpAddress = ").Append(IpAddress);
        builder.Append(", Ipv6Address = ").Append(Ipv6Address);
        builder.Append(", GeneratedPassword = ").Append(GeneratedPassword is null ? "null" : SecretMask.Text);
        return true;
    }
}
