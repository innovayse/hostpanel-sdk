using Hostpanel.Sdk.Tests.Fakes;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Hostpanel.Sdk.Tests.Base;

/// <summary>Tests of the settings access that the payment, provisioning and registrar base classes share.</summary>
public sealed class BaseClassSettingsTests
{
    /// <summary>The settings of two plugins, under the standard keys.</summary>
    private static readonly Dictionary<string, string?> Values = new()
    {
        ["integration:demo:apiKey"] = "key-1",
        ["integration:other:apiKey"] = "key-2",
    };

    /// <summary>Builds each kind of base class.</summary>
    /// <returns>One factory per base class, taking the plugin id, configuration and logger.</returns>
    public static IEnumerable<object[]> Bases()
    {
        yield return [(Func<string, IConfiguration, ILogger, ISettingsProbe>)((id, config, logger) => new PaymentSettingsProbe(id, config, logger))];
        yield return [(Func<string, IConfiguration, ILogger, ISettingsProbe>)((id, config, logger) => new ProvisioningSettingsProbe(id, config, logger))];
        yield return [(Func<string, IConfiguration, ILogger, ISettingsProbe>)((id, config, logger) => new RegistrarSettingsProbe(id, config, logger))];
    }

    /// <summary>A setting is read under <c>integration:{pluginId}:{key}</c>, each plugin reading only its own.</summary>
    /// <param name="create">Builds the base class under test.</param>
    [Theory]
    [MemberData(nameof(Bases))]
    public void GetConfig_ReadsTheKeyOfThePluginId(Func<string, IConfiguration, ILogger, ISettingsProbe> create)
    {
        var config = new DictionaryConfiguration(Values);

        create("demo", config, NullLogger.Instance).Read("apiKey").Should().Be("key-1");
        create("other", config, NullLogger.Instance).Read("apiKey").Should().Be("key-2");
        create("demo", config, NullLogger.Instance).Read("missing").Should().BeNull();
    }

    /// <summary>The constructor names the argument it refuses: a blank id, a missing id, configuration and logger.</summary>
    /// <param name="create">Builds the base class under test.</param>
    [Theory]
    [MemberData(nameof(Bases))]
    public void Constructor_RefusesEachBadArgumentByName(Func<string, IConfiguration, ILogger, ISettingsProbe> create)
    {
        var config = new DictionaryConfiguration(Values);

        var blank = () => create(" ", config, NullLogger.Instance);
        var noId = () => create(null!, config, NullLogger.Instance);
        var noConfig = () => create("demo", null!, NullLogger.Instance);
        var noLogger = () => create("demo", config, null!);

        blank.Should().ThrowExactly<ArgumentException>().WithParameterName("pluginId");
        noId.Should().ThrowExactly<ArgumentNullException>().WithParameterName("pluginId");
        noConfig.Should().ThrowExactly<ArgumentNullException>().WithParameterName("configuration");
        noLogger.Should().ThrowExactly<ArgumentNullException>().WithParameterName("logger");
    }

    /// <summary>With two bad arguments the first in the signature order is the one named: the id, then the configuration, then the logger.</summary>
    /// <param name="create">Builds the base class under test.</param>
    [Theory]
    [MemberData(nameof(Bases))]
    public void Constructor_NamesTheFirstBadArgument(Func<string, IConfiguration, ILogger, ISettingsProbe> create)
    {
        var idAndConfig = () => create(" ", null!, NullLogger.Instance);
        var configAndLogger = () => create("demo", null!, null!);

        idAndConfig.Should().ThrowExactly<ArgumentException>().WithParameterName("pluginId");
        configAndLogger.Should().ThrowExactly<ArgumentNullException>().WithParameterName("configuration");
    }

    /// <summary>A missing or blank key is refused, naming the key.</summary>
    /// <param name="create">Builds the base class under test.</param>
    [Theory]
    [MemberData(nameof(Bases))]
    public void GetConfig_RefusesAMissingOrBlankKey(Func<string, IConfiguration, ILogger, ISettingsProbe> create)
    {
        var probe = create("demo", new DictionaryConfiguration(Values), NullLogger.Instance);

        var blank = () => probe.Read(" ");
        var missing = () => probe.Read(null!);

        blank.Should().ThrowExactly<ArgumentException>().WithParameterName("key");
        missing.Should().ThrowExactly<ArgumentNullException>().WithParameterName("key");
    }
}
