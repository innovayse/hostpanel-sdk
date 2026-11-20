using Hostpanel.Sdk.Plugins.Enums;

namespace Hostpanel.Sdk.Plugins.VirtualServers;

/// <summary>
/// One rule of the firewall of a virtual server. A rule is valid when its ports are set only for <see cref="VirtualServerFirewallProtocol.Tcp"/>
/// or <see cref="VirtualServerFirewallProtocol.Udp"/>, when <paramref name="PortTo"/> is set only with <paramref name="PortFrom"/>, and when
/// every port is between 1 and 65535 with <paramref name="PortTo"/> not below <paramref name="PortFrom"/>.
/// </summary>
/// <param name="Direction">Whether the rule applies to traffic that arrives at the server or leaves it.</param>
/// <param name="Action">Whether the traffic passes or is dropped.</param>
/// <param name="Protocol">The protocol the rule matches.</param>
/// <param name="PortFrom">The first port, or the only one when <paramref name="PortTo"/> is <see langword="null"/>; <see langword="null"/> for every port or a protocol without ports.</param>
/// <param name="PortTo">The last port of a range, or <see langword="null"/> for a single port.</param>
/// <param name="Address">The address or network (CIDR) of the other side the rule matches, or <see langword="null"/> for any address.</param>
/// <param name="Description">A note for the person who reads the rule, or <see langword="null"/>.</param>
public sealed record VirtualServerFirewallRule(
    VirtualServerFirewallDirection Direction,
    VirtualServerFirewallAction Action,
    VirtualServerFirewallProtocol Protocol,
    int? PortFrom,
    int? PortTo,
    string? Address,
    string? Description);
