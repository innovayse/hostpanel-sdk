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
public sealed partial record VirtualServerRebuildContext(
    PanelServerConnection Server,
    PanelService Service,
    string ImageId,
    [property: SecretMember] string RootPassword)
{
    /// <summary>
    /// Gets a public key to authorize for the root user after the rebuild, or <see langword="null"/> for none. A rebuild erases
    /// the keys the earlier install had, so the host sends the key again when the client has one. A platform that cannot
    /// install a key ignores it. Optional and null by default, so existing plugins and hosts are unchanged.
    /// </summary>
    public string? SshPublicKey { get; init; }
}
