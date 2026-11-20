using Hostpanel.Sdk.Plugins.Host;
using Hostpanel.Sdk.Plugins.Provisioning;

namespace Hostpanel.Sdk.Tests.Fakes;

/// <summary>A host API with the service connection capability that answers one fixed context and records the service it was asked for.</summary>
internal sealed class StubServiceConnectionApi : IPluginHostServiceConnectionApi
{
    /// <summary>The context every resolve answers.</summary>
    private readonly PanelAccountContext _answer;

    /// <summary>Initializes the stub.</summary>
    /// <param name="answer">The context every resolve answers.</param>
    public StubServiceConnectionApi(PanelAccountContext answer)
    {
        _answer = answer;
    }

    /// <summary>Gets the service id of the last resolve, or <see langword="null"/> before the first.</summary>
    public int? LastServiceId { get; private set; }

    /// <inheritdoc />
    public Task<PanelAccountContext> ResolveServiceConnectionAsync(int serviceId, CancellationToken ct)
    {
        LastServiceId = serviceId;
        return Task.FromResult(_answer);
    }

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
