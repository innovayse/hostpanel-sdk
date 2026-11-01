namespace Innovayse.SDK.Plugins.Endpoints;

/// <summary>
/// Contract for a plugin that answers HTTP-shaped requests (<see cref="Enums.PluginType.App"/>). The host receives the
/// request, matches it against the routes the manifest declares, authorises the caller and only then hands the plugin a
/// <see cref="PluginCall"/>. The plugin never sees the HTTP request or a token. In the client area it sees no host client
/// or user id either: it works with the opaque references in <see cref="PluginCall.Caller"/> and the call-bound
/// services on the call. An admin call also carries the host ids of the page's records in <see cref="PluginCall.Context"/>.
/// </summary>
public interface IPluginEndpoints
{
    /// <summary>Answers one call.</summary>
    /// <param name="call">The matched, authorised call.</param>
    /// <param name="ct">
    /// Cancelled when the caller goes away or the host gives up waiting. It covers this method only: a streamed reply
    /// is cancelled through its enumerator (see <see cref="PluginReply.Stream"/>).
    /// </param>
    /// <returns>
    /// The reply. Return <see cref="PluginReply.Problem"/> for an expected failure; an exception is reported to the
    /// caller as a plugin failure without its detail.
    /// </returns>
    Task<PluginReply> HandleAsync(PluginCall call, CancellationToken ct);
}
