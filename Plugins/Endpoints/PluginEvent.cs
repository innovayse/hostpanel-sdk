using System.Text.Json;

namespace Innovayse.SDK.Plugins.Endpoints;

/// <summary>
/// One event of a streamed reply (<see cref="PluginReply.Stream"/>). The host writes it as a server-sent event, so
/// <see cref="Data"/> may span lines but never contains the binary of a file.
/// </summary>
/// <param name="Data">The event text; a JSON document is the usual content.</param>
/// <param name="Name">The event name a client listens for, or <see langword="null"/> for the default event.</param>
/// <param name="Id">The event id a client may resume from, or <see langword="null"/> for none.</param>
public sealed record PluginEvent(string Data, string? Name = null, string? Id = null)
{
    /// <summary>The camelCase JSON options <see cref="FromJson"/> writes with, the host's wire convention.</summary>
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    /// <summary>Builds an event whose data is the camelCase JSON of a value.</summary>
    /// <param name="payload">The value to serialise.</param>
    /// <param name="name">The event name, or <see langword="null"/> for the default event.</param>
    /// <returns>The event.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="payload"/> is <see langword="null"/>.</exception>
    public static PluginEvent FromJson(object payload, string? name = null)
    {
        ArgumentNullException.ThrowIfNull(payload);
        return new PluginEvent(JsonSerializer.Serialize(payload, JsonOptions), name);
    }
}
