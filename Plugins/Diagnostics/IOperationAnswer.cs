using Hostpanel.Sdk.Plugins.Enums;

namespace Hostpanel.Sdk.Plugins.Diagnostics;

/// <summary>
/// The outcome and refusal reason every operation result of the plugin contracts carries, so a contract helper checks the
/// shape of any of them with one piece of code. Internal: a plugin never implements or sees it.
/// </summary>
/// <typeparam name="TReason">The refusal reason enum of the contract the result belongs to.</typeparam>
internal interface IOperationAnswer<TReason>
    where TReason : struct, Enum
{
    /// <summary>Gets how the other side answered.</summary>
    PanelOperationOutcome Outcome { get; }

    /// <summary>Gets why it refused; set exactly when <see cref="Outcome"/> is <see cref="PanelOperationOutcome.Refused"/>.</summary>
    TReason? Reason { get; }
}
