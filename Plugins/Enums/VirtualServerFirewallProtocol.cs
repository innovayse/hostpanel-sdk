namespace Hostpanel.Sdk.Plugins.Enums;

/// <summary>The protocol a firewall rule of a virtual server matches. The values start at 1 and 0 is not valid.</summary>
public enum VirtualServerFirewallProtocol
{
    /// <summary>Any protocol; the rule has no ports.</summary>
    Any = 1,

    /// <summary>TCP, optionally limited to a port range.</summary>
    Tcp = 2,

    /// <summary>UDP, optionally limited to a port range.</summary>
    Udp = 3,

    /// <summary>ICMP; the rule has no ports.</summary>
    Icmp = 4,
}
