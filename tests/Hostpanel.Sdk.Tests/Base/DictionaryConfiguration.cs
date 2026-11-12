using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;

namespace Hostpanel.Sdk.Tests.Base;

/// <summary>A configuration backed by a dictionary, for the tests of the base classes: only the indexer is used.</summary>
internal sealed class DictionaryConfiguration : IConfiguration
{
    /// <summary>The values, by full key.</summary>
    private readonly Dictionary<string, string?> _values;

    /// <summary>Initializes a new instance of the <see cref="DictionaryConfiguration"/> class.</summary>
    /// <param name="values">The values, by full key.</param>
    internal DictionaryConfiguration(Dictionary<string, string?> values)
    {
        _values = values;
    }

    /// <inheritdoc/>
    public string? this[string key]
    {
        get => _values.GetValueOrDefault(key);
        set => _values[key] = value;
    }

    /// <inheritdoc/>
    public IEnumerable<IConfigurationSection> GetChildren()
    {
        throw new NotSupportedException();
    }

    /// <inheritdoc/>
    public IChangeToken GetReloadToken()
    {
        throw new NotSupportedException();
    }

    /// <inheritdoc/>
    public IConfigurationSection GetSection(string key)
    {
        throw new NotSupportedException();
    }
}
