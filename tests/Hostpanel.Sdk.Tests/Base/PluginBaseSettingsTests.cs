using Hostpanel.Sdk.Base;
using Hostpanel.Sdk.Plugins.Provisioning;
using Hostpanel.Sdk.Plugins.Registrar;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Hostpanel.Sdk.Tests.Base;

/// <summary>Tests of the settings access the payment, provisioning and registrar base classes share.</summary>
public sealed class PluginBaseSettingsTests
{
    /// <summary>The settings of the plugin under test, under the standard keys.</summary>
    private static readonly Dictionary<string, string?> Values = new()
    {
        ["integration:demo:apiKey"] = "key-1",
        ["integration:other:apiKey"] = "key-2",
    };

    /// <summary>Builds each kind of base class over the same configuration.</summary>
    /// <returns>One factory per base class, taking the plugin id, configuration and logger.</returns>
    public static IEnumerable<object[]> Bases()
    {
        yield return new object[] { (Func<string, IConfiguration, ILogger, IProbe>)((id, config, logger) => new PaymentProbe(id, config, logger)) };
        yield return new object[] { (Func<string, IConfiguration, ILogger, IProbe>)((id, config, logger) => new ProvisioningProbe(id, config, logger)) };
        yield return new object[] { (Func<string, IConfiguration, ILogger, IProbe>)((id, config, logger) => new RegistrarProbe(id, config, logger)) };
    }

    /// <summary>A setting is read under <c>integration:{pluginId}:{key}</c>, only for the plugin's own id.</summary>
    /// <param name="create">Builds the base class under test.</param>
    [Theory]
    [MemberData(nameof(Bases))]
    public void GetConfig_ReadsTheKeyOfThePluginId(Func<string, IConfiguration, ILogger, IProbe> create)
    {
        var probe = create("demo", new DictionaryConfiguration(Values), NullLogger.Instance);

        probe.Read("apiKey").Should().Be("key-1");
        probe.Read("missing").Should().BeNull();
    }

    /// <summary>The constructor refuses a missing or blank id, a missing configuration and a missing logger.</summary>
    /// <param name="create">Builds the base class under test.</param>
    [Theory]
    [MemberData(nameof(Bases))]
    public void Constructor_RefusesBadArguments(Func<string, IConfiguration, ILogger, IProbe> create)
    {
        var config = new DictionaryConfiguration(Values);

        var blank = () => create(" ", config, NullLogger.Instance);
        var noId = () => create(null!, config, NullLogger.Instance);
        var noConfig = () => create("demo", null!, NullLogger.Instance);
        var noLogger = () => create("demo", config, null!);

        blank.Should().Throw<ArgumentException>();
        noId.Should().Throw<ArgumentNullException>();
        noConfig.Should().Throw<ArgumentNullException>();
        noLogger.Should().Throw<ArgumentNullException>();
    }

    /// <summary>A blank key is refused.</summary>
    /// <param name="create">Builds the base class under test.</param>
    [Theory]
    [MemberData(nameof(Bases))]
    public void GetConfig_RefusesABlankKey(Func<string, IConfiguration, ILogger, IProbe> create)
    {
        var probe = create("demo", new DictionaryConfiguration(Values), NullLogger.Instance);

        var read = () => probe.Read(" ");

        read.Should().Throw<ArgumentException>();
    }

    /// <summary>What the tests need from a base class: reading a setting through the protected method.</summary>
    public interface IProbe
    {
        /// <summary>Reads a setting through the base class.</summary>
        /// <param name="key">The field key.</param>
        /// <returns>The stored value, or <see langword="null"/>.</returns>
        string? Read(string key);
    }

    /// <summary>A payment gateway base class with its settings reader opened.</summary>
    private sealed class PaymentProbe : PaymentGatewayBase, IProbe
    {
        /// <summary>Initializes a new instance of the <see cref="PaymentProbe"/> class.</summary>
        /// <param name="pluginId">The plugin's id.</param>
        /// <param name="configuration">The settings.</param>
        /// <param name="logger">The logger.</param>
        public PaymentProbe(string pluginId, IConfiguration configuration, ILogger logger)
            : base(pluginId, configuration, logger)
        {
        }

        /// <inheritdoc/>
        public string? Read(string key)
        {
            return GetConfig(key);
        }
    }

    /// <summary>A provisioning base class with its settings reader opened.</summary>
    private sealed class ProvisioningProbe : ProvisioningProviderBase, IProbe
    {
        /// <summary>Initializes a new instance of the <see cref="ProvisioningProbe"/> class.</summary>
        /// <param name="pluginId">The plugin's id.</param>
        /// <param name="configuration">The settings.</param>
        /// <param name="logger">The logger.</param>
        public ProvisioningProbe(string pluginId, IConfiguration configuration, ILogger logger)
            : base(pluginId, configuration, logger)
        {
        }

        /// <inheritdoc/>
        public string? Read(string key)
        {
            return GetConfig(key);
        }
    }

    /// <summary>A registrar base class with its settings reader opened.</summary>
    private sealed class RegistrarProbe : RegistrarProviderBase, IProbe
    {
        /// <summary>Initializes a new instance of the <see cref="RegistrarProbe"/> class.</summary>
        /// <param name="pluginId">The plugin's id.</param>
        /// <param name="configuration">The settings.</param>
        /// <param name="logger">The logger.</param>
        public RegistrarProbe(string pluginId, IConfiguration configuration, ILogger logger)
            : base(pluginId, configuration, logger)
        {
        }

        /// <inheritdoc/>
        public string? Read(string key)
        {
            return GetConfig(key);
        }
    }
}
