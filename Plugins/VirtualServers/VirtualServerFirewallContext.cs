using Hostpanel.Sdk.Plugins.Provisioning;

namespace Hostpanel.Sdk.Plugins.VirtualServers;

/// <summary>Input for <see cref="IVirtualServerFirewallPlugin.SetFirewallRulesAsync"/>.</summary>
/// <param name="Server">The platform the virtual server lives on.</param>
/// <param name="Service">The service and its virtual server; <see cref="PanelService.ProvisioningRef"/> is set.</param>
/// <param name="Rules">The whole set of rules the server should have afterwards, in the order they apply. An empty list removes every rule.</param>
public sealed record VirtualServerFirewallContext(PanelServerConnection Server, PanelService Service, IReadOnlyList<VirtualServerFirewallRule> Rules);
