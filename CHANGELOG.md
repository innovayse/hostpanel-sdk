# Changelog

All notable changes to the `Hostpanel.Sdk` package are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this package adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [3.15.0]

### Added

- `PanelAccount.IpAddress` (`string?`, `null` by default): the IP address the account is served on, when the panel says. It is an init property and not a constructor parameter, so every existing constructor call compiles and reads as before, and a plugin built on 3.14.0 loads unchanged.
- `IProvisioningPanelAccountPlugin` in `Hostpanel.Sdk.Plugins.Provisioning`: an optional interface that extends `IProvisioningPanelPlugin`, which does not change. `GetAccountAsync(PanelAccountContext, CancellationToken)` reads one account by its reference and answers a `ValueResult<PanelAccount, PanelRefusalReason>`: `Succeeded` with the account, `NotFound` when the panel holds no such account, `NotSupported` or `Refused`. It is a read and safe to repeat. A host built on an earlier version does not call it, so a plugin that relies on it sets `minHostVersion` to the first host release that does.
- `PluginCapabilities.ProvisioningPanelAccount` (`provisioningPanelAccount`): the manifest name of the interface above. It needs `provisioningPanel` as well, like `provisioningPanelLogin` and `provisioningPanelPackages`.
- `ProvisioningContract.AssertAccountIsWellFormedAsync` for a plugin's own tests: the call answers `Succeeded` with the reference that was asked for, a defined status and, when it has one, an `IpAddress` that is an IP address, or `NotSupported`.

### Changed

- `ProvisioningContract.AssertListAccountsIsCompleteAsync` and the new check share one account check, so a listed account is now also refused when its `IpAddress` is set and is not an IP address. The property is new, so no plugin built on an earlier version can break it.

## [3.14.0]

### Added

- `PluginManifest.SshAgentProfile` (`sshAgentProfile` in `plugin.json`, `string?`, `null` by default): the id of the SSH key agent profile a panel plugin ships in its own package. It is an init property, so a manifest without it reads as before and a plugin built on 3.13.0 loads unchanged. A host built on an earlier version ignores it, so a plugin that relies on it sets `minHostVersion` to the first host release that reads it.
- `PluginSshAgentLayout` in `Hostpanel.Sdk.Plugins.Manifest`: where the files of the profile sit in a package (`ProfileFile` is `agent/profile.conf`, `HelperDirectory` is `agent/helpers`), their limits (`MaxProfileBytes` 8 KiB, `MaxHelperCount` 32, `MaxHelperBytes` 64 KiB, `MaxProfileIdLength` 32, `MaxHelperNameLength` 64), the host's own `ReservedProfile` (`generic-linux`) that a plugin cannot name, and `IsValidProfileId` and `IsValidHelperName`.
- `PluginAgentPackageChecker` in the new `Hostpanel.Sdk.Packaging.Agent` namespace of `Hostpanel.Sdk.Packaging`, with `PluginAgentCheck`, `PluginAgentFiles` and `PluginAgentViolation`, and the new `PluginAgentRule` (`ProfileMissing`, `ProfileInvalid`, `LinkNotAllowed`, `KeyNotAllowed`, `ProviderMismatch`, `BindingNotEmpty`, `PathOutsideHelperDirectory`, `HelperInvalid`, `TooManyHelpers`). `Check(packageDirectory, profileId)` reads the profile and the helpers of an unpacked package and returns the bytes it checked, or the first rule broken: the profile must be a small regular file in the agent's configuration format that sets only keys the agent defines, has `provider` equal to the profile id, leaves `binding` empty and keeps `account_check_command` and `helper_dir` inside the agent's standard helper folder; there are at most 32 helpers of at most 64 KiB with plain names; and no file or folder on the way is a link.
- `PluginManifestChecker.Validate` has two new rules in `PluginManifestRule`, appended after the existing values: `SshAgentProfileInvalid` (the id is not well formed or is `generic-linux`) and `SshAgentProfileNotAllowed` (the plugin does not declare the `provisioningPanel` capability).

### Changed

- `PluginIconChecker` and the new agent checker share one internal reader for the files of a package, so the link, size and read rules are written once. The icon rules and their detail texts do not change.

### Deprecated

- `PluginManifest.AgentProfile` and `PluginAgentProfiles` are marked `[Obsolete]` and will be removed in 4.0.0, because a plugin now ships its own profile and names it in `SshAgentProfile` instead of choosing one the SDK defines. They keep working: the member still reads, and `PluginManifestChecker` still refuses a name `PluginAgentProfiles` does not define with `AgentProfileInvalid`.

## [3.13.0]

### Added

- `PluginManifest.SdkPackageVersion` (`sdkPackageVersion` in `plugin.json`, `string?`, `null` by default) (innovayse/hostpanel#1389): the version of the `Hostpanel.Sdk` package the plugin was built against. It is an init property, so a manifest without it reads as before. It is not `sdkVersion`, which names the manifest contract and stays `1.0`.
- `build/Hostpanel.Sdk.targets` in the `Hostpanel.Sdk` package: NuGet imports it into every project that references the package, and the build writes the referenced package's version into the project's `plugin.json` as `sdkPackageVersion`, so nobody keeps it in step by hand. Only the top level member is changed, and the byte order mark, line endings and layout are kept. With several `TargetFrameworks` only the first framework's build writes the file, and a project that resolves more than one `Hostpanel.Sdk` reference fails the build. A file that is not a JSON object fails the build. The MSBuild property `HostpanelStampSdkPackageVersion` set to `false` switches it off.
- `PluginManifestChecker.Validate` has two new rules in `PluginManifestRule`: `SdkPackageVersionInvalid` (the value is not a semantic version) and `MinHostVersionBelowSdk` (`minHostVersion` is missing or lower than the first host release that ships an SDK at least as new, from the new `PluginSdkHostReleases` table). A manifest without `sdkPackageVersion`, and an SDK no host release ships yet, are not checked.

## [3.12.0]

### Added

- `IFulfilmentPlugin.OnServicePlanChangedAsync(ServicePlanChangedContext, CancellationToken)`: the host tells a fulfilment plugin that a client's service moved to another plan or changed quantity. It is a default interface method that does nothing, so a plugin built on 3.11.0 loads and compiles unchanged and is simply not told, and a plugin that fulfils plan dependent products overrides it. Delivery is at-least-once, so the plugin is idempotent on `IdempotencyKey`. A plugin that relies on it sets `minHostVersion` to the first host release that sends it.
- `ServicePlanChangedContext` in `Hostpanel.Sdk.Plugins.Fulfilment`, shaped like the other fulfilment contexts: `ServiceId`, `ClientId`, `ClientRef` (secret, prints as `***`), `Account`, `Previous` and `Current` (a `FulfilmentLine` each), `Kind`, `ChangedAt` (UTC) and `IdempotencyKey`, which stays the same on every redelivery of one change and differs for the next change of the service.
- `ServicePlanChangeKind` in `Hostpanel.Sdk.Plugins.Enums`: `Upgrade = 1`, `Downgrade = 2` and `QuantityChange = 3`. A newer host may send a kind this version does not name, so a plugin checks `Enum.IsDefined`.

## [3.11.0]

### Added

- `RefundInstruction.TransactionId` (`string?`, optional, `null` by default) (innovayse/hostpanel-sdk#95): the transaction id the plugin itself reported in `GatewayPaymentStatus.TransactionId` when the payment became paid, so a refund that needs it no longer has to read the order again or keep the id in its own store. It is `null` when the host holds none, for a payment recorded before the host kept it or one the plugin reported no id for. It is an init property, so the constructor stays the one of 3.10.0 and callers and plugins built on it load and recompile unchanged. A host built on an earlier version never fills it, so a plugin that relies on it sets `minHostVersion` to the first host release that does.
- `PluginMoney` in `Hostpanel.Sdk.Plugins.Payment` (innovayse/hostpanel-sdk#95): the money arithmetic payment plugins each carried a copy of. `TryGetMinorUnitDigits(string?, out int)` and `GetMinorUnitDigits(string)` give the ISO 4217 minor unit digits of a currency named by its numeric code (the form `IPaymentPlugin.CurrencyCode` uses) or its three letter code, in any case; `ToDecimalText(long, string)` and `ToDecimalText(long, int)` write minor units as the decimal text a gateway expects ("12.34", "1200", "-0.05"); `ToMinorUnits(string, string)` and `ToMinorUnits(string, int)` read that text back. The conversions use whole numbers only, always write a dot whatever the culture, accept fewer fraction digits than the currency has and extra ones only when they are zero, refuse anything that is not a plain decimal amount with a `FormatException` and an amount that does not fit a `long` with an `OverflowException`, and never repeat the text in the message. Currencies with no minor unit in the standard are not listed and are refused. A new static class, so no plugin or host is affected.
- `IPluginHostPaymentApi` in `Hostpanel.Sdk.Plugins.Host` (innovayse/hostpanel-sdk#95), a new interface that extends `IPluginHostApi`, which does not change, so a plugin built on 3.10.0 loads and runs unchanged. A plugin callback (a webhook or a return from the gateway's page) checks `host is IPluginHostPaymentApi` and calls `RequestOrderReadAsync(gatewayOrderId, ct)` to ask the host to read that one gateway order now, which the host does by calling `GetStatusAsync`. The host may ignore the request, for example for an unknown or settled order or when requests come too often, and never says whether it read anything, so the plugin must work without it. It has no default body, and a callback never marks a payment paid itself.
- `PaymentInstruction` gains optional init properties, all `null` by default and set with an object initializer (innovayse/hostpanel-sdk#95): `BillingCountry` (ISO 3166-1 alpha-2), `PayerName`, `PayerPhone`, `ClientTaxId`, `DueAt`, `Lines` (a list of the new `PaymentInvoiceLine(Description, Quantity, UnitAmountMinor)`) and `CallbackBaseUrl`. `PayerName`, `PayerPhone` and `ClientTaxId` are personal data and print as `***`. `CallbackBaseUrl` is the absolute public base URL of the plugin's `api.callbacks` routes, with no trailing slash, for a gateway that must be told where to notify; it sits on the instruction because `CreatePaymentAsync` is the one call with no `PluginCall` and so no host API. A plugin that reads any of them sets `minHostVersion` to the first host release that fills them.
- `SavedMethod.Kind` (an init property of the new `SavedMethodKind`: `Card = 0`, `BankAccount = 1`, `Mandate = 2`, `Wallet = 3`, default `Card`) and `SavedMethod.HasExpiry` (innovayse/hostpanel-sdk#95), for a stored method that is not a card. The expiry stays `ExpiryMonth` and `ExpiryYear`, 0 when the method does not expire. A host built on an earlier version ignores `Kind` and shows every method as a card.
- `IPluginHostServiceConnectionApi` in `Hostpanel.Sdk.Plugins.Host` (innovayse/hostpanel-sdk#95), a new interface that extends `IPluginHostApi`, which does not change. A plugin route on a service tab checks `host is IPluginHostServiceConnectionApi` and calls `ResolveServiceConnectionAsync(serviceId, ct)` to get the `PanelAccountContext` (the server connection and the provisioning reference) of one of the caller's own services. It is allowed on a client area call for a product bound to the plugin, and only when the manifest `hostAccess` declares the new kind `PluginHostAccess.ServiceConnection` (`"serviceConnection"`), which `PluginHostAccess.All` lists after `databases`; the host shows declared kinds to the operator at install, the same rule as `ssh`, `websites` and `databases`. A refusal is a `PluginHostAccessException`. The answer carries stored server credentials, so a plugin sends them only to its own panel.
- Registrar: role contacts, extended attributes and an agreement on a registration, a transfer and a contact change (innovayse/hostpanel-sdk#95). `RegistrarRegisterContext`, `RegistrarTransferContext` and `RegistrarContactContext` gain init members with defaults, so every constructor keeps its shape: `RoleContacts` (a `RegistrarRoleContacts` of administrative, technical and billing contacts, whose `AdminOr`, `TechnicalOr` and `BillingOr` fall back to the registrant), `ExtendedAttributes` (the registry specific attributes of a top level domain, printed masked), and on a registration and a transfer `Agreement` (a `RegistrarAgreement` with the end user's address, printed masked, and the moment of acceptance). `RegistrarTransferContext.Years` defaults to 1.
- Registrar: `RegistrarDnsRecord` gains `Proxied` and `Comment`, `RegistrarDnsRecordType` gains `Caa = 8`, and `RegistrarAvailability` gains `Reason` (the new `RegistrarAvailabilityReason`: `Registered`, `Reserved`, `TldNotSupported`, `InvalidName`, `Premium`, `Other`) and `PremiumPrice` (a `RegistrarPremiumPrice` with the currency, the registration price and the renewal price). All are optional with a `null` default.
- Registrar: six optional interfaces that extend `IRegistrarOperationsPlugin`, each with its own capability and a type test in the host (innovayse/hostpanel-sdk#95): `IRegistrarChildNameserversPlugin` (`registrarChildNameservers`), `IRegistrarDnssecPlugin` (`registrarDnssec`, with `RegistrarDsRecord`), `IRegistrarRestorePlugin` (`registrarRestore`), `IRegistrarDomainListPlugin` (`registrarDomainList`, paged, with `RegistrarDomainPage` and `RegistrarListedDomain`), `IRegistrarSuggestionsPlugin` (`registrarSuggestions`, with `RegistrarSuggestion`) and `IRegistrarBalancePlugin` (`registrarBalance`, with `RegistrarBalance`). `RegistrarContract` gains `AssertCreateChildNameserverIsIdempotentAsync`, `AssertUpdateChildNameserverIsIdempotentAsync`, `AssertDeleteChildNameserverIsIdempotentAsync`, `AssertSetDnssecIsIdempotentAsync`, `AssertRestoreIsIdempotentAsync` and `AssertOptionalReadsAnswerValuesAsync`.
- Provisioning panels: `PanelOperationOutcome.Pending = 6` (innovayse/hostpanel-sdk#95), for an operation the panel accepts and finishes in the background. `PanelActionResult.Pending()` and `PanelCreateResult.Pending(provisioningRef)` build it, the host repeats the same call later, and the repeat answers `Pending` until the work is done and then `Succeeded` or `AlreadyApplied`. The idempotency checks of all three contract helpers accept it.
- Provisioning panels: `PanelUsage` gains the init members `InodesUsed`, `InodesLimit`, `MailboxesUsed`, `MailboxesLimit`, `DatabasesUsed`, `DatabasesLimit`, `DomainsUsed` and `DomainsLimit`, so the constructor keeps its shape. `PanelSsoContext.TargetPath` names the page of the control panel a single sign on link opens on. `PanelCreateAccountContext.Level` and `PanelListAccountsContext.Level` carry the new `PanelAccountLevel` (`User = 1`, `Reseller = 2`); `null` means an ordinary account. `PanelChangePackageContext.ProductOptions` and `VirtualServerCreateContext.ProductOptions` carry the per product choices the host stores next to the package or plan, as a read only dictionary of strings.
- Provisioning panels: `IProvisioningPanelPackagesPlugin` (capability `provisioningPanelPackages`, which needs `provisioningPanel`) lists the packages of a server with their limits (`PanelListPackagesContext`, `PanelPackageDetails`), and `ProvisioningContract.AssertPackagesAreWellFormedAsync` checks the listing. `PanelServerAddress.BaseUrl` and `BaseUri` build the base address of a server from its saved scheme, host and port, with the IPv6 brackets, and refuse a host name that carries a path, a query, credentials or white space, so plugins no longer keep their own copy.
- Virtual servers: `VirtualServerRebuildContext.SshPublicKey` for the key to authorize after a rebuild, and `VirtualServerInstance.PasswordUnknown` for a repeated create whose generated password the host never received (the answer is `AlreadyApplied` with no `GeneratedPassword`, and the host offers a root password reset). `VirtualServerContract.AssertLostPasswordIsReportedAsync` checks that promise.
- Virtual servers: three optional interfaces that extend `IVirtualServerPlugin`, each with its own capability, which needs `virtualServer` as well (innovayse/hostpanel-sdk#95): `IVirtualServerSnapshotPlugin` (`virtualServerSnapshots`) and `IVirtualServerBackupPlugin` (`virtualServerBackups`), which share `VirtualServerRestorePoint`, `VirtualServerRestorePointContext` and `VirtualServerRestorePointCreateContext` and are idempotent on the name the host chooses, and `IVirtualServerFirewallPlugin` (`virtualServerFirewall`), which reads and replaces the whole rule set (`VirtualServerFirewallRule`, `VirtualServerFirewallContext`, `VirtualServerFirewallDirection`, `VirtualServerFirewallAction`, `VirtualServerFirewallProtocol`). `VirtualServerContract` gains `AssertCreateSnapshotIsIdempotentAsync`, `AssertDeleteSnapshotIsIdempotentAsync`, `AssertSnapshotsAreWellFormedAsync`, `AssertCreateBackupIsIdempotentAsync`, `AssertDeleteBackupIsIdempotentAsync`, `AssertBackupsAreWellFormedAsync`, `AssertSetFirewallRulesIsIdempotentAsync` and `AssertFirewallRulesAreWellFormedAsync`.
- `PluginCapabilities` gains `ProvisioningPanelPackages`, `RegistrarChildNameservers`, `RegistrarDnssec`, `RegistrarRestore`, `RegistrarDomainList`, `RegistrarSuggestions`, `RegistrarBalance`, `VirtualServerSnapshots`, `VirtualServerBackups`, `VirtualServerFirewall` and `VirtualServerConsole` (`virtualServerConsole`, which needs `virtualServer` as well). A virtual server plugin that declares the last one promises that `CreateConsoleSessionAsync` is supported, so the host shows the console action up front; a plugin without it may still answer `NotSupported` when the call is made. No member is added to an interface, so a plugin built on 3.10.0 loads unchanged.
- `RegistrarConnectionRole` in `Hostpanel.Sdk.Plugins.Enums` (`AccountName = 1`, `ApiKey = 2`, `ApiUrl = 3`, `ClientIp = 4`, `TestMode = 5`) and the optional `PluginSetting.ConnectionRole` (`connectionRole` in `plugin.json`, `null` by default) (innovayse/hostpanel-sdk#95): a registrar plugin says which member of `RegistrarConnection` each of its settings fills, so the host can build the connection from the saved settings whatever the plugin names them. It is an init property, so a manifest without it reads as before and a plugin built on 3.10.0 loads unchanged. `PluginManifestChecker` has four new rules in `PluginManifestRule`: `ConnectionRoleNotRegistrar` (a role on a plugin that is not a registrar plugin), `ConnectionRoleDuplicate` (the same role on two settings), `ConnectionRoleTypeMismatch` (`apiKey` needs a `secret` setting, `apiUrl` a `url` setting and `testMode` a `boolean` setting) and `ConnectionRolesIncomplete` (a registrar plugin that declares a role must declare both `accountName` and `apiKey`). A host built on an earlier version ignores the member, so a plugin that relies on it sets `minHostVersion` to the first host release that reads it.

### Changed

- `IPaymentPlugin.RefundAsync(string, long, CancellationToken)` now has a default body that throws `NotSupportedException` (innovayse/hostpanel-sdk#95), so a plugin that overrides `RefundAsync(RefundInstruction, CancellationToken)` no longer has to carry a method that is never called. A plugin that implements the older overload is unchanged and still called through the default of the newer one. A plugin that overrides neither refuses the refund with `NotSupportedException`. Adding a body to an existing member keeps every plugin and host built on an earlier version loading.
- All additions to existing records are init members (innovayse/hostpanel-sdk#95). The primary constructors of `PaymentInstruction`, `RefundInstruction` and `SavedMethod`, like those of the registrar, panel and virtual server records, keep exactly the parameters they had in 3.10.0, so the constructor and `Deconstruct` signatures do not change and a plugin or host built on 3.10.0 loads against 3.11.0 without a rebuild. Tests pin each constructor.
- `PanelOperationOutcome` gains `Pending = 6` (innovayse/hostpanel-sdk#95) and `RegistrarDnsRecordType` gains `Caa = 8`. Values are only appended, so the numbers of 3.10.0 are unchanged. The idempotency checks of `ProvisioningContract`, `RegistrarContract` and `VirtualServerContract` now accept `Pending` next to `Succeeded` and `AlreadyApplied`, and a create that answers `Pending` may repeat `Pending` until it answers `AlreadyApplied`; their failure messages name `Pending` too. `RegistrarContract.AssertUnknownDomainIsNotFoundAsync` also checks the child nameserver, DNSSEC and restore operations when the plugin implements them.
- `PanelTransportContract` takes the bracketing of an IPv6 host from `PanelServerAddress`, so it brackets an IPv6 literal and nothing else.

## [3.10.0]

### Added

- `PluginViews.OpenUrlFromProperty` (`"openUrlFrom"`) (innovayse/hostpanel#1333): an optional member of an action block, the name of a field of the action's JSON reply that holds an absolute URL; the host opens it in a new tab. `{ "kind": "action", "label": "actions.open", "route": { "method": "post", "path": "/services/{serviceId}/login" }, "openUrlFrom": "url" }`. It is allowed only on an action that calls a route, and not together with `navigate`. A view without it renders as before, so a plugin built on 3.9.0 loads and runs unchanged; a plugin that uses it sets `minHostVersion` to the first host release that reads it.
- `PluginViews.IsOpenUrlFieldName(string?)` and `PluginViews.MaxOpenUrlFieldLength` (64): the one check of the value, 1 to 64 ASCII letters, digits or underscores, so it is never a dotted path. The host uses it when it checks a view document.

## [3.9.0]

### Added

- Websites and databases on a client's hosting service (innovayse/hostpanel#1294). `PluginHostAccess.Websites` (`"websites"`) and `PluginHostAccess.Databases` (`"databases"`) join `Ssh`, and `PluginHostAccess.All` lists the three.
- `IPluginHostHostingApi` in `Hostpanel.Sdk.Plugins.Host`, a new interface that extends `IPluginHostApi`, which does not change, so a plugin built on 3.8.0 loads and runs unchanged. A plugin checks `host is IPluginHostHostingApi` and does without when the host lacks it. Operations: `GetHostingCapabilitiesAsync`, `EnsureWebsiteAsync`, `RemoveWebsiteAsync`, `EnsureDatabaseAsync` and `RemoveDatabaseAsync`. It has no default bodies.
- The records they use: `PluginHostingCapabilities` (`Ssh`, `Websites`, `Databases`, `PhpVersions`), `PluginWebsiteSpec` (`Hostname`, `DocumentRoot` relative to the account home, `PhpVersion`, `DocumentRootMayBeLink` for pool settings that let the document root be switched through a symbolic link), `PluginWebsite` (`Id`, `Hostname`, `DocumentRoot`, `DocumentRootPath`, `PhpVersion`) and `PluginDatabase` (`Id`, `Name`, `Username`, `Host`, `Port`, `Password`). The password of a database is returned once, when it is created, and prints as `***`.
- `PluginHostAccessFailure.InvalidRequest` (5) and `Conflict` (6), appended after the values that exist.
- Login details for a hosted account (innovayse/hostpanel-sdk#84). `IProvisioningPanelLoginPlugin` in `Hostpanel.Sdk.Plugins.Provisioning` is a new interface that extends `IProvisioningPanelPlugin`, which does not change, so a panel plugin built on 3.8.0 loads and runs unchanged and is simply not asked. The host checks `plugin is IProvisioningPanelLoginPlugin`, and the manifest lists `provisioningPanelLogin` (`PluginCapabilities.ProvisioningPanelLogin`). Its one operation, `GetLoginDetailsAsync(PanelAccountContext, CancellationToken)`, answers a `ValueResult<PanelLoginDetails, PanelRefusalReason>`: `Succeeded` with the details, `NotFound`, `NotSupported` for a panel with no sign-in page to show, or `Refused`. It is a read, so a repeat is safe and there is no `AlreadyApplied`. `PanelLoginDetails` holds `Username`, `Password` (null when the panel never reveals one, secret, prints as `***`), `Domain`, `ServerIp` and `LoginUrl`, the absolute `http` or `https` address of the sign-in page, which is not a single sign on link.
  - `ProvisioningContract.AssertLoginDetailsAreReadableAsync` (a user name, an absolute web address, no details means `NotSupported`) and `AssertUnknownAccountLoginIsNotFoundAsync` (`NotFound` or `NotSupported`) for a plugin's own tests. They throw `PluginContractViolationException`, and the message never repeats an address the plugin answered.
- `PanelServerConnection.Password` (`string?`, optional, `null` by default) (innovayse/hostpanel-sdk#84): the panel user's password, for a panel that signs in with a user name and a password. A panel that uses an access key or a token has none, and the host leaves it `null`. It is a secret and prints as `***`, and `null` when it is not set, like `AccessHash` and `ApiToken`. It is a trailing optional record parameter after `AccountId`, so callers and plugins recompile unchanged. A host that does not fill it leaves it `null`, so a plugin that reads it sets `minHostVersion` to the first host release that does. As for `AccountId`, no overload of the old constructor is kept: the host constructs the record and a plugin only reads it, so the change is source compatible, and binary compatible for a plugin that reads the record.
- An SSH key agent profile in the panel plugin manifest (innovayse/hostpanel-sdk#85). `PluginManifest.AgentProfile` (`string?`, the `agentProfile` field, `null` by default) names the profile of the host's SSH key agent for the servers of a provisioning panel plugin. `PluginAgentProfiles` in `Hostpanel.Sdk.Plugins.Manifest` defines the names, so they stay neutral and a plugin cannot invent one: `HostingPanelV7` (`"hostingPanelV7"`), `All` and `IsKnown(string?)`, which compares exactly. `PluginManifestChecker.Validate` refuses any other name with the new `PluginManifestRule.AgentProfileInvalid` (23). The member is optional and new, so a plugin built on 3.8.0 loads and runs unchanged, and a host that does not read the field ignores it; a plugin that needs it sets `minHostVersion` to the first host release that does.

## [3.8.0]

### Added

- `SecretMemberAttribute` in `Hostpanel.Sdk.Plugins.Diagnostics` and a source generator that ships in the package under `analyzers/dotnet/cs` (innovayse/hostpanel-sdk#67). A positional record marks a secret or personal member with `[property: SecretMember]` and is declared `partial`; the generator then writes the `PrintMembers` the record's generated `ToString` calls, which prints every member as the compiler would and shows `***` (`SecretMask.Text`) for each marked one, and `null` for a marked one that is not set. Nobody writes `PrintMembers` by hand, so a member added later is printed without anyone editing a list. A plugin can mark the members of its own records the same way. A record that cannot be given the code (it, or a type that encloses it, is not `partial`) fails the build with `HPSDK001` instead of printing the secret. A record that derives from another record calls the base record's method first, as the compiler's own does. The generator needs the compiler of the .NET 8 SDK or later, which a `net8.0` plugin already builds with.

### Changed

- Numbers and texts the contract helpers and the icon check wrote inline are named constants (innovayse/hostpanel-sdk#67): the default tolerance and the 100 of a percentage in `TaxContract`, the read chunk of the loopback listener, the offsets and the tag length of the icon check, the reason text of the suspend and terminate checks, the spans of the store checks, the second path of the redirect check, and the `ProvisioningRef` and `RegistrarRef` words, which are now taken with `nameof`. `TaxLimits.CountryCodeLength` (2) is new beside `CurrencyLength`. The values, the defaults and the messages are unchanged.
- `PluginReply` names the HTTP statuses it checks (innovayse/hostpanel-sdk#67) and checks the two challenges the host answers itself, 401 and 407, in one place; the accepted statuses, the defaults and the exception messages are as before, and tests pin them.
- `Hostpanel.Sdk.Packaging` states what it repeated once (innovayse/hostpanel-sdk#67): the file buffer size of the archive guard and the signature payload, the refusal of an invalid signing key id (shared by the signer and the payload), what a path inside a package looks like (forward slashes, shared by the archive guard and the payload), the message and the check for an icon that is not a regular file, the two bounds of the host version range, and the two identical catches for a malformed signature. Nothing a caller sees changes: the same exceptions, messages and answers.
- `GatewayPaymentState` and `PluginHostAccessFailure` have explicit values (innovayse/hostpanel-sdk#67), the ones they always had (0 to 2 and 0 to 4), so a reordering can never renumber them; a test pins each.
- The eight retired `Plugins/HostingPanel` types now say in their `[Obsolete]` message that 4.0.0 removes them, where they said "the next major version"; the README and the 3.1.0 note say the same.
- `PluginSigner` no longer checks that the stale temporary file exists before deleting it when signing starts, which `File.Delete` allows. The cleanup after a failed write keeps its check, so a folder that has vanished does not replace the original error.
- `Hostpanel.Sdk.Packaging` states each identifier rule once (innovayse/hostpanel-sdk#67): a page id, a literal route segment and a slot widget id share the new `PluginSegment.Pattern` (`PluginPageIdentifier.Pattern` has its value), the patterns of a menu icon name and of `TrustedKeyId` describe the shape only, and the length is checked beside them by one constant (`TrustedKeyId.MaxLength` is new and public). What each accepts is unchanged.
- `PaymentGatewayBase`, `ProvisioningProviderBase` and `RegistrarProviderBase` read their settings through one internal reader (innovayse/hostpanel-sdk#67), so the key `integration:{pluginId}:{key}` and the checks on the arguments are written once. Their constructors, `Logger`, `GetConfig` and the exceptions they throw are as before.
- The 22 records that listed their members by hand in `PrintMembers`, and `PluginSshAccess.ToString`, now use the generated code. The printed text of the members that were already masked is the same. A member that is not secret and is not set now prints as the compiler prints a null (empty), where some records wrote `null`. The records became `partial`, which changes nothing for a caller. `TaxCalculationResult` prints through the generated `ToString`: its printed form now names the type of `Lines` (as for any list) where it showed the number of lines, and adds `Total`, which no longer throws for a result built by hand with a null line (the entry adds nothing to the sum).
- These members print as `***` now, where they printed in clear: `PaymentInstruction.CustomerRef` (the documented behaviour), `CredentialCustomerContext.Email` and `Name`, `PanelClientRef.Email` and `Name`, `StoredChargeInstruction.CustomerRef` and `MethodRef`, `PluginClientProfile.Email`, and `ClientRef` of `InvoicePaidContext`, `InvoiceRefundedContext`, `RefundAdviceContext` and `SubscriptionLapsedContext`. Only the printed form changes: the values, equality and the constructors are as before, so a plugin or host built on 3.7.0 loads and runs unchanged, and one rebuilt needs no source change.

## [3.7.0]

### Added

- Tax provider contract (innovayse/hostpanel-sdk#39): `ITaxPlugin` in `Hostpanel.Sdk.Plugins.Tax`, so a tax service can ship as an external plugin and the invoice tax rate no longer has to be typed by hand. One operation, `CalculateTaxAsync(TaxCalculationRequest, CancellationToken)`, answering a `TaxCalculationResult`. A calculation is a read: it changes nothing at the service, so a repeat is safe.
  - Request: `TaxCalculationRequest` (`Currency`, `PricesIncludeTax`, `Seller`, `Buyer`, `Lines`), with `TaxParty` (`Address`, `IsBusiness`, `TaxId`), `TaxAddress` (`Country`, `Region`, `PostalCode`, `City`, `Line1`) and `TaxLine` (`LineId`, `Amount`, `Quantity`, `TaxCategory`, `IsDigitalService`). The tax numbers and the address details print as `***`; the country and the region stay readable.
  - Result: `TaxCalculationResult` with the typed outcome `TaxOutcome` (`Calculated`, `Refused`, `Unavailable`), built with `Calculated`, `Refused` and `Unavailable`. A calculation answers one `TaxLineResult` per requested line (`LineId`, `Rate` as a percentage, `Amount`, `Jurisdiction`) with `TaxJurisdiction` (`Country`, `Region`), and `Total` is the sum of the line amounts. A refusal carries a `TaxRefusalReason` (`AddressUnsupported`, `TaxIdInvalid`, `CurrencyNotSupported`, `CategoryUnknown`, `AccessDenied`, `Other`). A rate is never guessed, and a service that cannot be reached is `Unavailable`, never a calculation of zero tax.
  - `TaxContract` (for a plugin's own tests, like `RegistrarContract`): `AssertCalculationIsWellFormedAsync` (every line answered once, rates between 0 and 100, amounts not negative and matching the rate and the price mode within a tolerance), `AssertCalculationIsRepeatableAsync`, `AssertUncalculableRequestIsRefusedAsync`, `AssertUnreachableServiceIsUnavailableAsync` and `AssertHidesSecrets`. They throw `PluginContractViolationException`.
  - `PluginType.Tax` (7), appended after `App`, and `PluginCapabilities.Tax` (`"tax"`), the manifest capability a plugin of any other type lists when it also implements `ITaxPlugin`.
  - `TaxLimits` names the limits of a request, which the contract helper checks: at most 500 lines, line ids and tax numbers up to 64 characters, address fields up to 128, a three letter upper case currency and a two letter upper case country.
- `PaymentInstruction.CustomerRef` (`string?`, optional, `null` by default) (innovayse/hostpanel-sdk#36): the gateway-side customer the saved method belongs to. The host fills it with the reference it got from `IStoredCredentialPlugin.EnsureCustomerAsync` whenever `SavePaymentMethod` is `true`, so a plugin can attach the stored method to that customer and a later stored charge for the same reference finds it. A plugin that does not save methods ignores it.
- The redirect rule of the plugin HTTP clients is now a documented requirement (innovayse/hostpanel-sdk#50): a host of 3.7.0 or later must not follow redirects for any client it gives to a plugin (the client of `IPanelTransport` and any client payment plugins take), so a 3xx answer reaches the plugin as it is and the plugin decides whether the address it names is acceptable and sends a new request itself. The remarks of `IPanelTransport` state it, with the advice to send credentials only to the host the plugin built the address for and to check the final address with `PluginHttp.EnsureFinalHost` on a host that does not meet it yet. No signature changes and no option is added, because the rule holds for every client. The change is documentation and a test helper, so it is source and binary compatible.
  - `PluginHttpContract.AssertRedirectIsNotFollowedAsync(createClient)` (for a plugin's own tests): sends a request to a local listener that answers a redirect and asserts that the client returns the 3xx answer and makes no second request. It throws `PluginContractViolationException`, and tells a client that followed the redirect from a listener it could not reach, which says nothing about redirects. The host's address guard may refuse a loopback address, so use a transport double. The listener of this helper and the one of `SelfSignedListener` are one internal loopback server.
- `PluginHttp` (an instance built with a `TimeProvider`), `PluginHttpException` and `PluginHttpFault` in the new `Hostpanel.Sdk.Plugins.Http` area (innovayse/hostpanel-sdk#49), an optional helper for the HTTP code every gateway and panel plugin repeats, with no dependency beyond the framework. `SendAsync(client, request, timeout, ct)` returns once the headers arrived and bounds the call by a deadline; `ReadBoundedAsync(response, maxBytes, timeout, ct)` refuses a declared `Content-Length` over the limit before reading a byte, cuts off a body with no length at the limit (`PluginHttpFault.TooLarge`) and gives up when the deadline passes, so a slow body is bounded too (`PluginHttpFault.TimedOut`, 4). A refused or dropped connection or a TLS error becomes a `PluginHttpException` with `PluginHttpFault.Transport`, a fixed message and no inner exception, because the inner message can carry the request address and query; a cancellation by the caller's token stays an `OperationCanceledException`. The static `EnsureFinalHost(response, Uri expected)` compares scheme, host and port and refuses a response from another one (`PluginHttpFault.UnexpectedHost`); it takes a `Uri` because an IPv6 host is compared with its brackets. All of this is new types in a new area, so the change is source and binary compatible and nothing existing changes.
- `IPanelTransport.CreateClient(PanelServerConnection server)` (innovayse/hostpanel-sdk#48), a second method with a default body that returns `CreateClient()`, so a 3.7.0 host that does not implement the setting behaves as before. A host older than 3.7.0 has no such member and a plugin that calls it there gets a `MissingMethodException`, so a plugin that uses it sets `minHostVersion` to 3.7.0. A host that supports the operator setting "accept a self signed certificate" returns, for that server, a client that accepts the certificate of that one host name and port and of nothing else; it never accepts a certificate for another host or port, never disables the check for every request, and keeps every other rule of the transport. The choice belongs to the operator, and the plugin only passes the server on (the host reads the setting by `PanelServerConnection.ServerId`). A default interface method is a new member with a body, so a transport or a plugin built against 3.6.0 loads and runs without a rebuild: the change is source and binary compatible.
  - `SelfSignedListener` and `PanelTransportContract` (for a plugin's own tests): a local HTTPS listener with a self signed certificate for `localhost` that answers every request with `200 ok` and gives up on a connection after 16 KiB of headers, and the checks `AssertSelfSignedIsRefusedByDefaultAsync` and `AssertSelfSignedIsAcceptedForTheServerOnlyAsync`, which throw `PluginContractViolationException`. They call the server's own host name and port, prove that another port and another name of the same address are still refused, and tell a refusal of the certificate from any other failure. The host's address guard may refuse a loopback address, so the listener is for the tests of a plugin and not for a transport behind that guard.
- `PanelServerConnection.AccountId` (`string?`, optional, `null` by default) and `PluginManifest.ServerAccountIdLabel` (`"serverAccountIdLabel"`, with `MaxServerAccountIdLabelLength` 60) (innovayse/hostpanel-sdk#47): the organisation, reseller or tenant id that scopes every API call of a panel that needs one next to the credentials. A plugin sets `serverAccountIdLabel` in its manifest to turn on a field of the server form with that label, a literal or a text key defined in the manifest's locales, and reads the operator's value from `AccountId` instead of asking for it inside the user name. `PluginManifestChecker` reports the new rule `PluginManifestRule.AccountIdLabelInvalid` (22) for an empty, white space, longer or control character label. `AccountId` is not a secret, so it prints in `ToString`; `AccessHash` and `ApiToken` stay masked, and a missing `Username` or `AccountId` prints as `null`. The host constructs `PanelServerConnection` and a plugin only reads it, and a getter stays binary compatible, so no overload of the old constructor is kept: the change is source compatible, and binary compatible for a plugin that reads the record.
- `IPluginStoreAdvanced : IPluginStore` (innovayse/hostpanel-sdk#45), an optional interface the plugin store also implements when the host supports it: `SetAsync(key, value, timeToLive, ct)` (a value that expires; an expired key reads as absent and the host removes it), `ListKeysAsync(prefix, limit, ct)` (keys under a prefix in ascending ordinal order, never an expired one, no paging) and `SetIfAbsentAsync(key, value, timeToLive, ct)` (an atomic conditional write: exactly one of several concurrent callers gets `true`, and an existing value is never changed). A plain `IPluginStore.SetAsync` replaces the value and clears its expiry. `PluginStoreKeys` holds the rules of a key and a prefix (1 to 200 ASCII letters, digits, `.`, `-`, `_` and `:`; a prefix may be empty), which `InMemoryPluginStore` enforces. `PluginStoreLimits` names the host's limits (`MaxKeys` 1000, `MaxListLimit` 200, `MaxTimeToLive` 30 days); a new key past the quota throws `PluginStoreQuotaException`, and a time to live that is zero, negative or above the limit throws `ArgumentOutOfRangeException`, with nothing written. `IPluginStore` is unchanged, so a plugin checks `store is IPluginStoreAdvanced` and falls back to what it does today; the change is source and binary compatible.
  - `InMemoryPluginStore` (a store for a plugin's own tests, with a clock moved by `AdvanceAsync`) and `PluginStoreContract` (run against it): `AssertSetIfAbsentIsAtomicAsync`, `AssertExpiredKeyReadsAsAbsentAsync`, `AssertPlainSetClearsTimeToLiveAsync`, `AssertTimeToLiveBoundsAsync` and `AssertListKeysHonoursPrefixAndLimitAsync`. They throw `PluginContractViolationException`.
- `PluginReply.Redirect(location, status = 303)` and `PluginReplyKind.Redirect` (6) (innovayse/hostpanel-sdk#44), with the new `PluginReply.Location`: a reply that sends the caller on to another address, for a public callback route that a gateway or a browser lands on, for example the return address of a payment. The status is one of 301, 302, 303, 307 or 308. The location is a host relative path (one leading slash, never two, and never under the API path, which the host checks) or an absolute `https` URL without user info, with no control character (a line break, a tab, a null, DEL, a C1 control), no leading or trailing white space and no backslash; the factory refuses anything else and the host validates again before it answers. A plugin never writes a header itself and never builds the location from request data. The existing replies and kinds are unchanged, so the change is source and binary compatible; the new kind needs a host of 3.7.0 or later, because an older host does not know `PluginReplyKind.Redirect`.
- `IPaymentReturnPlugin : IPaymentPlugin` (innovayse/hostpanel-sdk#43), an optional interface with `CompleteAsync(gatewayOrderId, returnParameters, ct)`, for gateways that finish a payment with a value only the payer's browser has when it comes back (a payment method nonce, a payer id in the return address), and the manifest capability `PluginCapabilities.PaymentReturn` (`"paymentReturn"`) a plugin lists when it implements it. The host calls it, when the plugin implements it, with the query of the return request (and the posted fields of a browser step) before it asks for the status; it must be idempotent. `returnParameters` is an `IReadOnlyDictionary<string, string>`, so it is read only; names are compared ordinally and the first value of a repeated name wins. The host keeps within `PaymentReturnLimits` (`MaxParameters` 32, `MaxNameLength` 64, `MaxValueLength` 2048) and never passes credentials, cookies or headers. A plugin without the interface compiles and loads as before and is never asked to complete. This is a new interface and a new constant, so the change is source and binary compatible. A session kind that says the payer completes a step in the browser is not part of this change.
- `PaymentFormPost`, `PaymentFormPostLimits` and `PaymentSession.FormPost` (`PaymentFormPost?`, optional, `null` by default) (innovayse/hostpanel-sdk#42): a second kind of payment session for gateways whose hosted page is opened with a form POST. `PaymentFormPost` holds the `Action` URL and the `Fields` (names and values, in order, a name may repeat; the list is compared by reference). When `FormPost` is set the host ignores `RedirectUrl`, which stays non empty and equals the action, and renders the form itself under its own rules: only an absolute `https` action, no user info or line break, non empty field names, names and values encoded as attribute text, no script from the plugin, a button for a payer without script, and the limits in `PaymentFormPostLimits`. A session built the old way stays a plain redirect. A plugin constructs `PaymentSession`, so the two argument constructor of earlier versions is kept as an explicit overload and a plugin built against 3.6.0 loads and runs without a rebuild: the change is source and binary compatible except the two value `Deconstruct`: it gains a third value, so code that deconstructs a session into two variables no longer compiles. `RedirectUrl` is now checked: an empty or white space address, also set with a `with` expression, throws `ArgumentException`.
- `SecretMask` and `SecretMask.Text` (`"***"`) are public (innovayse/hostpanel-sdk#46), in `Hostpanel.Sdk.Plugins.Diagnostics`. It is the text the SDK records print in place of a secret, and now the one text a plugin can print in place of a secret in its own settings object or diagnostic line, so every mask in a log is the same. The value is unchanged and a test pins the name, the namespace and the value; it is a convention for readable logs and not a security boundary. Only the visibility changed, so the change is source and binary compatible.
- `PaymentInstruction.PayerEmail` (`string?`, optional, `null` by default) (innovayse/hostpanel-sdk#41): the e-mail address of the client the invoice belongs to, for gateways that refuse to create a payment without one. The host leaves it `null` when it knows no address. It is personal data, so a plugin sends it only to its own gateway and never logs it, and the generated `ToString` prints it as `***`. It is a trailing optional record parameter after `CustomerRef`, so callers and plugins recompile unchanged; a plugin built against 3.6.0 binaries keeps working only when it is rebuilt against 3.7.0, as for the other trailing members.
- `PaymentSessionKind` (`Redirect` 1, `FormPost` 2, `BrowserStep` 3) and `PaymentSession.Kind` (innovayse/hostpanel-sdk#62): one kind for how the payer is taken through the hosted step, so the form post and the browser step are not two separate members. `BrowserStep` is for a gateway whose payer completes a step in the browser (a payment method nonce, a payer id) before the payment can be charged: the host waits for the return request, calls `IPaymentReturnPlugin.CompleteAsync` with its parameters and only then reads the status, so a plugin that answers it implements `IPaymentReturnPlugin` and lists `paymentReturn`. `Kind` is a trailing optional record parameter, `Redirect` by default; the kind and the form always agree, one value stored and checked in the constructors and in both `with` accessors: a `FormPost` is required for the kind `FormPost` and allowed for no other, so a pair that contradicts, or a kind that is not defined, throws whatever the order. A `with` expression can move a session between `Redirect` and `BrowserStep` and replace a form, but moving to or from `FormPost` needs a new session. A plugin constructs `PaymentSession`, so the two argument and the three argument constructors of earlier versions are kept as explicit overloads that derive the kind (a form gives `FormPost`, none gives `Redirect`), pinned by tests: a plugin built against 3.6.0 or the earlier 3.7.0 pre-releases loads and runs without a rebuild. The change is source and binary compatible except the `Deconstruct`, which gains a fourth value. A host older than 3.7.0 does not know the kind and treats the session as a redirect.

### Compatibility

- `CustomerRef` is a trailing optional record parameter, like the 3.5.0 members: callers and plugins recompile unchanged, and the existing constructor call sites keep their meaning. The record constructor and `Deconstruct` gain a parameter, so a plugin built against 3.6.0 binaries keeps working only when it is rebuilt against 3.7.0.
- The tax contract is additive: `PluginType.Tax` is appended and no existing member changes, so a plugin built on 3.6.0 compiles and loads as it is, and the change is source and binary compatible. How a host older than 3.7.0 treats the unknown plugin type is up to that host; a host that knows it reads `plugin is ITaxPlugin`.

## [3.6.0]

### Added

- Registrar operations (innovayse/hostpanel-sdk#37): `IRegistrarOperationsPlugin : IRegistrarPlugin`, so a domain registrar can ship as an external plugin. Twenty operations in the main interface, each taking one typed context and answering one typed result: `CheckAvailabilityAsync`, `RegisterAsync`, `TransferAsync`, `RenewAsync`, `CheckDomainActiveAsync` (for an order that answered `RequiresPolling`), `CancelTransferAsync`, `InitiateOutgoingTransferAsync`, `GetEppCodeAsync`, `SetNameserversAsync`, `SetAutoRenewAsync`, `SetWhoisPrivacyAsync`, `SetRegistrarLockAsync`, `GetContactAsync`, `SetContactAsync`, `GetDnsRecordsAsync`, `AddDnsRecordAsync`, `UpdateDnsRecordAsync`, `DeleteDnsRecordAsync`, `GetTldPricingAsync` and `TestConnectionAsync`.
  - `RegistrarConnection` is the registrar account settings record (`AccountName`, `ApiKey`, `ApiUrl`, `ClientIp`, `TestMode`); `ApiKey` and `AccountName` (which may be an e-mail address) print as `***`. It arrives in every context, so a plugin keeps no state between calls.
  - Contexts: `RegistrarAvailabilityContext`, `RegistrarRegisterContext`, `RegistrarTransferContext` (the authorization code prints as `***`), `RegistrarRenewContext` (`CurrentExpiresAt` lets a plugin tell a repeat from a second renewal), `RegistrarDomainContext`, `RegistrarFlagContext`, `RegistrarNameserversContext`, `RegistrarContactContext`, `RegistrarDnsRecordContext`, `RegistrarDnsRecordUpdateContext` (the record as it is now and the content it should have), `RegistrarDnsRecordRemovalContext` (the whole record), `RegistrarPricingContext` and `RegistrarTestConnectionContext`. Values: `RegistrarDomain`, `RegistrarContact`, `RegistrarDnsRecord`, `RegistrarOrder`, `RegistrarEppCode` (prints as `***`), `RegistrarAvailability` and `RegistrarTldPricing`.
  - Results: the new shared generic `OperationResult<TReason>` (no value) and `ValueResult<TValue, TReason>` (a value), in `Hostpanel.Sdk.Plugins.Results`, with factories `Done`, `AlreadyDone`, `NotFound`, `NotSupported` and `Refused`. The registrar uses them with the new `RegistrarRefusalReason`, so there is one implementation for every contract that adds results. The outcome is the existing `PanelOperationOutcome` and the connection test answers the existing `PanelConnectionTestResult`. A result never carries registrar text or a secret, and a success always has its value. The older `PanelActionResult` is the same shape for `PanelRefusalReason`; it is left exactly as it was, because changing its type would break plugins built on earlier versions.
  - Idempotency is part of the contract and documented per operation: a retried register or transfer answers `AlreadyApplied` with the same reference, a retried renewal is recognised through `RegistrarRenewContext.CurrentExpiresAt`, a switch or a list already in the wanted state is a success, and deleting a DNS record that is gone is `AlreadyApplied`, never `NotFound`.
  - `RegistrarContract` (for a plugin's own tests, like `ProvisioningContract`): `AssertRegisterIsIdempotentAsync`, `AssertTransferIsIdempotentAsync`, `AssertRenewIsIdempotentAsync`, `AssertSetNameserversIsIdempotentAsync`, `AssertSetAutoRenewIsIdempotentAsync`, `AssertSetWhoisPrivacyIsIdempotentAsync`, `AssertSetRegistrarLockIsIdempotentAsync`, `AssertSetContactIsIdempotentAsync`, `AssertDeleteDnsRecordIsIdempotentAsync`, `AssertUnknownDomainIsNotFoundAsync`, `AssertTestConnectionAnswersAsync` and `AssertHidesSecrets`. They throw `PluginContractViolationException`.
  - `PluginCapabilities.RegistrarOperations` (`"registrarOperations"`), the manifest capability a registrar plugin lists when it implements the interface.
  - The host keeps no registrar side id for a DNS record, so an update and a removal carry the full record content and a plugin finds the record by type, host and value; `RegistrarDnsRecord.Id` is optional and used only when given. A removal of a record that is gone is `AlreadyApplied`, a removal on a domain the registrar does not hold is `NotFound`.
  - `RegistrarContact`, the registrant of `RegistrarWhois` and the addresses of `RegistrarEmailForwardingRule` and `RegistrarEmailForwardingRemovalContext` are personal data and print masked, so a context or a result that holds them does not leak them.
  - Optional operations are separate interfaces that extend `IRegistrarOperationsPlugin`, each with its own capability, and a host treats a missing one as `NotSupported`: `IRegistrarEmailForwardingPlugin` (`SetEmailForwardingAsync`, `AddEmailForwardingRuleAsync`, `UpdateEmailForwardingRuleAsync`, `DeleteEmailForwardingRuleAsync`; `registrarEmailForwarding`), `IRegistrarDnsManagementPlugin` (`SetDnsManagementAsync`; `registrarDnsManagement`) and `IRegistrarWhoisPlugin` (`GetWhoisAsync`; `registrarWhois`). New operations always arrive as new interfaces, never as members of an existing one (see CONTRIBUTING.md).
  - More `RegistrarContract` checks: `AssertAddDnsRecordIsIdempotentAsync`, `AssertUpdateDnsRecordIsIdempotentAsync`, `AssertCancelTransferIsIdempotentAsync`, `AssertInitiateOutgoingTransferIsIdempotentAsync`, `AssertSetEmailForwardingIsIdempotentAsync`, `AssertAddEmailForwardingRuleIsIdempotentAsync`, `AssertUpdateEmailForwardingRuleIsIdempotentAsync`, `AssertDeleteEmailForwardingRuleIsIdempotentAsync`, `AssertSetDnsManagementIsIdempotentAsync`, `AssertReadsAnswerValuesAsync` and `AssertWhoisAnswersAsync`; `AssertUnknownDomainIsNotFoundAsync` also covers the DNS record changes and the optional operations the plugin implements.
  - The reads that the interface documents as possibly `NotSupported` (the availability, the price list, the authorization code, the DNS records, the usage of a virtual server, the console, the image and plan lists) are accepted as `NotSupported` by the contract checks; every other read must succeed with a value.
- Virtual server operations (innovayse/hostpanel-sdk#37): `IVirtualServerPlugin : IProvisioningPlugin`, so a virtual server or cloud platform can ship as an external plugin. It is a capability of the provisioning type and not a new `PluginType` value, because a virtual server is a service provisioned on a server the host knows, like a hosting account; `PluginType` is unchanged. Fifteen operations: `CreateServerAsync`, `StartServerAsync`, `StopServerAsync`, `RebootServerAsync`, `SuspendServerAsync`, `UnsuspendServerAsync`, `TerminateServerAsync`, `RebuildServerAsync`, `ChangePlanAsync`, `ResetRootPasswordAsync`, `CreateConsoleSessionAsync`, `GetServerUsageAsync`, `ListImagesAsync`, `ListPlansAsync` and `TestServerConnectionAsync`.
  - The contract reuses the provisioning vocabulary instead of copying it: `PanelServerConnection`, `PanelService`, `PanelClientRef`, `PanelPackage` (the plan name), `PanelAccountContext`, `PanelSuspendContext`, `PanelTerminateContext`, `PanelChangePackageContext`, `PanelChangePasswordContext`, `PanelTestConnectionContext`, `PanelActionResult`, `PanelConnectionTestResult`, `PanelOperationOutcome` and `IPanelTransport`. Values are answered with the shared `ValueResult<TValue, PanelRefusalReason>`.
  - New types: `VirtualServerCreateContext` (the root password prints as `***`), `VirtualServerRebuildContext`, `VirtualServerCatalogContext`, `VirtualServerInstance`, `VirtualServerRootPassword`, `VirtualServerConsole` (the address prints as `***`), `VirtualServerUsage`, `VirtualServerImage`, `VirtualServerPlan` and the enum `VirtualServerPowerState` (`Running`, `Stopped`, `Suspended`, `Provisioning`). A platform that cannot set the requested root password generates one and answers it in `GeneratedPassword`, which prints as `***`.
  - Idempotency is documented per operation: a retried create is keyed by `VirtualServerCreateContext.Name`, which the host stores before the first call, and answers `AlreadyApplied` with the same `ProvisioningRef`; start, stop, suspend, unsuspend and change plan succeed when the server is already in the wanted state; a terminate of a server that is already gone is `AlreadyApplied`, never `NotFound`. Reboot, rebuild, root password reset and console sessions repeat their work by nature.
  - `VirtualServerContract` (for a plugin's own tests): `AssertCreateIsIdempotentAsync`, `AssertStartIsIdempotentAsync`, `AssertStopIsIdempotentAsync`, `AssertSuspendIsIdempotentAsync`, `AssertUnsuspendIsIdempotentAsync`, `AssertTerminateIsIdempotentAsync`, `AssertChangePlanIsIdempotentAsync`, `AssertUnknownServerIsNotFoundAsync`, `AssertCatalogIsWellFormedAsync`, `AssertTestConnectionAnswersAsync` and `AssertHidesSecrets`.
  - `PluginCapabilities.VirtualServer` (`"virtualServer"`), the manifest capability a plugin lists when it implements the interface. `PluginManifest.ServerModules` and `PluginServerModules` now apply to a plugin that implements `IVirtualServerPlugin` as well as one that implements `IProvisioningPanelPlugin`.
  - A start of a server the host has suspended is refused with `PanelRefusalReason.AccessDenied`. `IVirtualServerListPlugin : IVirtualServerPlugin` (capability `PluginCapabilities.VirtualServerList`, `"virtualServerList"`) is an optional separate interface with `ListServersAsync`, a paged listing for reconciliation that answers `PanelAccount` entries; a host treats a plugin without it as `NotSupported`.
  - More `VirtualServerContract` checks: `AssertSuspendedServerIsNotStartedAsync`, `AssertReadsAnswerValuesAsync` and `AssertListServersIsCompleteAsync`. `AssertUnknownServerIsNotFoundAsync` accepts only `AlreadyApplied` for the termination of an unknown server.
- `PanelRefusalReason.ImageUnknown` (8) and `PanelRefusalReason.NameInvalid` (9), appended for the virtual server contract; the existing values keep theirs.

### Changed

- The checks that `ProvisioningContract`, `RegistrarContract` and `VirtualServerContract` share (the guarded call, the outcome and reason shape, the idempotent repeat, the connection test shape, the secret scan) moved into one internal class. Their behaviour and messages are unchanged, and `ProvisioningContractMessageTests` pins the exact text of the provisioning messages.
- `ProvisioningContract.AssertCreateIsIdempotentAsync`, `AssertTestConnectionAnswersAsync` and `AssertListAccountsIsCompleteAsync` return the shared check directly instead of being `async` methods, so a null plugin or context now throws `ArgumentNullException` when the helper is called, like the other helpers of the contract already did, and no longer from the returned task. Awaiting the call behaves the same.
- The documentation of `PanelOperationOutcome`, `PanelConnectionOutcome`, `PanelActionResult`, `PanelConnectionTestResult`, `PanelServerConnection` and `IPanelTransport` now says that the registrar and virtual server contracts use them too. No member changed.

### Compatibility

Everything is additive. `IRegistrarPlugin`, `RegistrarProviderBase`, `IProvisioningPlugin`, `ProvisioningProviderBase`, `IProvisioningPanelPlugin` and `PluginType` are unchanged, so a plugin built on 3.5.0 compiles and loads on 3.6.0 as it is; it has no registrar or virtual server operations, and a host that calls them skips it (`plugin is IRegistrarOperationsPlugin` and `plugin is IVirtualServerPlugin` are false). `PanelActionResult`, `PanelCreateResult`, `PanelUsageResult` and `PanelSsoResult` now implement an internal interface, which a plugin never sees. `ProvisioningContract.AssertUnknownAccountIsNotFoundAsync` keeps accepting `Succeeded` or `AlreadyApplied` for the termination of an unknown account, exactly as in 3.5.0, because tightening it would turn the existing test suites of provisioning plugins red; `VirtualServerContract.AssertUnknownServerIsNotFoundAsync` is new and accepts only `AlreadyApplied`, as `IVirtualServerPlugin.TerminateServerAsync` documents.

### Migrating a registrar plugin to the registrar operations

1. Raise the package reference to `Hostpanel.Sdk` 3.6.0.
2. Implement `IRegistrarOperationsPlugin` on the registrar class, next to `RegistrarProviderBase`, and take an `IPanelTransport` in the constructor. The operations are required; an operation the registrar does not have answers `PanelOperationOutcome.NotSupported` instead of throwing.
3. Add `"registrarOperations"` to `capabilities` in `plugin.json`. The list must agree with the code.
4. Return outcomes, not exceptions, for a refusal, a missing domain and an unsupported operation; throw only when the result is unknown. Never put registrar text, a domain, a contact detail or a secret in a result, a log line or an exception message.
5. In the plugin's tests, call the `RegistrarContract` checks against a stub of your registrar.

### Migrating a provisioning plugin to the virtual server operations

1. Raise the package reference to `Hostpanel.Sdk` 3.6.0.
2. Implement `IVirtualServerPlugin` on the provisioning class, next to `ProvisioningProviderBase`, and take an `IPanelTransport` in the constructor. An operation the platform does not have answers `PanelOperationOutcome.NotSupported`.
3. Add `"virtualServer"` to `capabilities` and the module ids to `serverModules` in `plugin.json`.
4. In the plugin's tests, call the `VirtualServerContract` checks against a stub of your platform.

## [3.5.0]

### Added

- `PaymentInstruction.SavePaymentMethod` (`bool`, optional, `false` by default): asks the gateway to keep the payment method the payer uses on this payment so the host can charge it later. A plugin that supports it sends the gateway's own "store this method" request; a plugin that does not support it ignores the flag and the host treats the missing method as "not saved", never as a payment failure.
- `GatewayPaymentStatus.SavedMethod` (`SavedMethod?`, optional, `null` by default): the method the gateway stored for a paid payment (token reference, brand, last four digits, expiry). It reuses the existing `StoredCredentials.SavedMethod` type, so a method saved during a payment is charged the same way as one saved in a save session.

### Compatibility

- Both members are trailing optional record parameters, like `GatewayPaymentStatus.AmountMinor` before them: callers and plugins recompile unchanged. The record constructors and `Deconstruct` gain a parameter, so a plugin built against 3.4.0 binaries keeps working only when it is rebuilt against 3.5.0.

## [3.4.0]

### Added

- `PluginManifest.MinHostVersion` (`"minHostVersion"`) and `PluginManifest.MaxHostVersion` (`"maxHostVersion"`): the optional range of host versions a plugin runs on; a missing bound means no limit. `PluginManifestChecker.Validate` refuses a bound that is not a semantic version (`major.minor.patch` with an optional pre-release, read by `PluginVersion`) with `PluginManifestRule.HostVersionInvalid`, and a lower bound above the upper one with `PluginManifestRule.HostVersionRangeInverted` (innovayse/hostpanel-sdk#31).
- `PluginManifest.Changelog` (`"changelog"`): optional notes for the plugin version, at most `PluginManifest.MaxChangelogLength` = 4000 characters; a longer one breaks the new `PluginManifestRule.ChangelogTooLong` (innovayse/hostpanel-sdk#31).

## [3.3.0]

### Added

- `PluginManifest.Tags` (`"tags"`): optional keywords a plugin registry uses for discovery, empty when missing. `PluginTags` holds the rule: each tag is `PluginTags.MinLength` = 2 to `PluginTags.MaxLength` = 30 lowercase letters, digits and hyphens with no leading `#`, at most `PluginTags.MaxCount` = 10 tags. `PluginManifestChecker.Validate` refuses a bad list, or a tag listed twice, with the new `PluginManifestRule.TagsInvalid` (innovayse/hostpanel-sdk#28).

## [3.2.0]

### Added

- `PluginPage.Layout` and `PluginPageLayouts` (`standard`, `workspace`): a client page may ask for the full width, with no service side menu (innovayse/hostpanel#910).
- `PluginViews.BlockKinds` gains `split` (two panes, `PanesProperty`, `SplitPaneCount`) and `preview` (a sandboxed frame on a client page, `PreviewSourcesProperty`, `MaxPreviewSources`, `MaxPreviewSourceIdLength`, `PreviewSizeProperty`, `PreviewSizes`). The schema version stays 1. A preview shows an address only on an origin the host allows for the reader (innovayse/hostpanel#910).
- `PluginChatEvents`: the stream events a `chat` block renders apart from the reply: `step` (`StepStates`), `result` (`ResultTones`, `MaxResultActions`) and `issue`. `error` and `done` stay the host's (innovayse/hostpanel#911).

## [3.1.0]

### Added

- Provisioning panel operations (innovayse/hostpanel-sdk#16): `IProvisioningPanelPlugin : IProvisioningPlugin`, so a hosting control panel can ship as an external plugin. Ten operations, each taking one typed context and answering one typed result: `CreateAccountAsync`, `SuspendAccountAsync`, `UnsuspendAccountAsync`, `TerminateAccountAsync`, `ChangePackageAsync`, `ChangePasswordAsync`, `CreateSsoLinkAsync`, `GetUsageAsync`, `TestConnectionAsync` and `ListAccountsAsync` (paged, for reconciliation).
  - Contexts: `PanelServerConnection` (host name, address, port, user and the secrets `AccessHash` and `ApiToken`), `PanelService`, `PanelClientRef`, `PanelPackage`, and one context per operation (`PanelCreateAccountContext`, `PanelSuspendContext`, `PanelTerminateContext`, `PanelChangePackageContext`, `PanelChangePasswordContext`, `PanelSsoContext`, `PanelAccountContext`, `PanelTestConnectionContext`, `PanelListAccountsContext`). Every secret member (`AccessHash`, `ApiToken`, `Password`, `NewPassword`) prints as `***`, like `HostingPanelConnection.ApiKey`.
  - Results: `PanelCreateResult`, `PanelActionResult`, `PanelSsoResult` (the link prints masked), `PanelUsageResult` with `PanelUsage`, `PanelConnectionTestResult` and `PanelAccountListResult` with `PanelAccount`. Outcomes are enums that start at 1: `PanelOperationOutcome` (`Succeeded`, `AlreadyApplied`, `NotFound`, `Refused`, `NotSupported`), `PanelConnectionOutcome`, `PanelRefusalReason` and `PanelAccountStatus`. A result never carries panel text or a secret; a refusal carries a `PanelRefusalReason`.
  - Idempotency is part of the contract and documented per operation: a retried create answers `AlreadyApplied` with the same reference, and a retried suspend, unsuspend, terminate or package change answers a success. A terminate of an account that is already gone is `AlreadyApplied`, never `NotFound`.
  - `ProvisioningContract` (for a plugin's own tests, like `StoredCredentialContract`): `AssertCreateIsIdempotentAsync`, `AssertSuspendIsIdempotentAsync`, `AssertUnsuspendIsIdempotentAsync`, `AssertTerminateIsIdempotentAsync`, `AssertChangePackageIsIdempotentAsync`, `AssertChangePasswordIsIdempotentAsync`, `AssertUnknownAccountIsNotFoundAsync`, `AssertTestConnectionAnswersAsync`, `AssertListAccountsIsCompleteAsync` and `AssertHidesSecrets`. They throw `PluginContractViolationException`.
  - `PluginCapabilities.ProvisioningPanel` (`"provisioningPanel"`), the manifest capability a plugin lists when it implements the interface.
  - `PluginManifest.ServerModules` (`"serverModules"`): the server module ids a provisioning panel plugin serves. `PluginServerModules.Validate` / `IsWellFormed` refuse an empty list, an empty, duplicate (ignoring case) or badly formed id (ASCII letters, digits, dash, dot, underscore, at most `PluginServerModules.MaxIdLength` = 50); violations are `PluginServerModuleViolation` with a `PluginServerModuleRule`.
  - `IPanelTransport` (`CreateClient()`): the host supplied HTTP client a panel plugin must use, taken in its constructor. It is routed through the host's outbound address guard, private route map, timeouts and no retry rule; a plugin never creates its own `HttpClient`.
  - `PanelServerConnection.UseSsl`; a null `Port` means the plugin's own default. `PanelAccount.ControlPanelUrl` and `PanelCreateResult.ControlPanelUrl` (a plain login page address, never a sign-in link). `Created` and `AlreadyExists` take an optional `controlPanelUrl`. The create context's `PanelClientRef` (reference, e-mail address, name) is required; the e-mail address and name inside it may be null.
- A second package, `Hostpanel.Sdk.Packaging` (project `Hostpanel.Sdk.Packaging/`, MIT, `net9.0`, versioned in step with this package), holds the checks a plugin package must pass so that the host that installs a package and the author who publishes it run the same code: the `plugin.sig` format and the exact v1 signature payload with Ed25519 signing and verification (`Signing`), plugin id, page id and strict semantic version rules (`Naming`), the assembly names a package may not carry (`PluginAssemblyNames`), archive guards for entry count, uncompressed size and path traversal (`Archive`), icon safety (`Icons`) and the structural manifest rules that need no host state (`Manifest`). Every refusal is an enum value. It is not part of the `Hostpanel.Sdk` package, which keeps its dependencies and its `net8.0` target.
- A test project, `tests/Hostpanel.Sdk.Packaging.Tests`, with a golden test that pins the v1 signature payload byte for byte.
- `PluginAssemblyOptions` (host prefix, plugin namespace prefix, extra reserved names): the host supplies how its own assemblies are named, so the package holds no host name. `PluginAssemblyNames.IsReserved` and `IsPluginNamespaceName` take it, together with the set of platform assemblies the caller provides.
- Behaviour of the new package worth knowing: `PluginArchiveGuard` compares paths exactly (ordinal), sums the declared size without overflow and accepts a folder entry that resolves to the unpack folder itself (`./`, `dir/..`); `PluginArchiveRule.EntryNotWritable` is answered when an entry cannot be written (a file and a folder of one name clash, or the path is not writable) instead of throwing. `PluginManifestChecker.ValidateSdkReference` reports a plugin folder that cannot be listed as `AssemblyUnreadable`. `PluginIconChecker` reads a style as a stylesheet does, with CSS escapes decoded and comments read as a space; a comment that never closes, or a text that keeps a pattern busy for a second, makes the icon invalid, and every pattern has a match timeout. `PluginSigner.Sign` writes `plugin.sig` through a temporary file and a move, and removes a leftover temporary file first. `PluginSignaturePayload.Build` refuses a package in which two files would stand for one payload path. `PluginVersion` implements `IEquatable<PluginVersion>` with `==` and `!=`, equal exactly when `CompareTo` is zero (build metadata is ignored).
- Both packages take their version from `Directory.Build.props` and are published as 3.1.0.

### Migrating a provisioning plugin to the panel operations

Nothing is required: `IProvisioningPlugin`, `ProvisioningProviderBase` and every existing type are unchanged, so a provisioning plugin built on 3.0.0 compiles and loads on 3.1.0 as it is. It has no panel operations, and a host that calls them skips it (`plugin is IProvisioningPanelPlugin` is false). To offer them:

1. Raise the package reference to `Hostpanel.Sdk` 3.1.0.
2. Implement `IProvisioningPanelPlugin` on the provisioning class, next to `ProvisioningProviderBase`. The ten operations are required; an operation the panel does not have answers `PanelOperationOutcome.NotSupported` instead of throwing.
3. Add `"provisioningPanel"` to `capabilities` in `plugin.json`, so the admin knows without activating the plugin. The list must agree with the code.
4. Return outcomes, not exceptions, for a refusal, a missing account and an unsupported operation; throw only when the result is unknown (the panel could not be reached). Never put panel text, a user name, a domain or a secret in a result, a log line or an exception message.
5. In the plugin's tests, call the `ProvisioningContract` checks against a stub of your panel.

### Changed

- Deprecated: the whole `Hostpanel.Sdk.Plugins.HostingPanel` area (`IHostingPanelApiClient`, `IHostingPanelV7ApiClient`, `HostingPanelConnection`, `HostingPanelAddress`, `HostingPanelApiReader`, `HostingPanelApiWire`, `HostingPanelServerInfo`, `HostingPanelV7ServerInfo`) is marked `[Obsolete]` and points to `IProvisioningPanelPlugin`. Nothing is removed in 3.1.0; the area goes in the next major version (4.0.0, named in 3.8.0). Code that uses it gets a warning, which a build that treats warnings as errors must suppress (`CS0618`) until it moves.

## [3.0.0]

Breaking: the package, the assembly and every namespace are renamed from `Innovayse.SDK` to `Hostpanel.Sdk`. No type, member or behaviour is renamed or removed besides this. The release also carries everything of 2.2.0 and 2.1.0 (the sections below).

### Added

- `RefundInstruction.AlreadyRefundedMinor` (required, `long`, zero or more): what the host has already recorded as refunded through the gateway order before this `RefundId`. A gateway with no idempotency key and no plugin storage can tell a retry from a new partial refund: if the gateway reports refunded at least `AlreadyRefundedMinor + AmountMinor`, the refund already happened, so skip it and answer its reference; otherwise send it. The default `RefundAsync(RefundInstruction, ct)` rejects a negative value and then calls the old overload, ignoring the value. A plugin that builds a `RefundInstruction` itself (in tests) passes the new argument.

- Documented: a plugin may take `IPluginStore` in its constructor; the host provides a plugin-wide store (scoped to the plugin id, no client) for data needed outside a routed call, with the same limits as the call store.

- Documented: the `endpoints` capability (`PluginCapabilities.Endpoints`) opens the client and admin routes, the pages and the slots for an app plugin only. For any other plugin type it currently opens only the public callback routes (`api.callbacks`).

### Changed

- `PackageId` and the assembly name are `Hostpanel.Sdk` (was `Innovayse.SDK`); the root namespace is `Hostpanel.Sdk`, so `Innovayse.SDK.Plugins.Payment` is now `Hostpanel.Sdk.Plugins.Payment` and `Innovayse.SDK.Base` is now `Hostpanel.Sdk.Base`. Every other namespace follows the same prefix change.
- The project file is `Hostpanel.Sdk.csproj` and the test project is `tests/Hostpanel.Sdk.Tests` (namespace `Hostpanel.Sdk.Tests`).

### Migrating a plugin

1. In the plugin project, replace `<PackageReference Include="Innovayse.SDK" ... />` with `<PackageReference Include="Hostpanel.Sdk" Version="3.0.0" ExcludeAssets="runtime" />`.
2. Replace the prefix `Innovayse.SDK` with `Hostpanel.Sdk` in every `using` and every fully qualified name (a find and replace of `Innovayse.SDK` with `Hostpanel.Sdk` is enough).
3. Rebuild. Package the plugin without `Hostpanel.Sdk.dll` (the host already loads it); a package that still ships `Innovayse.SDK.dll` is not loadable by a 3.0.0 host.
4. A plugin built against `Innovayse.SDK` 2.x does not load on a host that ships 3.0.0: rebuild it against 3.0.0. A manifest (`plugin.json`) needs no change.

Strings that embed the old name: none in the package. No type-name constant, JSON discriminator or reflection lookup uses the namespace or the assembly name.

## [2.2.0] (included in 3.0.0)

A plugin built against 2.1.0 loads unchanged; a view document without the new members renders as before. A host older than 2.2.0 refuses a view that uses them only if it checks unknown members, which no released host does; it renders the blocks without reacting to state.

### Added

- View state members (innovayse/hostpanel#852), documented on `PluginViews`:
  - `PluginViews.VisibleWhenProperty` (`"visibleWhen"`): a block shows only while a condition holds. A condition is `{ "route": <GET PluginViewRoute>, "field": "<dotted path>", "equals": <value or list of values> }`.
  - `PluginViews.ResetOnSuccessProperty` (`"resetOnSuccess"`): in a form's `submit`, `true` empties every field after a successful submit.
  - `PluginViews.FollowUpProperty` (`"followUp"`): on an action block or in a form's `submit`, `{ "refresh": [block ids], "until": <condition> }` reads the named blocks again after a successful call until the condition holds. The interval and the number of attempts belong to the host, so a plugin needs no polling of its own.
  - `PluginViews.MaxConditionValues` (20) and `PluginViews.MaxFollowUpRefresh` (20), the bounds of the `equals` and `refresh` lists.

## [2.1.0] (included in 3.0.0)

A plugin built against 2.0.0 loads unchanged: it only reads the fulfilment contexts and calls `IPluginHostApi`. The host builds the contexts and implements `IPluginHostApi`, so a host, and a plugin test that builds a context itself, passes the new `ClientRef` argument and implements the two new members.

### Added

- `PluginManifest.Icon` (`"icon": "icon.svg"`), an optional package-relative path to an SVG or PNG file that is the plugin's own icon. It is optional and defaults to `null`, so an existing manifest and plugin are unaffected; the host shows the colour block when no icon is declared (innovayse/hostpanel#845).
- Callbacks, requests from outside the panels such as a gateway notifying a plugin: `PluginArea.Callback` (3), `PluginApi.Callbacks` (`api.callbacks`, a list of `PluginRoute`, empty by default; a callback route has no `Permission` and is not `Streaming`, and `PluginApi.CallbackProblems()` lists the entries that break this), `PluginCall.Headers` (the request headers the host forwards, empty by default) and `PluginReplyKind.Raw` (5) with `PluginReply.Raw(status, body, contentType)`, which writes the bytes as they are. The status is 200 to 599 except 401 and 407, and the media type must not contain a line break. A callback carries no panel session: the plugin verifies the sender itself.
- Refund idempotency: `RefundInstruction(GatewayOrderId, AmountMinor, RefundId)` and the overload `IPaymentPlugin.RefundAsync(RefundInstruction, CancellationToken)`, a default interface member that falls back to the existing `RefundAsync(string, long, CancellationToken)`. The host calls the overload with a stable `RefundId` it stores before the first call and reuses on every retry; a plugin overrides it to pass the id to the gateway as the idempotency key, so a repeated id never refunds twice and answers the same reference. A plugin that does not override it compiles and works unchanged, without that protection.
- Cancelling an unsettled payment, as a separate optional interface: `IPaymentCancelPlugin` (extends `IPaymentPlugin`) with `CancelPaymentAsync(gatewayOrderId, ct)`, returning `PaymentCancelResult(Outcome, GatewayReference)`. `PaymentCancelOutcome` is `Cancelled` 1, `AlreadyCancelled` 2, `NotCancellable` 3. A repeated cancel is idempotent. Existing plugins are unaffected.
- `StoredCredentialContract.AssertCompleteSaveMethodIsIdempotentAsync(plugin, sessionId, calls, ct)`, a test helper for plugin authors that completes one session several times and throws `PluginContractViolationException` unless every call answers the same result (and `Saved` agrees with `Method`, and nothing throws).
- A test project, `tests/Hostpanel.Sdk.Tests`, run by CI; it is not part of the package.

### Changed

- `IStoredCredentialPlugin` now extends `IPaymentPlugin`. The documentation always said a stored credential plugin is implemented on its payment class beside `IPaymentPlugin`, and the host resolves it on top of the payment plugin, so a class that implemented both compiles unchanged; a class that implemented only `IStoredCredentialPlugin` now also has to implement the payment members.

### Added earlier in 2.1.0

- `ClientRef`, a record parameter after `ClientId`, on `InvoicePaidContext`, `InvoiceRefundedContext`, `RefundAdviceContext` and `SubscriptionLapsedContext`: the same plugin-scoped client reference an endpoint call of that plugin receives in `PluginCaller.ClientRef`, so what a plugin sold can be joined with what its endpoints see (innovayse/hostpanel#757).
- SSH access to a bound hosting service (innovayse/hostpanel#758): `IPluginHostApi.IssueSshAccessAsync(serviceId, label)` returns a `PluginSshAccess` (key id, host, port, user name, domain, the Ed25519 private key in OpenSSH format and the pinned host key fingerprint) and `IPluginHostApi.RevokeSshAccessAsync(serviceId, keyId)` takes it back. A refusal is a `PluginHostAccessException` carrying a `PluginHostAccessFailure`. `PluginSshAccess.ToString()` leaves the private key out.
- `PluginManifest.HostAccess` (`"hostAccess": ["ssh"]`), the kinds of hosting access a plugin asks for, named in `PluginHostAccess`.
- `PluginViews`, the view document schema a page or slot `view` file follows: `SchemaVersion` (1), `BlockKinds` (`header`, `text`, `stat`, `table`, `form`, `action`, `link`, `badge`, `empty`, `alert`, `tabs`, `chat`, `progress`), `LabelProperties` (members whose text is a label the host resolves in the caller's language) and `RouteProperties` (members that name a plugin route) (innovayse/hostpanel#643).
- `PluginViewRoute`, how a view names a route of the plugin: by `Method` and the path template exactly as declared in `api`, the area being the one of the page or slot that shows the view; `Names(route)` tells whether it names a declared route (innovayse/hostpanel#643).
- `PluginCapabilities.Endpoints` (`"endpoints"`), a capability any plugin type may declare in `capabilities`: the plugin class also implements `IPluginEndpoints`, and the host serves its declared `api` routes and its `ui` pages, menus and slots. An app plugin needs no declaration (innovayse/hostpanel#818).

### Documentation

- `PluginBillingCycle` and `FulfilmentLine.BillingCycle` list every cycle the host sends with its value (`Monthly` 1, `Annual` 2, `Quarterly` 3, `SemiAnnual` 4, `Biennial` 5) and tell a plugin to treat a cycle it does not know as a renewing line of unknown period, never as one-off and never by throwing (innovayse/hostpanel#761).

## [2.0.0]

This is a major release. A plugin built against 1.x recompiles after updating its `using` lines and the renames below; the table in "Migrating from 1.x" lists every move and rename.

**A plugin built against 1.x cannot load in a 2.0.0 host and must be rebuilt against 2.0.0.** The types it was compiled against moved namespaces and some were renamed, so the compiled plugin fails on a type load; updating the host does not fix it, only a rebuild does.

### Breaking: renames and changed shapes

- `PaymentRequest` is renamed `PaymentInstruction` (same members). `IPaymentPlugin.CreatePaymentAsync` takes it, and its parameter is now called `instruction`, so a caller that passes it by name changes too.
- `GatewayPaymentStatus.CurrencyNumeric` is renamed `Currency`, the name `StoredChargeInstruction`, `StoredChargeResult` and `StoredChargeLookup` already use for the same ISO 4217 numeric code.
- `CredentialCustomerDetails` is renamed `CredentialCustomerContext`, like the other inputs that describe a situation (`SaveMethodContext`, the fulfilment contexts). The parameter of `IStoredCredentialPlugin.EnsureCustomerAsync` is now called `context`.
- `FulfilmentLine.BillingCycle` is the new enum `PluginBillingCycle?` (`Monthly` 1, `Annual` 2, `Quarterly` 3, `SemiAnnual` 4, `Biennial` 5; `null` for a one-off line) instead of the text `"monthly"`, `"annual"` and so on.
- `HostingPanelApiWire` is a sealed record whose properties are `init` only; it was a class with setters.

### Breaking: namespaces

The plugin contracts are regrouped so a type is where its area says: every enum is in `Innovayse.SDK.Plugins.Enums`, and the interfaces, records and classes sit in a folder per area, each interface beside the records it works with. The types below were directly in `Innovayse.SDK.Plugins` and moved; the other types of 1.x keep their namespaces (the enums in `Plugins.Enums`, the fulfilment and identity types in `Plugins.Fulfilment` and `Plugins.Identity`, the stored credential types in `Plugins.StoredCredentials`, `IHostedPlugin` in `Plugins.Hosting`, the manifest blocks in `Plugins.Manifest`, and the base classes in `Innovayse.SDK.Base`). No type is left directly in `Plugins`, so `using Innovayse.SDK.Plugins;` is no longer needed and can be removed.

### Migrating from 1.x

| Type in 1.x | Old namespace | Type in 2.0.0 | New namespace |
|---|---|---|---|
| `IPaymentPlugin` | `Innovayse.SDK.Plugins` | `IPaymentPlugin` | `Innovayse.SDK.Plugins.Payment` |
| `PaymentRequest` | `Innovayse.SDK.Plugins` | `PaymentInstruction` (renamed) | `Innovayse.SDK.Plugins.Payment` |
| `PaymentSession` | `Innovayse.SDK.Plugins` | `PaymentSession` | `Innovayse.SDK.Plugins.Payment` |
| `GatewayPaymentStatus` (`CurrencyNumeric`) | `Innovayse.SDK.Plugins` | `GatewayPaymentStatus` (`Currency`) | `Innovayse.SDK.Plugins.Payment` |
| `IProvisioningPlugin` | `Innovayse.SDK.Plugins` | `IProvisioningPlugin` | `Innovayse.SDK.Plugins.Provisioning` |
| `IRegistrarPlugin` | `Innovayse.SDK.Plugins` | `IRegistrarPlugin` | `Innovayse.SDK.Plugins.Registrar` |
| `IHostingPanelApiClient`, `IHostingPanelV7ApiClient` | `Innovayse.SDK.Plugins` | unchanged names | `Innovayse.SDK.Plugins.HostingPanel` |
| `HostingPanelAddress`, `HostingPanelApiReader`, `HostingPanelApiWire`, `HostingPanelConnection`, `HostingPanelServerInfo`, `HostingPanelV7ServerInfo` | `Innovayse.SDK.Plugins` | unchanged names | `Innovayse.SDK.Plugins.HostingPanel` |
| `PluginManifest`, `PluginField`, `PluginCapabilities` | `Innovayse.SDK.Plugins` | unchanged names | `Innovayse.SDK.Plugins.Manifest` |
| `CredentialCustomerDetails` | `Innovayse.SDK.Plugins.StoredCredentials` | `CredentialCustomerContext` (renamed) | `Innovayse.SDK.Plugins.StoredCredentials` |
| `FulfilmentLine` (`BillingCycle` text) | `Innovayse.SDK.Plugins.Fulfilment` | `FulfilmentLine` (`BillingCycle` is `PluginBillingCycle?`) | `Innovayse.SDK.Plugins.Fulfilment` |

`IHostedPlugin`, `IFulfilmentPlugin`, `IClientResolver` and `IStoredCredentialPlugin` stay where they were.

### Added

- The app plugin type: `PluginType.App`, appended with the explicit value `6`. The members before it keep their
  values (`Provisioning` 0 to `Hosted` 5). An app plugin implements `IPluginEndpoints` and answers the routes its
  manifest declares in `api`.
- The endpoint contract, with no web framework type and no token, in `Plugins.Endpoints`:
  - `IPluginEndpoints` (`HandleAsync(PluginCall, CancellationToken)`).
  - `PluginCall`: area, method, matched route template, language, route values, query, verified slot context, body
    and content type, the caller, and the call-bound `Host` and `Store`.
  - `PluginCaller`: opaque `ClientRef` and `UserRef`, the permission names and `HasPermission`. The references are
    opaque and differ per plugin.
  - `PluginReply`: the factories `Json`, `NoContent`, `Problem` and `Stream`, and `Kind`, `Status`, `Body`, `Code`,
    `Detail` and `Events`. `Json` accepts only 200, 201, 202 and 203; `Problem` refuses 401 and 407, which the host
    answers itself, so a plugin that refuses a caller answers 403. `Stream` documents cancellation (the host cancels
    the enumerator, so an iterator needs `[EnumeratorCancellation]`; the `HandleAsync` token is not used for the
    stream) and that `Host` and `Store` stay valid until the stream ends.
  - `PluginEvent`: `Data`, `Name`, `Id` and `FromJson`. The constructor refuses a `null` `Data` and a `Name` or `Id`
    with a carriage return or line feed, because they become single lines of a server-sent event.
- The read-only host API, in `Plugins.Host`: the interface `IPluginHostApi` (`GetProfileAsync`, `ListServicesAsync`,
  `ListInvoicesAsync`; each list is at most the 50 most recent) bound to the call's client, so no method takes a client id, and its shapes
  `PluginClientProfile`, `PluginClientService` and `PluginClientInvoice`.
- `IPluginStore` in `Plugins.Store`: text key and value storage scoped to the plugin and the call's client.
- Optional lifecycle capabilities, in `Plugins.Lifecycle`: `IPluginStatus` (`GetStatusAsync`, answering the record
  `PluginStatusReport` with a `PluginHealth` and an optional detail) and `IPluginMigrations` (`MigrateAsync`).
- The enums `PluginArea` (`Client`, `Admin`), `PluginReplyKind` (`Json`, `NoContent`, `Problem`, `Stream`),
  `PluginHealth` (`Healthy`, `Degraded`, `Unavailable`), `PluginServiceStatus` (`Pending` to `Terminated`),
  `PluginInvoiceStatus` (`Draft` to `PartiallyRefunded`) and `PluginBillingCycle`, all with values from 1; 0 is not valid.
- `PluginPermissions.Any`, the documented marker (`"Any"`) that admits every signed-in caller in the `permission`
  of a client route, page or slot entry.
- The slot list, in `Plugins.Slots`: `PluginSlots` with `Version` (`1.0`), `All`, `Find` and the id constants,
  `PluginSlot` (with `Offers(key)`, which a host uses to validate the context keys of a widget) and
  `PluginSlotContextKeys`. Slot list 1.0:

  | Slot id | Area | Context keys |
  |---|---|---|
  | `client.dashboard.cards` | client | none |
  | `client.service.tabs` | client | `serviceId` |
  | `admin.client.panels` | admin | `clientId` |
  | `admin.service.panels` | admin | `serviceId`, `clientId` |

### Versioning of the slot list

`PluginSlots.Version` follows semantic versioning on its own. Adding a slot or a context key is a minor change of
the list and of this package. Removing a slot or a context key, renaming one, or changing what a context key
holds is a major change of both.

### Changed

- `PluginManifest.Fields` reads a missing or `null` `fields` as an empty list, like the other lists of the manifest.
- The base classes (`PaymentGatewayBase`, `ProvisioningProviderBase`, `RegistrarProviderBase`) refuse a `null`
  argument with `ArgumentNullException`, and an empty plugin id or key with `ArgumentException`.
- `HostingPanelAddress.BaseUrl` writes an IPv6 host in brackets (`https://[2001:db8::1]:2304`), and refuses a `null` host with `ArgumentNullException` and an empty or blank host with `ArgumentException`.
- A host that does not know the app type refuses a manifest that declares it.
- Every `PluginType` member now carries its value explicitly (`Provisioning` 0 to `Hosted` 5, `App` 6). The values
  are unchanged.
- The ids a plugin sees are documented: a client area call carries only the opaque `ClientRef` and `UserRef`, and the
  host never lets the store be plugin wide there; an admin call carries the real host ids (`clientId`, `serviceId`)
  in `Context`; `PluginClientService.Id` and `PluginClientInvoice.Id` are host ids of the caller's own records.

## [1.4.0]

### Added

- Manifest version 2. `PluginManifest` gains `ManifestVersion` (`1` when `manifestVersion` is absent, so a version 1
  manifest reads exactly as before), `Locales` (locale code to text key to text), `Settings`, `Api`, `Ui` and
  `ExternalOrigins`. Every new list, map and block reads as empty when absent or `null`, never as `null`.
- Typed settings: `PluginSetting` (`Key`, `Label`, `Type`, `Required`, `Options`), `PluginSettingOption` and the
  enum `PluginSettingType` (`Text`, `Url`, `Select`, `Secret`, `Number`, `Boolean`, `Textarea`, values 1 to 7).
  A setting also carries `Description`, `Default` (text), `Placeholder` and, for a number, `Min` and `Max`.
  They supersede `PluginField`, which stays readable. `PluginManifest.GetSettings()` answers one list for either
  format and `PluginSetting.FromField` converts a legacy field (`secret` and `password` become `Secret`, an unknown
  type becomes `Text`).
- The declared surface, in namespace `Innovayse.SDK.Plugins.Manifest`: `PluginApi` (routes in two lists, `Client` and
  `Admin`, like `PluginUi`; an admin route has no `Permission`) and `PluginRoute` (`Method`, `Path`, `Permission`,
  `RateLimit` and `Streaming`), with the enums `PluginRouteMethod` (`Get` to `Delete`, values 1 to 5) and
  `PluginRateLimitPolicy` (`Default`, `Strict`, `Heavy`, values 1 to 3; no policy named means `Default`), `PluginUi`,
  `PluginUiArea` (menus and pages for the client and the admin panel), `PluginMenuItem` (with `Order`),
  `PluginPage` and `PluginSlotEntry` (`Id`, `Slot`, `View`, `Title`, `Order`, `Permission`, `ProductIds` and
  `ContextKeys`).
- A version 2 manifest declares `sdkVersion` `1.0`, the one value the host supports; see the readme.

### Changed

- Nothing is removed or renamed. The package only reads and carries the new blocks; checking them is up to the host.

## [1.3.0]

### Added

- The optional `IStoredCredentialPlugin` capability for payment plugins (namespace
  `Innovayse.SDK.Plugins.StoredCredentials`): a payment plugin may keep payment methods at the gateway and charge
  them without the payer present. It comes with `CredentialCustomerDetails`, `CredentialCustomerResult`,
  `SaveMethodContext`, `SaveMethodSession`, `SavedMethod`, `SavedMethodResult`, `StoredChargeInstruction`,
  `StoredChargeResult` and `StoredChargeLookup`, and the enums `StoredChargeOutcome` and `SaveMethodMode`.
  `IPaymentPlugin` does not change, so existing plugins keep compiling. The enum values start at 1 (0 is not
  valid), `SaveMethodContext.ReturnUrl` is always an absolute address, `SavedMethod.Brand` and `Last4` are nullable
  for methods that are not cards, and `StoredChargeLookup` carries the `ActionUrl` of a charge that still needs
  the payer to authenticate.
- The optional `capabilities` list in the manifest (`PluginManifest.Capabilities`), with the names in
  `PluginCapabilities` (`storedCredentials`).

### Changed

- The SDK has its own repository, [innovayse/hostpanel-sdk](https://github.com/innovayse/hostpanel-sdk), with its
  history kept. The package's project and repository links point here, and the project builds on its own: it pins
  its package versions itself instead of reading them from the host's central package list.
- The package is built, packed and published by this repository's workflows.

## [1.2.0]

### Added

- Plugin contracts for six plugin types: provisioning, payment, registrar, identity, fulfilment and hosted
  plugins, with the base classes `ProvisioningProviderBase`, `PaymentGatewayBase` and `RegistrarProviderBase`.
- The package carries the MIT license text and its readme, which shows how to pin a version, write a minimal
  plugin and ship it.
