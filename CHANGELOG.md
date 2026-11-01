# Changelog

All notable changes to the `Innovayse.SDK` package are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this package adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [2.0.0]

This is a major release. A plugin built against 1.x recompiles after updating its `using` lines and the renames below; the table in "Migrating from 1.x" lists every move and rename.

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
    `Detail` and `Events`.
  - `PluginEvent`: `Data`, `Name`, `Id` and `FromJson`.
- The read-only host API, in `Plugins.Host`: the interface `IPluginHostApi` (`GetProfileAsync`, `ListServicesAsync`,
  `ListInvoicesAsync`) bound to the call's client, so no method takes a client id, and its shapes
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
  `PluginSlot` and `PluginSlotContextKeys`. Slot list 1.0:

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
- `HostingPanelAddress.BaseUrl` writes an IPv6 host in brackets (`https://[2001:db8::1]:2304`).
- A host that does not know the app type refuses a manifest that declares it.

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
