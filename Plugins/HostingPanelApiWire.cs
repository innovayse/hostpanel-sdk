using System.Text.Json.Serialization;

namespace Innovayse.SDK.Plugins;

/// <summary>The JSON envelope a hosting panel's REST API answers account calls with.</summary>
public sealed class HostingPanelApiWire
{
    /// <summary>The <see cref="Status"/> value a panel returns on success.</summary>
    public const string OkStatus = "OK";

    /// <summary>The <see cref="Status"/> value this envelope carries for a failure.</summary>
    public const string ErrorStatus = "Error";

    /// <summary>Gets or sets the result status: "OK" on success, "Error" on failure.</summary>
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    /// <summary>Gets or sets the human-readable message returned by the panel.</summary>
    [JsonPropertyName("msj")]
    public string Message { get; set; } = string.Empty;

    /// <summary>Gets a value indicating whether <see cref="Status"/> equals "OK" (case-insensitive).</summary>
    [JsonIgnore]
    public bool IsSuccess =>
        string.Equals(Status, OkStatus, StringComparison.OrdinalIgnoreCase);

    /// <summary>Builds the envelope this client reports when the panel's answer cannot be used.</summary>
    /// <param name="message">A fixed phrase; never the panel's own text.</param>
    /// <returns>A failure envelope.</returns>
    public static HostingPanelApiWire Failure(string message) =>
        new() { Status = ErrorStatus, Message = message };
}
