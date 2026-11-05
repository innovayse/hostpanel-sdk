using Hostpanel.Sdk.Packaging.Naming;

namespace Hostpanel.Sdk.Packaging.Tests.Naming;

/// <summary>Tests of <see cref="PluginAssemblyNames"/>: which assembly names a package may not carry.</summary>
public sealed class PluginAssemblyNamesTests
{
    /// <summary>The platform assemblies the tests pretend the host provides.</summary>
    private static readonly IReadOnlySet<string> Platform = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "Newtonsoft.Json", "Npgsql",
    };

    /// <summary>How the tests' pretend host names its assemblies, with one extra reserved name.</summary>
    private static readonly PluginAssemblyOptions Options = new("Acme.Host.", "Acme.Host.Plugins.")
    {
        ExtraReservedNames = new HashSet<string> { "Acme.Legacy.SDK" },
    };

    /// <summary>The host's own prefix is reserved, on its own and followed by any name.</summary>
    /// <param name="suffix">What follows the host prefix.</param>
    [Theory]
    [InlineData("Domain.dll")]
    [InlineData("Infrastructure.dll")]
    [InlineData("domain.DLL")]
    [InlineData("PluginsX.dll")]
    [InlineData("Plugins.dll")]
    public void IsReserved_HostPrefix_IsRefused(string suffix)
    {
        PluginAssemblyNames.IsReserved(Options.HostPrefix + suffix, Platform, Options).Should().BeTrue();
    }

    /// <summary>The plugin namespace is the one carve-out: an official plugin may carry its own name under it.</summary>
    [Fact]
    public void IsReserved_PluginNamespace_IsTheCarveOut()
    {
        var name = Options.PluginNamespacePrefix + "Acme.dll";

        PluginAssemblyNames.IsReserved(name, Platform, Options).Should().BeFalse();
        PluginAssemblyNames.IsReserved(name.ToUpperInvariant().Replace(".DLL", ".dll"), Platform, Options).Should().BeFalse();
    }

    /// <summary>The plugin namespace needs a name after the dot; the bare prefix stays reserved.</summary>
    [Fact]
    public void IsReserved_BarePluginNamespace_StaysReserved()
    {
        PluginAssemblyNames.IsReserved(Options.PluginNamespacePrefix.TrimEnd('.') + ".dll", Platform, Options).Should().BeTrue();
        PluginAssemblyNames.IsPluginNamespaceName(Options.PluginNamespacePrefix, Options).Should().BeFalse();
        PluginAssemblyNames.IsPluginNamespaceName(Options.PluginNamespacePrefix + "Acme", Options).Should().BeTrue();
    }

    /// <summary>The carve-out lifts the prefix only: a name the platform already provides is still refused.</summary>
    [Fact]
    public void IsReserved_PluginNamespaceNameProvidedByPlatform_IsRefused()
    {
        var provided = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { Options.PluginNamespacePrefix + "Acme" };

        PluginAssemblyNames.IsReserved(Options.PluginNamespacePrefix + "Acme.dll", provided, Options).Should().BeTrue();
    }

    /// <summary>A host with another prefix reserves its own names, not the ones of the host in the other tests.</summary>
    [Fact]
    public void IsReserved_FollowsTheHostsOwnPrefix()
    {
        var other = new PluginAssemblyOptions("Other.App.", "Other.App.Addons.");

        PluginAssemblyNames.IsReserved("Other.App.Core.dll", Platform, other).Should().BeTrue();
        PluginAssemblyNames.IsReserved("Other.App.Addons.Shop.dll", Platform, other).Should().BeFalse();
        PluginAssemblyNames.IsReserved("Acme.Host.Domain.dll", Platform, other).Should().BeFalse();
    }

    /// <summary>The SDK, this package, the host's extra names and the framework core names are reserved, whatever the case.</summary>
    /// <param name="file">The file name.</param>
    [Theory]
    [InlineData("Hostpanel.Sdk.dll")]
    [InlineData("hostpanel.sdk.DLL")]
    [InlineData("Hostpanel.Sdk.Packaging.dll")]
    [InlineData("Acme.Legacy.SDK.dll")]
    [InlineData("acme.legacy.sdk.dll")]
    [InlineData("System.dll")]
    [InlineData("mscorlib.dll")]
    [InlineData("netstandard.dll")]
    [InlineData("System.Text.Json.dll")]
    [InlineData("Microsoft.Extensions.Logging.dll")]
    [InlineData("Npgsql.dll")]
    [InlineData("newtonsoft.json.exe")]
    public void IsReserved_SdkFrameworkAndPlatformNames_AreRefused(string file)
    {
        PluginAssemblyNames.IsReserved(file, Platform, Options).Should().BeTrue();
    }

    /// <summary>A plugin's own assembly, a non-assembly file and a name that only resembles a reserved one are fine.</summary>
    /// <param name="file">The file name.</param>
    [Theory]
    [InlineData("Acme.Demo.dll")]
    [InlineData("Systematic.dll")]
    [InlineData("Hostpanel.SdkTools.dll")]
    [InlineData("Acme.HostTools.dll")]
    [InlineData("System.dll.txt")]
    [InlineData("readme.md")]
    [InlineData("plugin.json")]
    [InlineData("icon.png")]
    public void IsReserved_OtherNames_AreAccepted(string file)
    {
        PluginAssemblyNames.IsReserved(file, Platform, Options).Should().BeFalse();
    }

    /// <summary>
    /// A plugin built against the SDK's former assembly name is refused only when the host lists that name: the package
    /// holds no host or former name of its own, so the host passes it in <see cref="PluginAssemblyOptions.ExtraReservedNames"/>
    /// (the way the real host reserves <c>Innovayse.SDK</c>), in any letter case and with either extension.
    /// </summary>
    /// <param name="file">The file name.</param>
    [Theory]
    [InlineData("Innovayse.SDK.dll")]
    [InlineData("innovayse.sdk.DLL")]
    [InlineData("Innovayse.SDK.exe")]
    public void IsReserved_FormerSdkNameListedByTheHost_IsRefused(string file)
    {
        var options = new PluginAssemblyOptions("Acme.Host.", "Acme.Host.Plugins.")
        {
            ExtraReservedNames = new HashSet<string> { "Innovayse.SDK" },
        };

        PluginAssemblyNames.IsReserved(file, Platform, options).Should().BeTrue();
    }

    /// <summary>Pins the current behaviour: the former name is not reserved by the package itself, only by a host that lists it.</summary>
    [Fact]
    public void IsReserved_FormerSdkNameNotListed_IsNotReservedByThePackage()
    {
        PluginAssemblyNames.IsReserved("Innovayse.SDK.dll", Platform, Options).Should().BeFalse();
    }
}
