using Hostpanel.Sdk.Plugins.Host;

namespace Hostpanel.Sdk.Tests.Fakes;

/// <summary>A host API with the payment capability that records the gateway orders a plugin asked it to read.</summary>
internal sealed class RecordingPaymentHostApi : IPluginHostPaymentApi
{
    /// <summary>Gets the gateway order ids of the requests, in the order they arrived.</summary>
    public List<string> Requested { get; } = [];

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
    public Task RequestOrderReadAsync(string gatewayOrderId, CancellationToken ct)
    {
        Requested.Add(gatewayOrderId);
        return Task.CompletedTask;
    }
}
