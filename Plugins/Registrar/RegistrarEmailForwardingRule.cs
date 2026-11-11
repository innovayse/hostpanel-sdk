namespace Hostpanel.Sdk.Plugins.Registrar;

/// <summary>One e-mail forwarding rule of a domain: mail sent to the source address is delivered to the destination.</summary>
/// <param name="Source">The address or mailbox name on the domain that receives the mail, as the host stores it.</param>
/// <param name="Destination">The address the mail is forwarded to.</param>
public sealed record RegistrarEmailForwardingRule(string Source, string Destination);
