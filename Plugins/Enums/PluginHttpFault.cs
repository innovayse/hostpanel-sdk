namespace Hostpanel.Sdk.Plugins.Enums;

/// <summary>
/// What went wrong in a call of <see cref="Http.PluginHttp"/>, so a plugin can answer a refusal reason or an outcome
/// without reading a message. The values start at 1 and 0 is not valid. New values are only ever appended.
/// </summary>
public enum PluginHttpFault
{
    /// <summary>The response body is larger than the limit the plugin set.</summary>
    TooLarge = 1,

    /// <summary>The response came from another host than the plugin built the request for, for example after a redirect.</summary>
    UnexpectedHost = 2,

    /// <summary>The request did not complete: a timeout, a refused or dropped connection, or a TLS error.</summary>
    Transport = 3,
}
