namespace Hostpanel.Sdk.Plugins.Enums;

/// <summary>The direction of the traffic a firewall rule of a virtual server applies to. The values start at 1 and 0 is not valid.</summary>
public enum VirtualServerFirewallDirection
{
    /// <summary>Traffic that arrives at the server.</summary>
    Inbound = 1,

    /// <summary>Traffic that leaves the server.</summary>
    Outbound = 2,
}
