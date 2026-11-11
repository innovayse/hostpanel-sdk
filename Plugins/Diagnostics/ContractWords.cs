namespace Hostpanel.Sdk.Plugins.Diagnostics;

/// <summary>
/// The nouns a contract helper uses in its violation messages, so one set of checks says "account" and "panel" for the
/// provisioning contract, "domain" and "registrar" for the registrar contract and "server" and "platform" for the virtual
/// server contract. Internal: it only shapes message text.
/// </summary>
/// <param name="Target">What the caller arranges to be missing or present (account, domain, server).</param>
/// <param name="IndefiniteTarget">The target with its article, as a sentence needs it (an account, a domain, a server).</param>
/// <param name="Other">The other side the plugin talks to (panel, registrar, platform).</param>
/// <param name="Reference">The name of the reference a create or an order answers (ProvisioningRef, RegistrarRef).</param>
/// <param name="CreatedAgain">The sentence that says a repeat of a create did the work again and what it should have answered.</param>
internal sealed record ContractWords(string Target, string IndefiniteTarget, string Other, string Reference, string CreatedAgain);
