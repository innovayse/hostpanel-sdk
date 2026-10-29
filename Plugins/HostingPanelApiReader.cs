using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Innovayse.SDK.Plugins;

/// <summary>
/// The one place a hosting panel's HTTP answer is turned into a typed value, so every panel
/// provider treats an HTML page behind HTTP 200 the same way.
/// </summary>
/// <remarks>
/// These panels answer errors with HTTP 200 and an HTML body, so a status check upstream cannot
/// catch them. Nothing of such a body is logged beyond its size, because it may echo account data.
/// </remarks>
public static class HostingPanelApiReader
{
    /// <summary>
    /// Reads a response as the account-call envelope. A body that is not JSON is a
    /// <em>failure</em>, returned as an "Error" envelope with a fixed phrase, so a caller's
    /// <c>IsSuccess</c> check refuses the operation and an error page never reads as a success.
    /// </summary>
    /// <param name="response">The HTTP response message.</param>
    /// <param name="panel">The panel's display name, used in the fixed failure phrase and the log.</param>
    /// <param name="logger">Records the status and size of an unusable body.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The parsed envelope, or a failure envelope when the body is not JSON.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the response body is empty or the JSON null.</exception>
    public static async Task<HostingPanelApiWire> ReadEnvelopeAsync(
        HttpResponseMessage response,
        string panel,
        ILogger logger,
        CancellationToken ct)
    {
        var raw = await ReadBodyAsync(response, panel, ct);

        try
        {
            return JsonSerializer.Deserialize<HostingPanelApiWire>(raw)
                ?? throw new InvalidOperationException($"{panel} API returned null JSON.");
        }
        catch (JsonException ex)
        {
            LogUnusableBody(logger, panel, response, raw, ex);
            return HostingPanelApiWire.Failure($"{panel} returned a response that is not the expected JSON.");
        }
    }

    /// <summary>Reads a response as a typed JSON payload.</summary>
    /// <typeparam name="T">The payload type.</typeparam>
    /// <param name="response">The HTTP response message.</param>
    /// <param name="panel">The panel's display name, used in the exception text and the log.</param>
    /// <param name="logger">Records the status and size of an unusable body.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The parsed payload.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the body is empty, is not JSON, or is the JSON null.
    /// </exception>
    public static async Task<T> ReadPayloadAsync<T>(
        HttpResponseMessage response,
        string panel,
        ILogger logger,
        CancellationToken ct)
        where T : class
    {
        var raw = await ReadBodyAsync(response, panel, ct);

        try
        {
            return JsonSerializer.Deserialize<T>(raw)
                ?? throw new InvalidOperationException($"{panel} API returned null JSON.");
        }
        catch (JsonException ex)
        {
            LogUnusableBody(logger, panel, response, raw, ex);
            throw new InvalidOperationException($"{panel} returned a response that is not the expected JSON.", ex);
        }
    }

    /// <summary>Reads a flag a panel spells as a number, a string or a boolean.</summary>
    /// <param name="value">The JSON value holding the flag.</param>
    /// <returns>
    /// <see langword="true"/> for <c>1</c>, <c>"1"</c>, <c>"yes"</c>, <c>"true"</c> and <c>true</c>;
    /// anything else, an absent value included, is <see langword="false"/>.
    /// </returns>
    public static bool IsFlagSet(JsonElement value) => value.ValueKind switch
    {
        JsonValueKind.Number => value.TryGetInt32(out var n) && n == 1,
        JsonValueKind.String => value.GetString() is { } text
            && (text == "1"
                || text.Equals("yes", StringComparison.OrdinalIgnoreCase)
                || text.Equals("true", StringComparison.OrdinalIgnoreCase)),
        JsonValueKind.True => true,
        _ => false,
    };

    /// <summary>Reads the body as text and refuses an empty one.</summary>
    /// <param name="response">The HTTP response message.</param>
    /// <param name="panel">The panel's display name, used in the exception text.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The non-empty body.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the body is empty.</exception>
    private static async Task<string> ReadBodyAsync(HttpResponseMessage response, string panel, CancellationToken ct)
    {
        var raw = await response.Content.ReadAsStringAsync(ct);
        if (string.IsNullOrWhiteSpace(raw))
        {
            throw new InvalidOperationException($"{panel} API returned an empty response body.");
        }

        return raw;
    }

    /// <summary>Logs that a body was not JSON, with its status and size only.</summary>
    /// <param name="logger">The logger to write to.</param>
    /// <param name="panel">The panel's display name.</param>
    /// <param name="response">The response whose body was unusable.</param>
    /// <param name="raw">The body; only its length is logged.</param>
    /// <param name="ex">The parse failure.</param>
    private static void LogUnusableBody(
        ILogger logger,
        string panel,
        HttpResponseMessage response,
        string raw,
        JsonException ex)
    {
        logger.LogError(
            ex,
            "{Panel} API returned a non-JSON response body. Status {Status}, body length {Length}.",
            panel,
            (int)response.StatusCode,
            raw.Length);
    }
}
