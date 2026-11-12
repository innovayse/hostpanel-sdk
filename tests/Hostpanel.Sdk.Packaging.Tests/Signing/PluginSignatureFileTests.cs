using System.Text.Json;
using Hostpanel.Sdk.Packaging.Signing;
using Hostpanel.Sdk.Packaging.Tests.Support;

namespace Hostpanel.Sdk.Packaging.Tests.Signing;

/// <summary>Tests of <see cref="PluginSignatureFile"/>.</summary>
public sealed class PluginSignatureFileTests
{
    /// <summary>A valid 64-byte signature used by the tests.</summary>
    private static readonly byte[] Signature = Enumerable.Range(0, 64).Select(i => (byte)i).ToArray();

    /// <summary>The file writes exactly two members, camelCase, ending in a line feed, and reads back what it wrote.</summary>
    [Fact]
    public void ToJson_RoundTripsThroughParse()
    {
        var file = new PluginSignatureFile(DemoKey.Id, Signature);

        var json = file.ToJson();

        json.Should().EndWith("\n");
        using var document = JsonDocument.Parse(json);
        document.RootElement.EnumerateObject().Select(member => member.Name).Should().Equal("keyId", "signature");
        PluginSignatureFile.Parse(json).Should().BeEquivalentTo(file);
    }

    /// <summary>The base64 is written as it is: a plus sign stays a plus sign, and is not escaped.</summary>
    [Fact]
    public void ToJson_WritesBase64Unescaped()
    {
        var bytes = Enumerable.Repeat((byte)0xFB, 64).ToArray();

        new PluginSignatureFile(DemoKey.Id, bytes).ToJson().Should().Contain("+").And.NotContain("\\u002B");
    }

    /// <summary>A signature that is not 64 bytes is not a signature file.</summary>
    [Fact]
    public void Parse_ShortSignature_ThrowsFormatException()
    {
        var json = new PluginSignatureFile(DemoKey.Id, Signature).ToJson().Replace(Convert.ToBase64String(Signature), "AAAA");

        var act = () => PluginSignatureFile.Parse(json);

        act.Should().Throw<FormatException>();
    }

    /// <summary>A key id outside the allowed shape is not a signature file.</summary>
    [Fact]
    public void Parse_InvalidKeyId_ThrowsFormatException()
    {
        var json = "{\"keyId\":\"Not Valid\",\"signature\":\"" + Convert.ToBase64String(Signature) + "\"}";

        var act = () => PluginSignatureFile.Parse(json);

        act.Should().Throw<FormatException>();
    }
}
