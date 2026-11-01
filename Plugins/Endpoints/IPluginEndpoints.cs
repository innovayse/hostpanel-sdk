namespace Innovayse.SDK.Plugins.Endpoints;

/// <summary>
/// Contract for a plugin that answers HTTP-shaped requests (<see cref="Enums.PluginType.App"/>). The host receives the
/// request, matches it against the routes the manifest declares, authorises the caller and only then hands the plugin a
/// <see cref="PluginCall"/>. The plugin never sees the HTTP request, a token or a client id of the caller: it works with
/// the opaque references in <see cref="PluginCall.Caller"/> and the call-bound services on the call.
/// </summary>
public interface IPluginEndpoints
{
    /// <summary>Answers one call.</summary>
    /// <param name="call">The matched, authorised call.</param>
    /// <param name="ct">Cancelled when the caller goes away or the host gives up waiting.</param>
    /// <returns>
    /// The reply. Return <see cref="PluginReply.Problem"/> for an expected failure; an exception is reported to the
    /// caller as a plugin failure without its detail.
    /// </returns>
    Task<PluginReply> HandleAsync(PluginCall call, CancellationToken ct);
}
