using Hostpanel.Sdk.Plugins.Enums;

namespace Hostpanel.Sdk.Plugins.Http;

/// <summary>
/// Thrown by <see cref="PluginHttp"/> when a call to a gateway or a panel did not end in a usable answer. It carries the
/// <see cref="Fault"/> and a fixed message, never an inner exception, a request address, a query or a header: the inner
/// message of a transport fault can contain the address the plugin called and the arguments in its query string, which a
/// log line or a result must not repeat. A plugin catches it to turn the fault into an outcome, or lets it propagate when
/// the result of the operation is unknown.
/// </summary>
public sealed class PluginHttpException : Exception
{
    /// <summary>Initializes a new instance of the <see cref="PluginHttpException"/> class.</summary>
    /// <param name="fault">What went wrong.</param>
    /// <param name="message">A fixed sentence that names the fault and holds no address or query.</param>
    public PluginHttpException(PluginHttpFault fault, string message)
        : base(message)
    {
        Fault = fault;
    }

    /// <summary>Gets what went wrong.</summary>
    public PluginHttpFault Fault { get; }
}
