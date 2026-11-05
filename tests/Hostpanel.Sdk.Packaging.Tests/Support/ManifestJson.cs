using System.Text.Json;
using System.Text.Json.Serialization;
using Hostpanel.Sdk.Plugins.Manifest;

namespace Hostpanel.Sdk.Packaging.Tests.Support;

/// <summary>Builds manifests the way a host reads them: camelCase names and enum names.</summary>
public static class ManifestJson
{
    /// <summary>The members every manifest needs, as the inside of a JSON object (no braces, no trailing comma).</summary>
    private const string Required =
        "\"id\":\"demo\",\"name\":\"Demo\",\"version\":\"1.0.0\",\"author\":\"Acme\",\"description\":\"d\","
        + "\"type\":\"App\",\"category\":\"tools\",\"entryPoint\":\"Acme.Demo.Plugin\",\"sdkVersion\":\"3.0.0\"";

    /// <summary>Reads JSON the way the host does.</summary>
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    /// <summary>Reads a manifest made of the required members plus extra members.</summary>
    /// <param name="extraMembers">More JSON members (no braces), for example <c>"manifestVersion":2</c>; empty for none.</param>
    /// <returns>The manifest.</returns>
    public static PluginManifest Read(string extraMembers = "")
    {
        var json = "{" + Required + (extraMembers.Length == 0 ? string.Empty : "," + extraMembers) + "}";
        return JsonSerializer.Deserialize<PluginManifest>(json, Options)!;
    }

    /// <summary>Reads a manifest with explicit id and version in place of the defaults.</summary>
    /// <param name="id">The plugin id.</param>
    /// <param name="version">The plugin version text.</param>
    /// <returns>The manifest.</returns>
    public static PluginManifest ReadWith(string id, string version)
    {
        var json = "{" + Required.Replace("\"id\":\"demo\"", "\"id\":\"" + id + "\"").Replace("\"version\":\"1.0.0\"", "\"version\":\"" + version + "\"") + "}";
        return JsonSerializer.Deserialize<PluginManifest>(json, Options)!;
    }
}
