using Hostpanel.Sdk.Plugins.Host;

namespace Hostpanel.Sdk.Tests.Fakes;

/// <summary>A host API with the hosting capability that is never called; it only stands for a host that implements <see cref="IPluginHostHostingApi"/>.</summary>
internal sealed class StubHostingApi : IPluginHostHostingApi
{
    /// <inheritdoc />
    public Task<PluginClientProfile?> GetProfileAsync(CancellationToken ct)
    {
        throw new NotSupportedException();
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<PluginClientService>> ListServicesAsync(CancellationToken ct)
    {
        throw new NotSupportedException();
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<PluginClientInvoice>> ListInvoicesAsync(CancellationToken ct)
    {
        throw new NotSupportedException();
    }

    /// <inheritdoc />
    public Task<PluginSshAccess> IssueSshAccessAsync(int serviceId, string label, CancellationToken ct)
    {
        throw new NotSupportedException();
    }

    /// <inheritdoc />
    public Task RevokeSshAccessAsync(int serviceId, Guid keyId, CancellationToken ct)
    {
        throw new NotSupportedException();
    }

    /// <inheritdoc />
    public Task<PluginHostingCapabilities> GetHostingCapabilitiesAsync(int serviceId, CancellationToken ct)
    {
        throw new NotSupportedException();
    }

    /// <inheritdoc />
    public Task<PluginWebsite> EnsureWebsiteAsync(int serviceId, PluginWebsiteSpec spec, CancellationToken ct)
    {
        throw new NotSupportedException();
    }

    /// <inheritdoc />
    public Task RemoveWebsiteAsync(int serviceId, Guid websiteId, CancellationToken ct)
    {
        throw new NotSupportedException();
    }

    /// <inheritdoc />
    public Task<PluginDatabase> EnsureDatabaseAsync(int serviceId, string label, CancellationToken ct)
    {
        throw new NotSupportedException();
    }

    /// <inheritdoc />
    public Task RemoveDatabaseAsync(int serviceId, Guid databaseId, CancellationToken ct)
    {
        throw new NotSupportedException();
    }
}
