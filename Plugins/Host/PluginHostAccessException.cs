using Hostpanel.Sdk.Plugins.Enums;

namespace Hostpanel.Sdk.Plugins.Host;

/// <summary>Thrown by <see cref="IPluginHostApi"/> when the host refuses a request for access to a client's hosting.</summary>
public sealed class PluginHostAccessException : Exception
{
    /// <summary>Initializes a new instance of the <see cref="PluginHostAccessException"/> class.</summary>
    /// <param name="failure">Why the host refused.</param>
    public PluginHostAccessException(PluginHostAccessFailure failure)
        : base($"The host refused the request: {failure}.")
    {
        Failure = failure;
    }

    /// <summary>Gets why the host refused.</summary>
    public PluginHostAccessFailure Failure { get; }
}
