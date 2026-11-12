using Hostpanel.Sdk.Plugins.Diagnostics;
using Hostpanel.Sdk.Plugins.Registrar;

namespace Hostpanel.Sdk.Tests.Diagnostics;

/// <summary>Tests that pin the public shape of <see cref="SecretMask"/>.</summary>
public sealed class SecretMaskTests
{
    /// <summary>The type is public, static and in the diagnostics namespace, so a plugin can use it.</summary>
    [Fact]
    public void SecretMask_IsPublicAndStaticInTheDiagnosticsNamespace()
    {
        var type = typeof(SecretMask);

        type.IsPublic.Should().BeTrue();
        (type.IsAbstract && type.IsSealed).Should().BeTrue("a static class is abstract and sealed");
        type.Namespace.Should().Be("Hostpanel.Sdk.Plugins.Diagnostics");
        type.Name.Should().Be("SecretMask");
    }

    /// <summary>
    /// The text is a public constant with the value the records print, so a change is a deliberate break.
    /// </summary>
    [Fact]
    public void Text_IsAPublicConstantWithTheMask()
    {
        var field = typeof(SecretMask).GetField(nameof(SecretMask.Text));

        field.Should().NotBeNull();
        field!.IsLiteral.Should().BeTrue();
        field.IsPublic.Should().BeTrue();
        SecretMask.Text.Should().Be("***");
    }

    /// <summary>An SDK record prints the very text a plugin can now use, so the two cannot drift apart.</summary>
    [Fact]
    public void SdkRecord_PrintsTheMask()
    {
        var connection = new RegistrarConnection("account", "key-value", null, null, true);

        connection.ToString().Should().Contain("ApiKey = " + SecretMask.Text);
    }
}
