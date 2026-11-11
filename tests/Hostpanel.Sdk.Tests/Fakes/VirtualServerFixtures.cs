using Hostpanel.Sdk.Plugins.Provisioning;
using Hostpanel.Sdk.Plugins.VirtualServers;

namespace Hostpanel.Sdk.Tests.Fakes;

/// <summary>
/// The contexts the virtual server tests send. The platform, the service and the shared operations come from
/// <see cref="PanelFixtures"/>, because the contract reuses those records; only the virtual server specific ones are here.
/// </summary>
internal static class VirtualServerFixtures
{
    /// <summary>The root password of the fixture create.</summary>
    public const string RootPassword = "root-secret-301";

    /// <summary>The password the fixture rebuild asks for.</summary>
    public const string RebuildPassword = "root-secret-302";

    /// <summary>The root password a platform generated, as the fixture password reset answers it.</summary>
    public const string GeneratedPassword = "generated-secret-201";

    /// <summary>Gets a create for a service that has no server yet.</summary>
    public static VirtualServerCreateContext Create { get; } = new(
        PanelFixtures.Server,
        new PanelService(42, null),
        new PanelClientRef("client-9", "client@example.test", "A Client"),
        new PanelPackage("small"),
        "img-1",
        "vps-42",
        RootPassword,
        "ssh-ed25519 AAAAexample");

    /// <summary>Gets a rebuild of the existing server.</summary>
    public static VirtualServerRebuildContext Rebuild { get; } = new(PanelFixtures.Server, PanelFixtures.Service, "img-2", RebuildPassword);

    /// <summary>Gets the catalogue request.</summary>
    public static VirtualServerCatalogContext Catalog { get; } = new(PanelFixtures.Server);
}
