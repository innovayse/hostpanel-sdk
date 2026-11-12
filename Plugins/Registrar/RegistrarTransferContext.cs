using Hostpanel.Sdk.Plugins.Diagnostics;

namespace Hostpanel.Sdk.Plugins.Registrar;

/// <summary>
/// Input for <see cref="IRegistrarOperationsPlugin.TransferAsync"/>. The host sends the same context on every retry of one
/// transfer, so a transfer the registrar already holds for the domain is the earlier attempt. <see cref="EppCode"/> is a
/// secret and is printed masked.
/// </summary>
/// <param name="Connection">The registrar account to transfer to.</param>
/// <param name="DomainName">The fully qualified domain name to transfer in.</param>
/// <param name="EppCode">The authorization code from the current registrar. Secret.</param>
/// <param name="WhoisPrivacy"><see langword="true"/> to hide the registrant's details in the public WHOIS record.</param>
/// <param name="Contact">The registrant contact.</param>
public sealed partial record RegistrarTransferContext(
    RegistrarConnection Connection,
    string DomainName,
    [property: SecretMember] string EppCode,
    bool WhoisPrivacy,
    RegistrarContact Contact);
