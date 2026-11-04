using System.Text.Json;
using System.Text.Json.Serialization;
using Hostpanel.Sdk.Plugins.Enums;
using Hostpanel.Sdk.Plugins.Manifest;

namespace Hostpanel.Sdk.Tests.Manifest;

/// <summary>Tests of <see cref="PluginApi.Callbacks"/> and <see cref="PluginApi.CallbackProblems"/>.</summary>
public sealed class PluginApiCallbacksTests
{
    /// <summary>Reads JSON the way the host does: camelCase names and enum names.</summary>
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    /// <summary>A manifest with no <c>callbacks</c> member reads as an empty list.</summary>
    [Fact]
    public void Callbacks_Absent_IsEmpty()
    {
        new PluginApi().Callbacks.Should().BeEmpty();
        JsonSerializer.Deserialize<PluginApi>("{}", Options)!.Callbacks.Should().BeEmpty();
    }

    /// <summary>A <c>null</c> <c>callbacks</c> member reads as an empty list, never as null.</summary>
    [Fact]
    public void Callbacks_JsonNull_IsEmpty()
    {
        JsonSerializer.Deserialize<PluginApi>("{\"callbacks\":null}", Options)!.Callbacks.Should().NotBeNull().And.BeEmpty();
    }

    /// <summary>The JSON name <c>api.callbacks</c> binds to <see cref="PluginApi.Callbacks"/>, beside the other lists.</summary>
    [Fact]
    public void Callbacks_Json_BindsRoutes()
    {
        var json = "{\"client\":[{\"method\":\"Get\",\"path\":\"/a\",\"permission\":\"any\"}],"
            + "\"callbacks\":[{\"method\":\"Post\",\"path\":\"/notify\"}]}";

        var api = JsonSerializer.Deserialize<PluginApi>(json, Options)!;

        api.Callbacks.Should().ContainSingle().Which.Path.Should().Be("/notify");
        api.Callbacks[0].Method.Should().Be(PluginRouteMethod.Post);
        api.Client.Should().ContainSingle();
        api.CallbackProblems().Should().BeEmpty();
    }

    /// <summary>A callback with a permission is reported, because no signed-in caller exists.</summary>
    [Fact]
    public void CallbackProblems_Permission_IsReported()
    {
        var api = new PluginApi { Callbacks = [new PluginRoute { Method = PluginRouteMethod.Post, Path = "/n", Permission = "any" }] };

        api.CallbackProblems().Should().ContainSingle().Which.Should().Contain("permission").And.Contain("/n");
    }

    /// <summary>A streaming callback is reported, because a callback answers with one reply.</summary>
    [Fact]
    public void CallbackProblems_Streaming_IsReported()
    {
        var api = new PluginApi { Callbacks = [new PluginRoute { Method = PluginRouteMethod.Post, Path = "/n", Streaming = true }] };

        api.CallbackProblems().Should().ContainSingle().Which.Should().Contain("streaming");
    }

    /// <summary>A callback with a rate limit is valid.</summary>
    [Fact]
    public void CallbackProblems_RateLimit_IsAllowed()
    {
        var api = new PluginApi { Callbacks = [new PluginRoute { Method = PluginRouteMethod.Post, Path = "/n", RateLimit = PluginRateLimitPolicy.Default }] };

        api.CallbackProblems().Should().BeEmpty();
    }
}
