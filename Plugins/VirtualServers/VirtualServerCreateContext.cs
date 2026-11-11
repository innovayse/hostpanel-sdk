using System.Text;
using Hostpanel.Sdk.Plugins.Diagnostics;
using Hostpanel.Sdk.Plugins.Provisioning;

namespace Hostpanel.Sdk.Plugins.VirtualServers;

/// <summary>
/// Input for <see cref="IVirtualServerPlugin.CreateServerAsync"/>. The host chooses <see cref="Name"/> and stores it before it
/// calls the plugin, so a retry of the same create sends the same name: that is what makes the call idempotent.
/// <see cref="RootPassword"/> is a secret and is printed masked.
/// </summary>
/// <param name="Server">The platform to create the virtual server on.</param>
/// <param name="Service">The service the virtual server is for; <see cref="PanelService.ProvisioningRef"/> is <see langword="null"/>.</param>
/// <param name="Client">The client the virtual server is for. Required; the e-mail address and the name inside it are optional because the host may not have them.</param>
/// <param name="Plan">The plan to size the server by; <see cref="PanelPackage.Name"/> is a name <see cref="IVirtualServerPlugin.ListPlansAsync"/> returned.</param>
/// <param name="ImageId">The id of the image to install, as <see cref="IVirtualServerPlugin.ListImagesAsync"/> returned it in <see cref="VirtualServerImage.Id"/>.</param>
/// <param name="Name">The name of the virtual server on the platform, also used as its host name. Stable across retries of one create, and unique on the platform.</param>
/// <param name="RootPassword">The initial password of the root or administrator user. Secret. A platform that cannot set it generates one and answers it in <see cref="VirtualServerInstance.GeneratedPassword"/>.</param>
/// <param name="SshPublicKey">A public key to authorize for the root user, or <see langword="null"/> for none.</param>
public sealed record VirtualServerCreateContext(
    PanelServerConnection Server,
    PanelService Service,
    PanelClientRef Client,
    PanelPackage Plan,
    string ImageId,
    string Name,
    string RootPassword,
    string? SshPublicKey)
{
    /// <summary>Prints the record without its secret member, so the generated <c>ToString</c> never reveals it.</summary>
    /// <param name="builder">The builder the generated <c>ToString</c> is assembling.</param>
    /// <returns>Always <see langword="true"/>: members are written.</returns>
    private bool PrintMembers(StringBuilder builder)
    {
        builder.Append("Server = ").Append(Server);
        builder.Append(", Service = ").Append(Service);
        builder.Append(", Client = ").Append(Client);
        builder.Append(", Plan = ").Append(Plan);
        builder.Append(", ImageId = ").Append(ImageId);
        builder.Append(", Name = ").Append(Name);
        builder.Append(", RootPassword = ").Append(SecretMask.Text);
        builder.Append(", SshPublicKey = ").Append(SshPublicKey);
        return true;
    }
}
