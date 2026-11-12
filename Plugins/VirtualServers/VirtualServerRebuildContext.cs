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
    [property: SecretMember] string RootPassword);
