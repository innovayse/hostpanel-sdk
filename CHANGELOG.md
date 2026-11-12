# Changelog

All notable changes to the `Hostpanel.Sdk` package are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this package adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [3.7.0]

### Added

- `PaymentInstruction.CustomerRef` (`string?`, optional, `null` by default) (innovayse/hostpanel-sdk#36): the gateway-side customer the saved method belongs to. The host fills it with the reference it got from `IStoredCredentialPlugin.EnsureCustomerAsync` whenever `SavePaymentMethod` is `true`, so a plugin can attach the stored method to that customer and a later stored charge for the same reference finds it. A plugin that does not save methods ignores it.
- `IPaymentReturnPlugin : IPaymentPlugin` (innovayse/hostpanel-sdk#43), an optional interface with `CompleteAsync(gatewayOrderId, returnParameters, ct)`, for gateways that finish a payment with a value only the payer's browser has when it comes back (a payment method nonce, a payer id in the return address). The host calls it, when the plugin implements it, with the query of the return request (and the posted fields of a browser step) before it asks for the status; it must be idempotent. `returnParameters` is an `IReadOnlyDictionary<string, string>`, so it is read only, and the host keeps within `PaymentReturnLimits` (`MaxParameters` 32, `MaxNameLength` 64, `MaxValueLength` 2048) and never passes credentials, cookies or headers. A plugin without the interface compiles and loads as before and is never asked to complete.

### Compatibility

- `CustomerRef` is a trailing optional record parameter, like the 3.5.0 members: callers and plugins recompile unchanged, and the existing constructor call sites keep their meaning. The record constructor and `Deconstruct` gain a parameter, so a plugin built against 3.6.0 binaries keeps working only when it is rebuilt against 3.7.0.

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

- Deprecated: the whole `Hostpanel.Sdk.Plugins.HostingPanel` area (`IHostingPanelApiClient`, `IHostingPanelV7ApiClient`, `HostingPanelConnection`, `HostingPanelAddress`, `HostingPanelApiReader`, `HostingPanelApiWire`, `HostingPanelServerInfo`, `HostingPanelV7ServerInfo`) is marked `[Obsolete]` and points to `IProvisioningPanelPlugin`. Nothing is removed in 3.1.0; the area goes in the next major version. Code that uses it gets a warning, which a build that treats warnings as errors must suppress (`CS0618`) until it moves.

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
