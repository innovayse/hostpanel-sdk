namespace Innovayse.SDK.Plugins.Enums;

/// <summary>
/// The lifecycle state of a client's service as <see cref="Host.IPluginHostApi"/> reports it. Values start at 1;
/// 0 is not a status.
/// </summary>
public enum PluginServiceStatus
{
    /// <summary>The service is ordered but not provisioned yet.</summary>
    Pending = 1,

    /// <summary>The service is active.</summary>
    Active = 2,

    /// <summary>The service is temporarily suspended.</summary>
    Suspended = 3,

    /// <summary>The service is permanently terminated.</summary>
    Terminated = 4,
}
