using System.Text;
using Hostpanel.Sdk.Plugins.Diagnostics;

namespace Hostpanel.Sdk.Plugins.Provisioning;

/// <summary>Input for <see cref="IProvisioningPanelPlugin.ChangePasswordAsync"/>. <see cref="NewPassword"/> is a secret and is printed masked.</summary>
/// <param name="Server">The server the account lives on.</param>
/// <param name="Service">The service and its account; <see cref="PanelService.ProvisioningRef"/> is set.</param>
/// <param name="NewPassword">The password to set. Secret.</param>
public sealed record PanelChangePasswordContext(PanelServerConnection Server, PanelService Service, string NewPassword)
{
    /// <summary>Prints the record without its secret members, so the generated <c>ToString</c> never reveals them.</summary>
    /// <param name="builder">The builder the generated <c>ToString</c> is assembling.</param>
    /// <returns>Always <see langword="true"/>: members are written.</returns>
    private bool PrintMembers(StringBuilder builder)
    {
        builder.Append("Server = ").Append(Server);
        builder.Append(", Service = ").Append(Service);
        builder.Append(", NewPassword = ").Append(SecretMask.Text);
        return true;
    }
}
