namespace Hostpanel.Sdk.Plugins.Enums;

/// <summary>Why the host refused a request for access to a client's hosting (<see cref="Host.PluginHostAccessException"/>).</summary>
public enum PluginHostAccessFailure
{
    /// <summary>
    /// Not allowed: the manifest does not declare the access, the call is not a client-area call, or the service's
    /// product is not bound to the plugin.
    /// </summary>
    NotAllowed,

    /// <summary>The service, or the key, is not one of the caller's; another client's answers the same.</summary>
    NotFound,

    /// <summary>The service cannot take the change now: it is not active, or SSH is not available for it.</summary>
    Unavailable,

    /// <summary>The service already holds as many SSH keys as the host allows.</summary>
    LimitReached,

    /// <summary>The hosting server did not answer, or the outcome is unknown; try again later.</summary>
    ServerUnavailable,
}
