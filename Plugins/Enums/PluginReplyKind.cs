namespace Innovayse.SDK.Plugins.Enums;

/// <summary>
/// The shape of a <see cref="Endpoints.PluginReply"/>. Values start at 1; 0 is not a reply.
/// </summary>
public enum PluginReplyKind
{
    /// <summary>One JSON document with a success status.</summary>
    Json = 1,

    /// <summary>A success with no body.</summary>
    NoContent = 2,

    /// <summary>A failure the host turns into its standard error response.</summary>
    Problem = 3,

    /// <summary>A stream of <see cref="Endpoints.PluginEvent"/> values the host forwards as server-sent events.</summary>
    Stream = 4,
}
