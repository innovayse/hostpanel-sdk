using Hostpanel.Sdk.Plugins.Endpoints;
using Hostpanel.Sdk.Plugins.Enums;
using Hostpanel.Sdk.Tests.Fakes;

namespace Hostpanel.Sdk.Tests.Endpoints;

/// <summary>Tests of <see cref="PluginCall.Headers"/> and the enum values added with callbacks.</summary>
public sealed class PluginCallHeadersTests
{
    /// <summary>Builds a callback call, with the headers when given.</summary>
    /// <param name="headers">The headers to set, or <see langword="null"/> to set a null map.</param>
    /// <returns>The call.</returns>
    private static PluginCall BuildWithHeaders(IReadOnlyDictionary<string, IReadOnlyList<string>>? headers)
    {
        return new PluginCall
        {
            Area = PluginArea.Callback,
            Method = PluginRouteMethod.Post,
            Route = "/notify",
            Language = "en",
            Caller = new PluginCaller { UserRef = "none" },
            Host = new StubHostApi(),
            Store = new StubPluginStore(),
            Headers = headers!,
        };
    }

    /// <summary>A call built without headers reads as an empty map, so an existing caller needs no change.</summary>
    [Fact]
    public void Headers_NotSet_IsEmpty()
    {
        var call = new PluginCall
        {
            Area = PluginArea.Client,
            Method = PluginRouteMethod.Get,
            Route = "/x",
            Language = "en",
            Caller = new PluginCaller { UserRef = "u" },
            Host = new StubHostApi(),
            Store = new StubPluginStore(),
        };

        call.Headers.Should().NotBeNull().And.BeEmpty();
    }

    /// <summary>A null map, as a loose deserialiser may produce, reads as empty and never as null.</summary>
    [Fact]
    public void Headers_Null_IsEmpty()
    {
        BuildWithHeaders(null).Headers.Should().NotBeNull().And.BeEmpty();
    }

    /// <summary>Headers set by the host are kept, a repeated header with its values in order.</summary>
    [Fact]
    public void Headers_Set_KeepsValuesInOrder()
    {
        var call = BuildWithHeaders(new Dictionary<string, IReadOnlyList<string>> { ["X-Signature"] = ["a", "b"] });

        call.Headers["X-Signature"].Should().Equal("a", "b");
    }

    /// <summary>The new enum members take the next free values and the earlier ones are unchanged.</summary>
    [Fact]
    public void EnumValues_AreAdditive()
    {
        ((int)PluginArea.Client).Should().Be(1);
        ((int)PluginArea.Admin).Should().Be(2);
        ((int)PluginArea.Callback).Should().Be(3);
        ((int)PluginReplyKind.Stream).Should().Be(4);
        ((int)PluginReplyKind.Raw).Should().Be(5);
    }
}
