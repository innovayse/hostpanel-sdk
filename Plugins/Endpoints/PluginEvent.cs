using System.Text.Json;

namespace Innovayse.SDK.Plugins.Endpoints;

/// <summary>
/// One event of a streamed reply (<see cref="PluginReply.Stream"/>). The host writes it as a server-sent event, so
/// <see cref="Data"/> may span lines but never contains the binary of a file. <see cref="Name"/> and <see cref="Id"/>
/// become single lines of the event, so neither may contain a carriage return or a line feed; the constructor refuses
/// them. The members are read-only: a changed event is a new, validated event.
/// </summary>
public sealed record PluginEvent
{
    /// <summary>The camelCase JSON options <see cref="FromJson"/> writes with, the host's wire convention.</summary>
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    /// <summary>Initializes a new instance of the <see cref="PluginEvent"/> class.</summary>
    /// <param name="data">The event text; a JSON document is the usual content.</param>
    /// <param name="name">The event name a client listens for, or <see langword="null"/> for the default event.</param>
    /// <param name="id">The event id a client may resume from, or <see langword="null"/> for none.</param>
    /// <exception cref="ArgumentNullException"><paramref name="data"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="name"/> or <paramref name="id"/> contains a carriage return or a line feed.</exception>
    public PluginEvent(string data, string? name = null, string? id = null)
    {
        ArgumentNullException.ThrowIfNull(data);
        ThrowIfLineBreak(name, nameof(name));
        ThrowIfLineBreak(id, nameof(id));

        Data = data;
        Name = name;
        Id = id;
    }

    /// <summary>Gets the event text; may span lines.</summary>
    public string Data { get; }

    /// <summary>Gets the event name a client listens for, or <see langword="null"/> for the default event; never contains a line break.</summary>
    public string? Name { get; }

    /// <summary>Gets the event id a client may resume from, or <see langword="null"/> for none; never contains a line break.</summary>
    public string? Id { get; }

    /// <summary>Builds an event whose data is the camelCase JSON of a value.</summary>
    /// <param name="payload">The value to serialise.</param>
    /// <param name="name">The event name, or <see langword="null"/> for the default event.</param>
    /// <returns>The event.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="payload"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="name"/> contains a carriage return or a line feed.</exception>
    public static PluginEvent FromJson(object payload, string? name = null)
    {
        ArgumentNullException.ThrowIfNull(payload);
        return new PluginEvent(JsonSerializer.Serialize(payload, JsonOptions), name);
    }

    /// <summary>Refuses a value that would break the line of the server-sent event it is written on.</summary>
    /// <param name="value">The value to check; <see langword="null"/> is allowed.</param>
    /// <param name="parameterName">The parameter the value came from, for the exception.</param>
    /// <exception cref="ArgumentException"><paramref name="value"/> contains a carriage return or a line feed.</exception>
    private static void ThrowIfLineBreak(string? value, string parameterName)
    {
        if (value is not null && value.AsSpan().IndexOfAny('\r', '\n') >= 0)
        {
            throw new ArgumentException("An event name or id is one line and cannot contain a carriage return or a line feed.", parameterName);
        }
    }
}
