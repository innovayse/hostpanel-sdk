namespace Hostpanel.Sdk.Plugins.Manifest;

/// <summary>
/// The capability names a plugin may list in the <c>capabilities</c> field of its manifest. The list lets the
/// admin and the client portal show what a plugin can do without activating it; it must agree with the
/// interfaces the plugin class really implements.
/// </summary>
public static class PluginCapabilities
{
    /// <summary>
    /// The payment plugin also implements <see cref="Hostpanel.Sdk.Plugins.Payment.IPaymentReturnPlugin"/>, which
    /// completes a payment from the parameters of the return request.
    /// </summary>
    public const string PaymentReturn = "paymentReturn";

    /// <summary>The payment plugin also implements <see cref="Hostpanel.Sdk.Plugins.StoredCredentials.IStoredCredentialPlugin"/>.</summary>
    public const string StoredCredentials = "storedCredentials";

    /// <summary>The provisioning plugin also implements <see cref="Hostpanel.Sdk.Plugins.Provisioning.IProvisioningPanelPlugin"/>, the account operations of a hosting control panel.</summary>
    public const string ProvisioningPanel = "provisioningPanel";

    /// <summary>
    /// The provisioning plugin also implements <see cref="Hostpanel.Sdk.Plugins.Provisioning.IProvisioningPanelLoginPlugin"/>, which tells the host
    /// how a client signs in to an account. It needs <see cref="ProvisioningPanel"/> as well.
    /// </summary>
    public const string ProvisioningPanelLogin = "provisioningPanelLogin";

    /// <summary>
    /// The provisioning plugin also implements <see cref="Hostpanel.Sdk.Plugins.Provisioning.IProvisioningPanelPackagesPlugin"/>, which lists
    /// the packages a server offers with their limits. It needs <see cref="ProvisioningPanel"/> as well.
    /// </summary>
    public const string ProvisioningPanelPackages = "provisioningPanelPackages";

    /// <summary>
    /// The provisioning plugin also implements <see cref="Hostpanel.Sdk.Plugins.VirtualServers.IVirtualServerPlugin"/>, the
    /// operations of a virtual server or cloud platform. The plugin type stays <see cref="Hostpanel.Sdk.Plugins.Enums.PluginType.Provisioning"/>:
    /// a virtual server is a service provisioned on a server the host knows, like a hosting account.
    /// </summary>
    public const string VirtualServer = "virtualServer";

    /// <summary>The registrar plugin also implements <see cref="Hostpanel.Sdk.Plugins.Registrar.IRegistrarOperationsPlugin"/>, the domain operations of a registrar.</summary>
    public const string RegistrarOperations = "registrarOperations";

    /// <summary>The registrar plugin also implements <see cref="Hostpanel.Sdk.Plugins.Registrar.IRegistrarEmailForwardingPlugin"/>, e-mail forwarding for its domains.</summary>
    public const string RegistrarEmailForwarding = "registrarEmailForwarding";

    /// <summary>The registrar plugin also implements <see cref="Hostpanel.Sdk.Plugins.Registrar.IRegistrarDnsManagementPlugin"/>, the DNS management switch.</summary>
    public const string RegistrarDnsManagement = "registrarDnsManagement";

    /// <summary>The registrar plugin also implements <see cref="Hostpanel.Sdk.Plugins.Registrar.IRegistrarWhoisPlugin"/>, the WHOIS lookup.</summary>
    public const string RegistrarWhois = "registrarWhois";

    /// <summary>The registrar plugin also implements <see cref="Hostpanel.Sdk.Plugins.Registrar.IRegistrarChildNameserversPlugin"/>, the child nameserver hosts of a domain.</summary>
    public const string RegistrarChildNameservers = "registrarChildNameservers";

    /// <summary>The registrar plugin also implements <see cref="Hostpanel.Sdk.Plugins.Registrar.IRegistrarDnssecPlugin"/>, the DNSSEC delegation signer records of a domain.</summary>
    public const string RegistrarDnssec = "registrarDnssec";

    /// <summary>The registrar plugin also implements <see cref="Hostpanel.Sdk.Plugins.Registrar.IRegistrarRestorePlugin"/>, the restore of a domain from redemption.</summary>
    public const string RegistrarRestore = "registrarRestore";

    /// <summary>The registrar plugin also implements <see cref="Hostpanel.Sdk.Plugins.Registrar.IRegistrarDomainListPlugin"/>, the listing of the domains an account holds, for import.</summary>
    public const string RegistrarDomainList = "registrarDomainList";

    /// <summary>The registrar plugin also implements <see cref="Hostpanel.Sdk.Plugins.Registrar.IRegistrarSuggestionsPlugin"/>, domain name suggestions.</summary>
    public const string RegistrarSuggestions = "registrarSuggestions";

    /// <summary>The registrar plugin also implements <see cref="Hostpanel.Sdk.Plugins.Registrar.IRegistrarBalancePlugin"/>, the balance of the registrar account.</summary>
    public const string RegistrarBalance = "registrarBalance";

    /// <summary>The virtual server plugin also implements <see cref="Hostpanel.Sdk.Plugins.VirtualServers.IVirtualServerListPlugin"/>, the listing of the servers a platform holds, for reconciliation.</summary>
    public const string VirtualServerList = "virtualServerList";

    /// <summary>
    /// The virtual server plugin also implements <see cref="Hostpanel.Sdk.Plugins.VirtualServers.IVirtualServerSnapshotPlugin"/>,
    /// the snapshots of a server. It needs <see cref="VirtualServer"/> as well.
    /// </summary>
    public const string VirtualServerSnapshots = "virtualServerSnapshots";

    /// <summary>
    /// The virtual server plugin also implements <see cref="Hostpanel.Sdk.Plugins.VirtualServers.IVirtualServerBackupPlugin"/>,
    /// the backups of a server. It needs <see cref="VirtualServer"/> as well.
    /// </summary>
    public const string VirtualServerBackups = "virtualServerBackups";

    /// <summary>
    /// The virtual server plugin also implements <see cref="Hostpanel.Sdk.Plugins.VirtualServers.IVirtualServerFirewallPlugin"/>,
    /// the firewall rules of a server. It needs <see cref="VirtualServer"/> as well.
    /// </summary>
    public const string VirtualServerFirewall = "virtualServerFirewall";

    /// <summary>
    /// The virtual server plugin promises that <see cref="Hostpanel.Sdk.Plugins.VirtualServers.IVirtualServerPlugin.CreateConsoleSessionAsync"/>
    /// is supported, so the host shows the console action up front. A plugin that does not declare it may still answer
    /// <see cref="Hostpanel.Sdk.Plugins.Enums.PanelOperationOutcome.NotSupported"/> when the call is made. It needs
    /// <see cref="VirtualServer"/> as well.
    /// </summary>
    public const string VirtualServerConsole = "virtualServerConsole";

    /// <summary>
    /// Any plugin type may declare it. It means the plugin class also implements
    /// <see cref="Hostpanel.Sdk.Plugins.Endpoints.IPluginEndpoints"/>, so the host serves the plugin's declared
    /// <c>api</c> routes and its <c>ui</c> pages, menus and slots. An <see cref="Hostpanel.Sdk.Plugins.Enums.PluginType.App"/>
    /// plugin is served without declaring it, and for it the host opens the client and admin routes, the pages and the
    /// slots. For any other plugin type the capability currently opens only the public callback routes
    /// (<c>api.callbacks</c>); its client and admin routes, pages and slots are not served yet.
    /// </summary>
    public const string Endpoints = "endpoints";

    /// <summary>
    /// Any plugin type may declare it. It means the plugin class also implements
    /// <see cref="Hostpanel.Sdk.Plugins.Tax.ITaxPlugin"/>, so the host can ask it to calculate the tax of an invoice. A plugin
    /// of type <see cref="Hostpanel.Sdk.Plugins.Enums.PluginType.Tax"/> is a tax plugin without declaring it; the capability is
    /// for a plugin of another type that also offers tax, for example a payment gateway whose provider calculates tax.
    /// </summary>
    public const string Tax = "tax";
}
