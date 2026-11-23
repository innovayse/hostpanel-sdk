using Hostpanel.Sdk.Plugins.Host;

namespace Hostpanel.Sdk.Tests.Fakes;

/// <summary>A host API with the source restricted SSH capability that records what it was asked and is never otherwise called.</summary>
internal sealed class StubSshSourceApi : IPluginHostSshSourceApi
{
    /// <summary>Gets the sources of the last issue call, or null before any.</summary>
    public IReadOnlyList<string>? LastAllowedFrom { get; private set; }

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
    public Task<PluginSshAccess> IssueSshAccessAsync(
        int serviceId,
        string label,
        IReadOnlyList<string> allowedFrom,
        CancellationToken ct)
    {
        LastAllowedFrom = allowedFrom;
        return Task.FromResult(new PluginSshAccess(Guid.NewGuid(), "ssh.example.test", 22, "acct", null, "key", null));
    }

    /// <inheritdoc />
    public Task RevokeSshAccessAsync(int serviceId, Guid keyId, CancellationToken ct)
    {
        throw new NotSupportedException();
    }
}
