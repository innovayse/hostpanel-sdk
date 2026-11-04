using Hostpanel.Sdk.Plugins.Host;

namespace Hostpanel.Sdk.Tests.Fakes;

/// <summary>A host API that is never called; it only fills the required <c>Host</c> member of a test call.</summary>
internal sealed class StubHostApi : IPluginHostApi
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
}
