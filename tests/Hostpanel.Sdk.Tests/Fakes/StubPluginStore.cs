using Hostpanel.Sdk.Plugins.Store;

namespace Hostpanel.Sdk.Tests.Fakes;

/// <summary>A plugin store that is never called; it only fills the required <c>Store</c> member of a test call.</summary>
internal sealed class StubPluginStore : IPluginStore
{
    /// <inheritdoc />
    public Task<string?> GetAsync(string key, CancellationToken ct)
    {
        throw new NotSupportedException();
    }

    /// <inheritdoc />
    public Task SetAsync(string key, string value, CancellationToken ct)
    {
        throw new NotSupportedException();
    }

    /// <inheritdoc />
    public Task<bool> RemoveAsync(string key, CancellationToken ct)
    {
        throw new NotSupportedException();
    }
}
