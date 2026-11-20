namespace Hostpanel.Sdk.Plugins.Enums;

/// <summary>What a firewall rule of a virtual server does with the traffic it matches. The values start at 1 and 0 is not valid.</summary>
public enum VirtualServerFirewallAction
{
    /// <summary>The traffic passes.</summary>
    Allow = 1,

    /// <summary>The traffic is dropped.</summary>
    Deny = 2,
}
