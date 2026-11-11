using Hostpanel.Sdk.Plugins.Enums;
using Hostpanel.Sdk.Plugins.Provisioning;
using Hostpanel.Sdk.Plugins.Registrar;
using Hostpanel.Sdk.Plugins.Results;

namespace Hostpanel.Sdk.Tests.Fakes;

/// <summary>
/// A registrar plugin that implements only <see cref="IRegistrarOperationsPlugin"/>, none of the optional interfaces. It
/// forwards every operation to a <see cref="ScriptedRegistrarPlugin"/>, which counts the calls, so a test can show that a
/// contract check skips the optional operations such a plugin does not have.
/// </summary>
internal sealed class OperationsOnlyRegistrarPlugin : IRegistrarOperationsPlugin
{
    /// <summary>Initializes a new instance of the <see cref="OperationsOnlyRegistrarPlugin"/> class.</summary>
    /// <param name="inner">The scripted plugin every operation is forwarded to.</param>
    public OperationsOnlyRegistrarPlugin(ScriptedRegistrarPlugin inner)
    {
        Inner = inner;
    }

    /// <summary>Gets the scripted plugin every operation is forwarded to; it counts the calls.</summary>
    public ScriptedRegistrarPlugin Inner { get; }

    /// <inheritdoc />
    public Task<ValueResult<RegistrarAvailability, RegistrarRefusalReason>> CheckAvailabilityAsync(RegistrarAvailabilityContext context, CancellationToken ct)
    {
        return Inner.CheckAvailabilityAsync(context, ct);
    }

    /// <inheritdoc />
    public Task<ValueResult<RegistrarOrder, RegistrarRefusalReason>> RegisterAsync(RegistrarRegisterContext context, CancellationToken ct)
    {
        return Inner.RegisterAsync(context, ct);
    }

    /// <inheritdoc />
    public Task<ValueResult<RegistrarOrder, RegistrarRefusalReason>> TransferAsync(RegistrarTransferContext context, CancellationToken ct)
    {
        return Inner.TransferAsync(context, ct);
    }

    /// <inheritdoc />
    public Task<ValueResult<RegistrarOrder, RegistrarRefusalReason>> RenewAsync(RegistrarRenewContext context, CancellationToken ct)
    {
        return Inner.RenewAsync(context, ct);
    }

    /// <inheritdoc />
    public Task<ValueResult<RegistrarOrder, RegistrarRefusalReason>> CheckDomainActiveAsync(RegistrarDomainContext context, CancellationToken ct)
    {
        return Inner.CheckDomainActiveAsync(context, ct);
    }

    /// <inheritdoc />
    public Task<OperationResult<RegistrarRefusalReason>> CancelTransferAsync(RegistrarDomainContext context, CancellationToken ct)
    {
        return Inner.CancelTransferAsync(context, ct);
    }

    /// <inheritdoc />
    public Task<OperationResult<RegistrarRefusalReason>> InitiateOutgoingTransferAsync(RegistrarDomainContext context, CancellationToken ct)
    {
        return Inner.InitiateOutgoingTransferAsync(context, ct);
    }

    /// <inheritdoc />
    public Task<ValueResult<RegistrarEppCode, RegistrarRefusalReason>> GetEppCodeAsync(RegistrarDomainContext context, CancellationToken ct)
    {
        return Inner.GetEppCodeAsync(context, ct);
    }

    /// <inheritdoc />
    public Task<OperationResult<RegistrarRefusalReason>> SetNameserversAsync(RegistrarNameserversContext context, CancellationToken ct)
    {
        return Inner.SetNameserversAsync(context, ct);
    }

    /// <inheritdoc />
    public Task<OperationResult<RegistrarRefusalReason>> SetAutoRenewAsync(RegistrarFlagContext context, CancellationToken ct)
    {
        return Inner.SetAutoRenewAsync(context, ct);
    }

    /// <inheritdoc />
    public Task<OperationResult<RegistrarRefusalReason>> SetWhoisPrivacyAsync(RegistrarFlagContext context, CancellationToken ct)
    {
        return Inner.SetWhoisPrivacyAsync(context, ct);
    }

    /// <inheritdoc />
    public Task<OperationResult<RegistrarRefusalReason>> SetRegistrarLockAsync(RegistrarFlagContext context, CancellationToken ct)
    {
        return Inner.SetRegistrarLockAsync(context, ct);
    }

    /// <inheritdoc />
    public Task<ValueResult<RegistrarContact, RegistrarRefusalReason>> GetContactAsync(RegistrarDomainContext context, CancellationToken ct)
    {
        return Inner.GetContactAsync(context, ct);
    }

    /// <inheritdoc />
    public Task<OperationResult<RegistrarRefusalReason>> SetContactAsync(RegistrarContactContext context, CancellationToken ct)
    {
        return Inner.SetContactAsync(context, ct);
    }

    /// <inheritdoc />
    public Task<ValueResult<IReadOnlyList<RegistrarDnsRecord>, RegistrarRefusalReason>> GetDnsRecordsAsync(RegistrarDomainContext context, CancellationToken ct)
    {
        return Inner.GetDnsRecordsAsync(context, ct);
    }

    /// <inheritdoc />
    public Task<OperationResult<RegistrarRefusalReason>> AddDnsRecordAsync(RegistrarDnsRecordContext context, CancellationToken ct)
    {
        return Inner.AddDnsRecordAsync(context, ct);
    }

    /// <inheritdoc />
    public Task<OperationResult<RegistrarRefusalReason>> UpdateDnsRecordAsync(RegistrarDnsRecordUpdateContext context, CancellationToken ct)
    {
        return Inner.UpdateDnsRecordAsync(context, ct);
    }

    /// <inheritdoc />
    public Task<OperationResult<RegistrarRefusalReason>> DeleteDnsRecordAsync(RegistrarDnsRecordRemovalContext context, CancellationToken ct)
    {
        return Inner.DeleteDnsRecordAsync(context, ct);
    }

    /// <inheritdoc />
    public Task<ValueResult<IReadOnlyList<RegistrarTldPricing>, RegistrarRefusalReason>> GetTldPricingAsync(RegistrarPricingContext context, CancellationToken ct)
    {
        return Inner.GetTldPricingAsync(context, ct);
    }

    /// <inheritdoc />
    public Task<PanelConnectionTestResult> TestConnectionAsync(RegistrarTestConnectionContext context, CancellationToken ct)
    {
        return Inner.TestConnectionAsync(context, ct);
    }
}
