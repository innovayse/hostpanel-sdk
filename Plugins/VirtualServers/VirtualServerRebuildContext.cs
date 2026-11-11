using System.Text;
using Hostpanel.Sdk.Plugins.Diagnostics;
using Hostpanel.Sdk.Plugins.Provisioning;

namespace Hostpanel.Sdk.Plugins.VirtualServers;

/// <summary>
/// Input for <see cref="IVirtualServerPlugin.RebuildServerAsync"/>. A rebuild erases the server's disk, so the host sends it
/// only on the client's explicit request. <see cref="RootPassword"/> is a secret and is printed masked.
/// </summary>
/// <param name="Server">The platform the virtual server lives on.</param>
/// <param name="Service">The service and its virtual server; <see cref="PanelService.ProvisioningRef"/> is set.</param>
/// <param name="ImageId">The id of the image to install, as <see cref="IVirtualServerPlugin.ListImagesAsync"/> returned it in <see cref="VirtualServerImage.Id"/>.</param>
/// <param name="RootPassword">The password of the root or administrator user after the rebuild. Secret. A platform that cannot set it generates one and answers it in <see cref="VirtualServerRootPassword.GeneratedPassword"/>.</param>
public sealed record VirtualServerRebuildContext(
    PanelServerConnection Server,
    PanelService Service,
    string ImageId,
    string RootPassword)
{
    /// <summary>Prints the record without its secret member, so the generated <c>ToString</c> never reveals it.</summary>
    /// <param name="builder">The builder the generated <c>ToString</c> is assembling.</param>
    /// <returns>Always <see langword="true"/>: members are written.</returns>
    private bool PrintMembers(StringBuilder builder)
    {
        builder.Append("Server = ").Append(Server);
        builder.Append(", Service = ").Append(Service);
        builder.Append(", ImageId = ").Append(ImageId);
        builder.Append(", RootPassword = ").Append(SecretMask.Text);
        return true;
    }
}
