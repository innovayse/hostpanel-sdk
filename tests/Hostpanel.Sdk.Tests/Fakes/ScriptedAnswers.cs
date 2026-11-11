using Hostpanel.Sdk.Plugins.Enums;
using Hostpanel.Sdk.Plugins.Provisioning;
using Hostpanel.Sdk.Plugins.Registrar;
using Hostpanel.Sdk.Plugins.Results;

namespace Hostpanel.Sdk.Tests.Fakes;

/// <summary>Scripts that several contract tests share: a change that is done the first time and then answers not found, which breaks the idempotency promise.</summary>
internal static class ScriptedAnswers
{
    /// <summary>Answers done on the first call and not found on every later one, for a registrar change.</summary>
    /// <param name="call">The call number, starting at 1.</param>
    /// <returns>The answer of that call.</returns>
    public static OperationResult<RegistrarRefusalReason> RegistrarDoneThenNotFound(int call)
    {
        return call == 1 ? OperationResult<RegistrarRefusalReason>.Done() : OperationResult<RegistrarRefusalReason>.NotFound();
    }

    /// <summary>Answers done on the first call and not found on every later one, for a change of a panel account or a virtual server.</summary>
    /// <param name="call">The call number, starting at 1.</param>
    /// <returns>The answer of that call.</returns>
    public static PanelActionResult PanelDoneThenNotFound(int call)
    {
        return call == 1 ? PanelActionResult.Done() : PanelActionResult.NotFound();
    }
}
