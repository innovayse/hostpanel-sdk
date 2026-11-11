namespace Hostpanel.Sdk.Plugins.Enums;

/// <summary>The power state of a virtual server, as a usage reading reports it. The values start at 1 and 0 is not valid.</summary>
public enum VirtualServerPowerState
{
    /// <summary>The server is running.</summary>
    Running = 1,

    /// <summary>The server is powered off.</summary>
    Stopped = 2,

    /// <summary>The server is suspended by the host: powered off and not startable by the client.</summary>
    Suspended = 3,

    /// <summary>The server is being created, rebuilt or moved to another plan and is not usable yet.</summary>
    Provisioning = 4,
}
