# Hostpanel.Sdk

Plugin contracts for a hostpanel host. A plugin compiles against this package, implements one
or more of the contracts below, and ships as its own assembly plus a `plugin.json` manifest.

[![NuGet](https://img.shields.io/nuget/v/Hostpanel.Sdk?label=nuget)](https://www.nuget.org/packages/Hostpanel.Sdk)
[![License: MIT](https://img.shields.io/badge/license-MIT-blue)](https://github.com/innovayse/hostpanel-sdk/blob/main/LICENSE)

[Hostpanel](https://github.com/innovayse/hostpanel) |
[Product site](https://hostpanel.innovayse.com) |
[Documentation](https://hostpanel.innovayse.com/docs/) |
[Write a plugin](https://hostpanel.innovayse.com/docs/developers/write-a-plugin) |
[Plugin marketplace](https://plugins-hostpanel.innovayse.com) |
[Discord](https://discord.gg/NfqTkyTgD)

## License

This package is MIT licensed (see `LICENSE` in the package). You decide how to license your
plugin: it may be free or paid, open source or closed source. The hostpanel host
([innovayse/hostpanel](https://github.com/innovayse/hostpanel)) is AGPL-3.0-or-later; compiling
against this package does not put your plugin under the AGPL.

## Referencing the package

Pin the version you build against, so a new SDK release never changes your plugin by surprise:

```bash
dotnet add package Hostpanel.Sdk --version 3.6.0
```

or, in the plugin's `.csproj`:

```xml
<ItemGroup>
  <!-- ExcludeAssets="runtime" keeps Hostpanel.Sdk.dll out of the plugin's output: the host already
       loads it, and a second copy in the plugin folder breaks every cast (see "Shipping" below). -->
  <PackageReference Include="Hostpanel.Sdk" Version="3.6.0" ExcludeAssets="runtime" />
</ItemGroup>
```

A version still being built on `develop` ships as a prerelease, `X.Y.Z-preview.N`, published from a
`vX.Y.Z-preview.N` tag. Pin it the same way (`Version="2.1.0-preview.1"`) to build against an API that is
not released yet, and move to `X.Y.Z` once it is. NuGet never picks a prerelease for you.

The package targets `net8.0`, so a plugin can target `net8.0` or later. The version follows the
contract: a new minor version adds members, a new major version removes or renames them. The
manifest's `sdkVersion` field is the plugin contract level the host checks, not the package version: see
"Manifest" below for the value to declare.

Upgrading from 1.x? Version 2.0.0 moves types into one namespace per area and renames a few members. The migration table in [`CHANGELOG.md`](CHANGELOG.md#migrating-from-1x) lists every move and rename. A plugin built against 1.x cannot load in a 2.0.0 host: it must be rebuilt against 2.0.0.

## Plugin types

| `PluginType` | Contract | What it is for |
|---|---|---|
| `Provisioning` | `IProvisioningPlugin`, `ProvisioningProviderBase` | Hosting accounts on a server or control panel. `IProvisioningPanelPlugin` adds the account operations (see "Provisioning panel plugins") and `IVirtualServerPlugin` the operations of a virtual server or cloud platform (see "Virtual server plugins"); the marker and the base class give settings access. |
| `Payment` | `IPaymentPlugin`, `PaymentGatewayBase` | A payment gateway: start a payment, read its status, refund it. |
| `Registrar` | `IRegistrarPlugin`, `RegistrarProviderBase` | A domain registrar. `IRegistrarOperationsPlugin` adds the domain operations (see "Registrar plugins"); the marker and the base class give settings access. |
| `Identity` | `IClientResolver` | Decides which client a signed-in principal acts for and what it may do there. At most one identity plugin may be enabled. |
| `Fulfilment` | `IFulfilmentPlugin` | Delivers digital products once an invoice is paid, and is asked before a refund moves money back. Every enabled fulfilment plugin sees every digital invoice. |
| `Hosted` | `IHostedPlugin` | A background worker the host starts after its own services are up and stops on shutdown. A plugin of another type may implement it too. |
| `App` | `IPluginEndpoints` | Answers the routes its manifest declares and adds pages and slot content, without web framework types or user tokens. |

### Fulfilment plugins

`IFulfilmentPlugin` delivers digital products. When a payment is recorded for an invoice with at
least one digital line, the host writes a fulfilment row in the same transaction as the payment and
delivers it to every enabled fulfilment plugin in the background.

- **At least once.** A delivery is retried with back-off until the plugin returns normally; throw to
  fail an attempt and have it retried. The same paid invoice can therefore reach you more than once, so
  **every method must be idempotent on `InvoiceId`**: delivering twice must leave the same result as once.
- `OnInvoicePaidAsync` runs when a paid invoice has a digital line. `OnInvoiceRefundedAsync` runs after the
  money moved back, to reverse the fulfilment. `OnSubscriptionLapsedAsync` runs when a renewal invoice passes
  its grace period unpaid.
- `AdviseRefundAsync` runs **before** a refund moves money, with the amount the administrator asked for.
  Return a `RefundAdvice`: `MaxRefundableAmount` is the most that may still be refunded in the invoice's
  currency (return the requested amount to allow it in full) and `Reason` is a short sentence for the
  administrator. The host takes the smallest cap across all plugins. A cap of zero or less refuses the refund,
  a smaller positive cap reduces it and your reason is written to the refund's notes. If the call throws or
  does not answer in time the refund is refused: the host cannot tell whether you would have capped it.

### Identity plugins

`IClientResolver` decides which client a signed-in principal acts for. When one is enabled, every
client-portal route calls `ResolveAsync` with the caller's `ClaimsPrincipal` instead of using the host's
default rule (the client whose owner equals the principal's subject). Return `null` when the principal acts
for no account; the host then answers that there is no client profile. Otherwise return a `ClientAccess`:

- `Account` is an `ExternalAccount`: a `Provider` name you choose and never change (for example `crm`), the
  account's `Id` in that system, and optionally a `DisplayName` and a `Currency` for a client created on the
  first order. The host stores the pair on its own client row, unique per provider, and creates the row
  when needed; the plugin never touches persistence.
- `CanPurchase` and `CanManagePaymentMethods` map to the host's client permissions. Every admitted
  principal can read services, domains, open support tickets and emails. `CanPurchase` adds placing orders and
  viewing and paying invoices. `CanManagePaymentMethods` adds viewing invoices, which is the permission that
  guards the saved payment method actions. A plugin cannot grant any other permission.

At most one identity plugin may be enabled: two enabled at once is a configuration error and is refused, not
resolved by picking one.

### Hosted plugins

`IHostedPlugin` is a background worker. The host calls `StartAsync` once per process, after its own services
are up, and `StopAsync` when it begins to stop (the token is cancelled when the shutdown grace period ends).
Return from `StartAsync` once the worker is running; do not block on its loop. A plugin of another type may
implement `IHostedPlugin` too and is then started the same way. A plugin that throws while starting or
stopping is logged with its id and never stops the host or the other plugins.

### App plugins

An app plugin (`PluginType.App`, value `6`) answers requests. It implements `IPluginEndpoints` (namespace `Hostpanel.Sdk.Plugins.Endpoints`, one method, `HandleAsync(PluginCall, CancellationToken)`) and declares its routes in the manifest `api` block. The host
matches the request to a declared route, checks the caller's permission and only then calls the plugin, so the
plugin sees no HTTP request, no token and no web framework type.

- `PluginCall` carries the matched route template, route values, query, body, language, the verified slot
  `Context` and the call-bound `Host` and `Store`. `PluginCaller` holds an opaque `ClientRef` and `UserRef`
  (stable for your plugin, different for every other plugin) and the permission names of the caller.
- Ids: a client area call carries only the opaque `ClientRef` and `UserRef`, never a host client or user id. An admin
  call carries real host ids in `Context` (`clientId`, `serviceId`), because an administrator is staff.
  `PluginClientService.Id` and `PluginClientInvoice.Id` are host ids of the caller's own records.
- Reply with `PluginReply.Json` (status 200, 201, 202 or 203), `NoContent`, `Problem(status, code)` or `Stream(events)`.
  `Problem` refuses 401 and 407, which the host answers itself: refuse a caller with 403. An exception is reported as
  a plugin failure without its detail; return `Problem` for an expected failure.
- A stream is a sequence of `PluginEvent` values the host forwards as server-sent events. A `PluginEvent` refuses a
  `null` `Data` and a `Name` or `Id` containing a carriage return or line feed. Cancellation reaches the stream
  through its enumerator: when the caller disconnects the host cancels the token it passes to `GetAsyncEnumerator`,
  so an iterator method needs a parameter marked `[EnumeratorCancellation]` and must pass it to every awaited call.
  The token given to `HandleAsync` is not used for the stream. `call.Host` and `call.Store` stay valid until the
  stream ends, and not after.
- `IPluginHostApi` gives read-only facts about the call's client: profile, services and invoices (each list is at
  most the 50 most recent). It is bound to that client, so no method takes a client id. `IPluginStore` is your own
  text key and value storage, scoped to your plugin and the call's client. The host refuses a client area call
  without a client, so the store is never plugin wide there; only an admin call outside any client has a plugin wide
  store.
  A plugin that needs data outside a routed call (a payment plugin remembering a gateway order id between
  `CreatePaymentAsync`, `GetStatusAsync` and `RefundAsync`, or a cursor read by a webhook) may also take
  `IPluginStore` in its constructor. The host provides it when it activates the plugin: it is plugin wide, scoped
  to your plugin id and to no client, with the same key rules, size limits and removal semantics as the call store
  (it is the same data as the call store of an admin call outside any client). It never reaches another plugin's or
  a client's values. Keep client data in `call.Store`, which is bound to the call's client.
- Optional: implement `IPluginStatus` to report health, and `IPluginMigrations` to bring data kept outside the
  host up to date before the first call.

### Slots

A slot is a place in a host page where a plugin adds content (`ui.slots` in the manifest). `PluginSlots` is the
stable, versioned list; `PluginSlots.Version` is `1.0`. A widget may declare only the context keys its slot offers;
the host checks them with `PluginSlot.Offers(key)`. The context values are host ids (see "Ids" above).

| Slot id | Area | Context keys |
|---|---|---|
| `client.dashboard.cards` | client | none (the caller's client is implied) |
| `client.service.tabs` | client | `serviceId` |
| `admin.client.panels` | admin | `clientId` |
| `admin.service.panels` | admin | `serviceId`, `clientId` |

Versioning rule: adding a slot or a context key is a minor change; removing or changing one is a major change.

### Endpoints on any plugin type

An app plugin implements `IPluginEndpoints` and the host serves its client and admin routes, pages, menus and slots.
Any other plugin type can have the same by also implementing `IPluginEndpoints` and listing `endpoints` (`PluginCapabilities.Endpoints`)
in the manifest's `capabilities`. The host then serves the declared `api` routes and the `ui` pages, menus and slots of
that plugin exactly as it does for an app plugin, while the plugin keeps its own type's contract: a `Fulfilment`
plugin that grants credits on a paid invoice can also give the client a screen, and both sides see the same
`ClientRef`. Without the capability a plugin that is not an app plugin has its `api` and `ui` blocks refused, and a
declared capability whose class does not implement `IPluginEndpoints` is refused when the plugin is activated.

### Callbacks

A plugin can receive requests from outside the panels, for example a gateway telling it a payment changed. Declare
the routes under `api.callbacks` in `plugin.json` (a list like `api.client`, `PluginApi.Callbacks`); the host calls
`IPluginEndpoints.HandleAsync` with `PluginCall.Area` set to `PluginArea.Callback`. A callback route has no
`permission` and is not `streaming`, because there is no signed-in caller; it may set a `rateLimit`.
`PluginApi.CallbackProblems()` lists the entries that break this. The host forwards request headers in
`PluginCall.Headers` (empty on other calls), and the plugin must verify the sender itself, for example by a signature
over `PluginCall.Body`. Answer with `PluginReply.Raw(status, bytes, contentType)` when the sender expects a fixed
plain answer such as `OK`; the status is 200 to 599 except 401 and 407.

### Refunds, cancelling and idempotency

The host calls `IPaymentPlugin.RefundAsync(RefundInstruction, ct)`. `RefundInstruction.RefundId` is a stable id the
host stores before the first call and reuses on every retry of the same refund (after a timeout, a crash or an
exception). Override that overload and pass the id to the gateway as its idempotency key, or record it yourself when
the gateway has none: a repeated id must never refund twice and should answer the same reference. A plugin that only
implements `RefundAsync(gatewayOrderId, amountMinor, ct)` keeps working, without that protection.

A gateway with no idempotency key can use `RefundInstruction.AlreadyRefundedMinor` instead of storage: it is what the
host has already recorded as refunded through that gateway order before this `RefundId`. If the gateway reports a
refunded total of at least `AlreadyRefundedMinor + AmountMinor`, the refund already happened (a retry), so skip it and
answer the earlier reference; otherwise send it.

A plugin that can void a payment which was created but not settled also implements `IPaymentCancelPlugin`
(`CancelPaymentAsync`). It is optional; a repeated cancel answers `AlreadyCancelled`, and a payment that can no longer
be cancelled answers `NotCancellable` (a refund is the way back) instead of throwing.

### Provisioning panel plugins

A hosting control panel is a provisioning plugin that also implements `IProvisioningPanelPlugin`, which extends the
`IProvisioningPlugin` marker. The marker and `ProvisioningProviderBase` do not change, so a provisioning plugin built on
3.0.0 compiles and loads as before, with no panel operations. List `provisioningPanel` in the manifest's `capabilities`;
the list must agree with the code. One plugin serves every server of its panel type: each call carries the server as a
`PanelServerConnection` with the credentials the host stores, so keep no per-server state.

| Operation | Context | Result | Repeat |
|---|---|---|---|
| `CreateAccountAsync` | `PanelCreateAccountContext` | `PanelCreateResult` | An account already held under the same user name is `AlreadyApplied` with the same `ProvisioningRef` |
| `SuspendAccountAsync` | `PanelSuspendContext` | `PanelActionResult` | Already suspended is a success |
| `UnsuspendAccountAsync` | `PanelAccountContext` | `PanelActionResult` | Already active is a success |
| `TerminateAccountAsync` | `PanelTerminateContext` | `PanelActionResult` | Already gone is `AlreadyApplied`, never `NotFound` |
| `ChangePackageAsync` | `PanelChangePackageContext` | `PanelActionResult` | Already on the package is a success |
| `ChangePasswordAsync` | `PanelChangePasswordContext` | `PanelActionResult` | Sets it again and succeeds |
| `CreateSsoLinkAsync` | `PanelSsoContext` | `PanelSsoResult` | Issues a new link each time |
| `GetUsageAsync` | `PanelAccountContext` | `PanelUsageResult` | Read only |
| `TestConnectionAsync` | `PanelTestConnectionContext` | `PanelConnectionTestResult` | Never throws for an unreachable panel |
| `ListAccountsAsync` | `PanelListAccountsContext` | `PanelAccountListResult` | Read only, paged by `NextCursor` |

- **Outcomes, not exceptions.** Every operation answers a `PanelOperationOutcome` (`Succeeded`, `AlreadyApplied`,
  `NotFound`, `Refused`, `NotSupported`); the test answers a `PanelConnectionOutcome`. The values start at 1 and 0 is
  not valid. A refusal carries a `PanelRefusalReason` and the host turns it into a sentence in the caller's language.
  Throw only when you do not know what happened, for example when the panel could not be reached: the host then retries,
  which is safe because of the next point.
- **Idempotency.** The host retries after a lost answer. It chooses the user name of a create and stores it before the
  first call, so the create is keyed by `PanelCreateAccountContext.Username`. When the panel refuses a repeat, look the
  account up and answer from what you find instead of repeating the refusal.
- **No panel text, no secrets.** A result never contains the panel's own error text (it can carry a user name, a domain
  or an address) or a secret. Log the detail and answer a reason. `AccessHash`, `ApiToken`, `Password`, `NewPassword` and
  the single sign on link print as `***`; do not undo that in your own logs.
- **Packages and references.** `PanelPackage.Name` is the name as the panel knows it. `PanelService.ProvisioningRef` is
  exactly what your `CreateAccountAsync` returned; it is `null` only for the create.
- **Usage** is in bytes, and every figure is optional: report what the panel has.
- **Network.** Take an `IPanelTransport` in the constructor and send every request on `CreateClient()`. Never create
  your own `HttpClient`: only the transport is routed through the host's outbound address guard, private route map,
  timeouts and no retry rule. The client has no base address; build it from `PanelServerConnection` (`UseSsl`; a null
  `Port` means your own default port) and set the authorization header per request.
- **Server modules.** List the module ids the plugin serves in `serverModules` of `plugin.json` (ASCII letters, digits,
  dash, dot, underscore, at most 50, no duplicates). `PluginServerModules.Validate` checks the list.
- **Addresses.** `PanelAccount.ControlPanelUrl` and `PanelCreateResult.ControlPanelUrl` are the account's control panel
  login page when the panel tells it; they are not sign-in links.
- **The old panel area is obsolete.** `IHostingPanelApiClient`, `IHostingPanelV7ApiClient` and the types beside them
  (`Hostpanel.Sdk.Plugins.HostingPanel`) are marked `[Obsolete]`; move to `IProvisioningPanelPlugin`.
- **Listing** is for reconciliation: following `NextCursor` to the end returns every account once, and a panel that
  returns everything at once answers `null` as the cursor.

In the plugin's own tests, run the `ProvisioningContract` checks against a stub of your panel:
`AssertCreateIsIdempotentAsync`, `AssertSuspendIsIdempotentAsync`, `AssertUnsuspendIsIdempotentAsync`,
`AssertTerminateIsIdempotentAsync`, `AssertChangePackageIsIdempotentAsync`, `AssertChangePasswordIsIdempotentAsync`,
`AssertUnknownAccountIsNotFoundAsync`, `AssertTestConnectionAnswersAsync`, `AssertListAccountsIsCompleteAsync` and
`AssertHidesSecrets`. Each throws `PluginContractViolationException` naming the promise that was broken.

### Registrar plugins

A domain registrar is a registrar plugin that also implements `IRegistrarOperationsPlugin`, which extends the
`IRegistrarPlugin` marker. The marker and `RegistrarProviderBase` do not change, so a registrar plugin built on an earlier
version compiles and loads as before, with no operations. List `registrarOperations` in the manifest's `capabilities`; the
list must agree with the code. One plugin serves one registrar account: each call carries it as a `RegistrarConnection`
(account name, `ApiKey`, optional `ApiUrl` and `ClientIp`, `TestMode`), so keep no state between calls.

| Operation | Context | Result | Repeat |
|---|---|---|---|
| `CheckAvailabilityAsync` | `RegistrarAvailabilityContext` | `RegistrarDataResult<RegistrarAvailability>` | Read only |
| `RegisterAsync` | `RegistrarRegisterContext` | `RegistrarDataResult<RegistrarOrder>` | A domain the account already holds is `AlreadyApplied` with the same `RegistrarRef` |
| `TransferAsync` | `RegistrarTransferContext` | `RegistrarDataResult<RegistrarOrder>` | A transfer already under way or complete is `AlreadyApplied` |
| `RenewAsync` | `RegistrarRenewContext` | `RegistrarDataResult<RegistrarOrder>` | An expiry already past `CurrentExpiresAt` plus the years is `AlreadyApplied` |
| `CheckDomainActiveAsync` | `RegistrarDomainContext` | `RegistrarDataResult<RegistrarOrder>` | Read only, for an order that answered `RequiresPolling` |
| `CancelTransferAsync`, `InitiateOutgoingTransferAsync` | `RegistrarDomainContext` | `RegistrarActionResult` | Already cancelled or prepared is a success |
| `GetEppCodeAsync` | `RegistrarDomainContext` | `RegistrarDataResult<RegistrarEppCode>` | Read only; the code prints as `***` |
| `GetNameserversAsync` | `RegistrarDomainContext` | `RegistrarDataResult<IReadOnlyList<string>>` | Read only |
| `SetNameserversAsync` | `RegistrarNameserversContext` | `RegistrarActionResult` | The same list is a success |
| `SetAutoRenewAsync`, `SetWhoisPrivacyAsync`, `SetRegistrarLockAsync` | `RegistrarFlagContext` | `RegistrarActionResult` | The state already wanted is a success |
| `GetContactAsync` | `RegistrarDomainContext` | `RegistrarDataResult<RegistrarContact>` | Read only |
| `SetContactAsync` | `RegistrarContactContext` | `RegistrarActionResult` | The same contact is a success |
| `GetDnsRecordsAsync` | `RegistrarDomainContext` | `RegistrarDataResult<IReadOnlyList<RegistrarDnsRecord>>` | Read only |
| `AddDnsRecordAsync`, `UpdateDnsRecordAsync` | `RegistrarDnsRecordContext` | `RegistrarActionResult` | The same record is a success |
| `DeleteDnsRecordAsync` | `RegistrarDnsRecordRemovalContext` | `RegistrarActionResult` | A record already gone is `AlreadyApplied`, never `NotFound` |
| `GetTldPricingAsync` | `RegistrarPricingContext` | `RegistrarDataResult<IReadOnlyList<RegistrarTldPricing>>` | Read only |
| `TestConnectionAsync` | `RegistrarTestConnectionContext` | `PanelConnectionTestResult` | Never throws for an unreachable registrar |

- **Outcomes, not exceptions.** Every operation answers a `PanelOperationOutcome` (`Succeeded`, `AlreadyApplied`,
  `NotFound`, `Refused`, `NotSupported`), the same one the provisioning contracts use; the test answers a
  `PanelConnectionOutcome`. A refusal carries a `RegistrarRefusalReason`. Throw only when you do not know what happened,
  for example when the registrar could not be reached: the host then retries, which is safe because of the idempotency
  column above. A purchase is a success only when the registrar did it; a registrar that is not configured refuses.
- **No registrar text, no secrets.** A result never contains the registrar's own error text (it can carry a domain or a
  contact detail) or a secret. `ApiKey` and the authorization code print as `***`; do not undo that in your own logs.
- **Orders.** `RegistrarOrder.RequiresPolling` is `true` while the registrar has accepted an order but not finished it, as a
  transfer in usually is; the host then calls `CheckDomainActiveAsync` until it is `false`. `ExpiresAt` is `null` when the
  registrar does not say, and the host extends the expiry it holds.
- **Network.** Take an `IPanelTransport` in the constructor and send every request on `CreateClient()`; never create your
  own `HttpClient`. The client has no base address, so build it from `RegistrarConnection.ApiUrl` or your own default, and
  honour `TestMode` by calling the sandbox API and never the live one.
- **Not in the contract yet.** E-mail forwarding rules, the DNS management switch and the WHOIS lookup stay with the host
  for now and will be added as separate operations.

In the plugin's own tests, run the `RegistrarContract` checks against a stub of your registrar:
`AssertRegisterIsIdempotentAsync`, `AssertTransferIsIdempotentAsync`, `AssertRenewIsIdempotentAsync`,
`AssertSetNameserversIsIdempotentAsync`, `AssertSetAutoRenewIsIdempotentAsync`, `AssertSetWhoisPrivacyIsIdempotentAsync`,
`AssertSetRegistrarLockIsIdempotentAsync`, `AssertSetContactIsIdempotentAsync`, `AssertDeleteDnsRecordIsIdempotentAsync`,
`AssertUnknownDomainIsNotFoundAsync`, `AssertTestConnectionAnswersAsync` and `AssertHidesSecrets`. Each throws
`PluginContractViolationException` naming the promise that was broken.

### Virtual server plugins

A virtual server or cloud platform is a provisioning plugin that also implements `IVirtualServerPlugin`, which extends the
`IProvisioningPlugin` marker. It is a capability of the provisioning type and not a new plugin type: a virtual server is
a service provisioned on a server the host knows, like a hosting account, and the plugin type values do not change. The
marker, `ProvisioningProviderBase` and `IProvisioningPanelPlugin` do not change either. List `virtualServer` in the
manifest's `capabilities` and the module ids the plugin serves in `serverModules`; the lists must agree with the code. The
vocabulary is the one of the panel contract: the platform arrives as a `PanelServerConnection`, the service as a
`PanelService` whose `ProvisioningRef` is what `CreateServerAsync` returned, and the answers are `PanelActionResult` and
`PanelDataResult<T>`.

| Operation | Context | Result | Repeat |
|---|---|---|---|
| `CreateServerAsync` | `VirtualServerCreateContext` | `PanelDataResult<VirtualServerInstance>` | A server already held under the same `Name` is `AlreadyApplied` with the same `ProvisioningRef` |
| `StartServerAsync`, `StopServerAsync` | `PanelAccountContext` | `PanelActionResult` | Already running or stopped is a success |
| `RebootServerAsync` | `PanelAccountContext` | `PanelActionResult` | Restarts again each time |
| `SuspendServerAsync` | `PanelSuspendContext` | `PanelActionResult` | Already suspended is a success |
| `UnsuspendServerAsync` | `PanelAccountContext` | `PanelActionResult` | Not suspended is a success |
| `TerminateServerAsync` | `PanelTerminateContext` | `PanelActionResult` | Already gone is `AlreadyApplied`, never `NotFound` |
| `RebuildServerAsync` | `VirtualServerRebuildContext` | `PanelDataResult<VirtualServerRootPassword>` | Erases the disk again each time |
| `ChangePlanAsync` | `PanelChangePackageContext` | `PanelActionResult` | Already on the plan is a success |
| `ResetRootPasswordAsync` | `PanelChangePasswordContext` | `PanelDataResult<VirtualServerRootPassword>` | Sets it again and succeeds |
| `CreateConsoleSessionAsync` | `PanelAccountContext` | `PanelDataResult<VirtualServerConsole>` | Issues a new address each time |
| `GetServerUsageAsync` | `PanelAccountContext` | `PanelDataResult<VirtualServerUsage>` | Read only |
| `ListImagesAsync`, `ListPlansAsync` | `VirtualServerCatalogContext` | `PanelDataResult<IReadOnlyList<...>>` | Read only |
| `TestServerConnectionAsync` | `PanelTestConnectionContext` | `PanelConnectionTestResult` | Never throws for an unreachable platform |

- **Outcomes, idempotency, secrets and network** follow the panel contract: outcomes instead of exceptions, a stable
  create keyed by the name the host stores before the first call, no platform text in a result, and every request on the
  `IPanelTransport` client. `PanelRefusalReason` gains `ImageUnknown` and `NameInvalid` for this contract; the plan name
  travels in `PanelPackage.Name` and is a name `ListPlansAsync` returned.
- **Passwords.** The host sends the root password to set. A platform that cannot set one generates it and answers it in
  `VirtualServerInstance.GeneratedPassword` or `VirtualServerRootPassword.GeneratedPassword`; both print as `***`. The
  console address in `VirtualServerConsole.Url` is a credential and prints as `***` too.
- **Usage** is in bytes, and every figure is optional: report what the platform has.
- **Suspension.** A platform with no suspension of its own powers the server off and keeps the client from starting it in
  whatever way the platform allows; `UnsuspendServerAsync` lifts that and does not power the server on.

In the plugin's own tests, run the `VirtualServerContract` checks against a stub of your platform:
`AssertCreateIsIdempotentAsync`, `AssertStartIsIdempotentAsync`, `AssertStopIsIdempotentAsync`,
`AssertSuspendIsIdempotentAsync`, `AssertUnsuspendIsIdempotentAsync`, `AssertTerminateIsIdempotentAsync`,
`AssertChangePlanIsIdempotentAsync`, `AssertUnknownServerIsNotFoundAsync`, `AssertCatalogIsWellFormedAsync`,
`AssertTestConnectionAnswersAsync` and `AssertHidesSecrets`. Each throws `PluginContractViolationException` naming the
promise that was broken.

### Saving the method during a payment

To let a payer save the method they pay with, the host sets `PaymentInstruction.SavePaymentMethod` to `true`. A plugin
that supports it asks the gateway to store the method and, when `GetStatusAsync` reports the payment as paid, returns it
in `GatewayPaymentStatus.SavedMethod` (a `SavedMethod`: token reference, brand, last four digits and expiry). A plugin
that does not support it ignores the flag and leaves `SavedMethod` null; the host treats that as "not saved" and the
payment itself is unaffected. Both members are optional, so a plugin that never saves methods needs no change.

### Stored credential plugins

A payment plugin may also implement `IStoredCredentialPlugin` to keep payment methods at the gateway and charge
them later without the payer present. The interface extends `IPaymentPlugin`, so the implementing class must be a
payment plugin too, and `IPaymentPlugin` itself does not change. In the plugin's own tests, call
`StoredCredentialContract.AssertCompleteSaveMethodIsIdempotentAsync(plugin, sessionId)` to check that
`CompleteSaveMethodAsync` answers the same result on every call for one session. The host checks for the interface at run
time; a disabled plugin, or one without the interface, has no such capability. List `storedCredentials` in the
manifest's `capabilities` so the admin and the client portal know without activating the plugin; the list must
agree with the code.

- **Outcomes.** `ChargeAsync` answers a `StoredChargeOutcome`. `Succeeded` means the money is taken: set
  `TransactionId` and report the amount and currency so the host can refuse a mismatch. `Declined` is final.
  `ActionRequired` means the payer must authenticate; set `ActionUrl`. `Pending` is accepted but not settled: the
  host never treats it as paid and resolves it later through `FindChargeAsync`.
- **A refusal is an outcome, not an exception.** Throw only when the outcome is unknown, for example when the
  gateway could not be reached.
- **Idempotency.** The host creates the `IdempotencyKey` once per attempt and stores it before calling you. A
  retry of the same attempt reuses it, so a repeated `ChargeAsync` with the same key must never charge twice:
  pass the key to the gateway, or look it up yourself first when the gateway has no native idempotency. The key
  is opaque ASCII, at most 64 characters.
- **`FindChargeAsync`** answers whether a key ever charged. It is the recovery path after a timeout or a crash.
  `Found = false` is trusted only when the lookup itself succeeded; if it cannot be answered, throw.
- **Saving a method.** `BeginSaveMethodAsync` returns a `SaveMethodMode`; the values start at 1 and 0 is not valid,
  as for `StoredChargeOutcome`. `SaveMethodContext.ReturnUrl` is always an absolute address: a `Redirect` plugin
  sends the payer back to it, an `EmbeddedToken` plugin ignores it. With `EmbeddedToken` your own front end part
  mounts its card field with `ClientToken`; with `Redirect` the payer goes to `RedirectUrl` and comes back. The host
  calls `CompleteSaveMethodAsync` in both modes, and may call it more than once for a session, so it must be
  idempotent per `SessionId`: every call answers the same method and never stores it twice. An abandoned or
  expired session answers `Saved = false` and does not throw. Resolve the result from the `SessionId` alone, the
  host passes nothing else. The consent wording and the tick box stay in the host.
- **`ActionRequired`.** Set `ActionUrl`. After the payer authenticates, the host resolves the final outcome with
  `FindChargeAsync` and the same idempotency key, so report the settled outcome there. While the charge still
  waits for the payer, `FindChargeAsync` returns `ActionRequired` with the `ActionUrl`, so the host can offer it again.
- **The instruction.** `StoredChargeInstruction.AmountMinor` is positive, and the host checks `AcceptsCurrency`
  before it calls `ChargeAsync`.
- A `SavedMethod` that is not a card leaves `Brand` and `Last4` null, and a method that does not expire reports
  `ExpiryMonth` and `ExpiryYear` as 0.
- The host stores no methods: it lists them live through `ListMethodsAsync` and keeps only its consent link to a
  `MethodRef`.

## Manifest

`plugin.json` sits beside the plugin assembly and declares `id`, `name`, `version`, `author`,
`description`, `type`, `category`, `entryPoint` (the full type name the host creates) and
`sdkVersion`, plus optional `fields` (the settings an administrator fills in), `color`,
`showInIntegrations`, `integrationsCategory` and `capabilities` (the optional capabilities the plugin
implements: `storedCredentials` for a payment plugin, `endpoints` for any type), `minHostVersion` and
`maxHostVersion` (the range of host versions the plugin runs on, each a semantic version such as `2.1.0`; a missing
bound means no limit, and the lower may not be above the upper) and `changelog` (notes for this version, at most 4000
characters). The `id` is lower-case letters and digits,
words joined by single hyphens.

### Manifest version 2

`manifestVersion` names the format of the manifest. It is `1` when absent, and a version 1 manifest reads exactly
as before. Version 2 declares everything a plugin offers up front, so the host can check it before any plugin
code runs. Every block is optional and an absent or `null` block reads as empty.

| Member | What it declares |
|---|---|
| `settings` | Typed settings an administrator fills in; supersedes `fields`. Each has `key`, `label`, `type` (`text`, `url`, `select`, `secret`, `number`, `boolean`, `textarea`) and optional `required`, `description`, `default` (always text, for example `"30"` or `"true"`), `placeholder`, `min` and `max` (for `number`) and `options` (`value` and `label`, for `select`). |
| `locales` | Texts per locale code: `{ "en": { "menu.reports": "Reports" } }`. A `label` or `title` may be a key of it. |
| `api.client`, `api.admin` | The routes the plugin answers, one list per panel. Each has `method` (`get`, `post`, `put`, `patch`, `delete`), `path`, an optional `rateLimit` (`default`, `strict`, `heavy`; absent means `default`) and `streaming` (`true` for a route that replies with a stream of events). A client route may have a `permission`; an admin route never has one, so its `Permission` reads as `null`. |
| `ui.client`, `ui.admin` | What the plugin adds to each panel: `menus` (`id`, `label`, `page`, optional `icon` and `order`) and `pages` (`id`, `path`, `title`, `view`, optional `permission` on the client panel). |
| `ui.slots` | Content for the slots the host offers: `id` (the widget id, unique within the plugin), `slot`, `view` and optional `title`, `order` (lowest first), `permission` (client slots only), `productIds` (show only for services of these products; empty means every product) and `contextKeys` (the slot context the widget needs, for example `serviceId`). |
| `externalOrigins` | The web origins the plugin's pages may load content from or link to. |

A version 2 manifest declares `"sdkVersion": "1.0"`. The host accepts exactly that value today and skips or
refuses a plugin that declares another, whatever package version it was built with; the value moves only when the
host starts to support a new contract level.

`fields` stays readable. `PluginManifest.GetSettings()` answers one list whichever format a manifest uses: the
`settings` when there are any, otherwise each field converted to a setting (`secret` and `password` become
`secret`, an unknown type becomes `text`).

```json
{
  "manifestVersion": 2,
  "sdkVersion": "1.0",
  "settings": [
    { "key": "baseUrl", "label": "setting.baseUrl", "type": "url", "required": true },
    { "key": "mode", "label": "setting.mode", "type": "select",
      "options": [ { "value": "live", "label": "Live" }, { "value": "test", "label": "Test" } ] }
  ],
  "api": {
    "client": [ { "method": "get", "path": "/invoices/{id}", "permission": "Any" },
                { "method": "post", "path": "/chat", "rateLimit": "strict", "streaming": true } ],
    "admin": [ { "method": "get", "path": "/status" } ]
  },
  "ui": {
    "client": {
      "menus": [ { "id": "reports", "label": "menu.reports", "page": "reports" } ],
      "pages": [ { "id": "reports", "path": "/reports", "title": "menu.reports", "view": "views/reports.json" } ]
    },
    "slots": [ { "id": "summary", "slot": "client.dashboard.cards", "view": "views/card.json", "order": 10 } ]
  },
  "externalOrigins": [ "https://example.com" ]
}
```

The package only reads and carries the declared surface; checking it and acting on it is the host's job.

### View documents

The `view` member of a page or slot names a JSON file of the package. The document is declarative: it describes what
a page shows and the plugin routes it calls, and the panel renders it. The shape is described by `PluginViews`.

```json
{
  "schema": 1,
  "blocks": [
    { "kind": "header", "title": "pages.site",
      "actions": [ { "kind": "action", "label": "actions.publish", "confirm": "actions.publishConfirm",
                     "route": { "method": "post", "path": "/services/{serviceId}/site/publish" } } ] },
    { "kind": "table", "source": { "route": { "method": "get", "path": "/services/{serviceId}/site" } },
      "columns": [ { "label": "columns.name" } ] }
  ]
}
```

- **Document.** An object with `schema` (`PluginViews.SchemaVersion`, today `1`) and `blocks`, an array of blocks.
- **Block.** Any object in the document that has a `kind` member: the top-level blocks, the `blocks` of a tab, the
  `actions` of a header. `kind` is one of `PluginViews.BlockKinds` (`header`, `text`, `stat`, `table`, `form`,
  `action`, `link`, `badge`, `empty`, `alert`, `tabs`, `chat`, `progress`, `split`, `preview`). Every other member is the block's own data.
- **Labels.** A string under a member of `PluginViews.LabelProperties` (`title`, `label`, `confirm`, `placeholder`,
  `text`), at any depth, is a label: a key of the manifest `locales` or plain text, like a menu `label`. The host
  replaces it with its text in the caller's language, English when that language lacks the key, before the document
  reaches a panel, so a panel never sees a key.
- **Route references.** An object under a member of `PluginViews.RouteProperties` (`route`, `send`, `history`), at any
  depth, names one route of the plugin as a `PluginViewRoute`: `{ "method": "post", "path": "/services/{serviceId}/site/publish" }`.
  Routes have no ids, so a view names a route by method and the path template exactly as `api` declares it. The area
  is the one of the page or slot that shows the view (a client page uses `api.client`, an admin page `api.admin`, a
  slot the area of the slot). Each `{parameter}` is filled from the page path parameter or the slot context key of
  the same name.

The host checks every view document when it loads a plugin and when one is installed, before any plugin code runs: the
file exists (`PLUGIN_VIEW_MISSING`), is a JSON object (`PLUGIN_VIEW_INVALID`), has a supported `schema`
(`PLUGIN_VIEW_SCHEMA_UNSUPPORTED`), uses only known block kinds (`PLUGIN_VIEW_BLOCK_KIND_UNKNOWN`), every label that
looks like a key exists in the `en` locale (`PLUGIN_LABEL_KEY_MISSING`) and every route reference names a route
declared in the area (`PLUGIN_VIEW_ROUTE_UNDECLARED`). A plugin that fails one is refused.

## How the host builds a plugin

The host creates the `entryPoint` type once the plugin is enabled in the admin panel and every
required field has a value. The constructor receives an `IConfiguration` holding only this
plugin's settings, under `integration:{id}:{key}`, and may also ask for host services such as
`ILogger<T>`.

## A minimal plugin

A hosted plugin that logs a heartbeat. The project:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Hostpanel.Sdk" Version="3.6.0" ExcludeAssets="runtime" />
    <None Include="plugin.json" CopyToOutputDirectory="PreserveNewest" />
  </ItemGroup>
</Project>
```

The plugin:

```csharp
using Hostpanel.Sdk.Plugins.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Example.Heartbeat;

/// <summary>Logs a heartbeat at the interval the administrator set.</summary>
public sealed class HeartbeatPlugin : IHostedPlugin, IDisposable
{
    /// <summary>The seconds between two heartbeats.</summary>
    private readonly TimeSpan _interval;

    /// <summary>Where the heartbeat is written.</summary>
    private readonly ILogger<HeartbeatPlugin> _logger;

    /// <summary>Stops the loop when the host stops the plugin.</summary>
    private readonly CancellationTokenSource _stopping = new();

    /// <summary>The running loop, awaited on stop.</summary>
    private Task _loop = Task.CompletedTask;

    /// <summary>Builds the plugin from its own settings.</summary>
    /// <param name="configuration">This plugin's settings only.</param>
    /// <param name="logger">The host's logger.</param>
    public HeartbeatPlugin(IConfiguration configuration, ILogger<HeartbeatPlugin> logger)
    {
        var seconds = int.TryParse(configuration["integration:heartbeat:interval_seconds"], out var value) ? value : 60;
        _interval = TimeSpan.FromSeconds(Math.Max(seconds, 1));
        _logger = logger;
    }

    /// <inheritdoc />
    public Task StartAsync(CancellationToken ct)
    {
        _loop = RunAsync(_stopping.Token);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task StopAsync(CancellationToken ct)
    {
        await _stopping.CancelAsync();
        await _loop.WaitAsync(ct);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _stopping.Dispose();
    }

    /// <summary>Writes a heartbeat every interval until stopped.</summary>
    /// <param name="ct">Cancelled when the plugin is stopped.</param>
    /// <returns>A task that completes when the loop ends.</returns>
    private async Task RunAsync(CancellationToken ct)
    {
        using var timer = new PeriodicTimer(_interval);
        try
        {
            while (await timer.WaitForNextTickAsync(ct))
            {
                _logger.LogInformation("Heartbeat");
            }
        }
        catch (OperationCanceledException)
        {
            // Stopping is the normal way out of the loop.
        }
    }
}
```

The manifest, `plugin.json`:

```json
{
  "id": "heartbeat",
  "name": "Heartbeat",
  "version": "1.0.0",
  "author": "You",
  "description": "Logs a heartbeat.",
  "type": "Hosted",
  "category": "Utilities",
  "entryPoint": "Example.Heartbeat.HeartbeatPlugin",
  "sdkVersion": "1.0",
  "fields": [
    { "key": "interval_seconds", "label": "Seconds between heartbeats", "type": "text", "required": false }
  ]
}
```

## Shipping

Zip the plugin's own assembly, its own dependencies and `plugin.json` with the manifest at the
root of the archive, and upload it in the admin panel under Plugins, or place the folder in the
host's `plugins/<id>/` folder. Never ship `Hostpanel.Sdk.dll` or a host assembly inside the plugin:
the host refuses such an archive, and a second copy of the SDK on disk would make every cast fail.

## Checking and signing a package

The checks a host runs on an uploaded package (the `plugin.sig` signature, plugin id and version rules,
reserved assembly names, archive guards, icon safety and the structural manifest rules) are published as a
second package, `Hostpanel.Sdk.Packaging` (MIT, `net9.0`, same version as this package), so you can run the same
code in your release pipeline before you publish: `PluginSigner.Sign(packageDirectory, keyId, privateKey)` writes
the signature, and `PluginManifestChecker`, `PluginIconChecker` and `PluginAssemblyNames` answer the same questions
the host will. See `Hostpanel.Sdk.Packaging/README.md`.

## Versioning

The package follows [semantic versioning](https://semver.org/): a patch release fixes, a minor
release adds members, a major release removes or renames them. A version is published by raising
`<Version>` in `Directory.Build.props` (one version for `Hostpanel.Sdk` and `Hostpanel.Sdk.Packaging`): the publish workflow pushes a version the feed does not have
yet and skips one it already has, so a change that keeps the version publishes nothing. Each
release is listed in `CHANGELOG.md`.

## Contributing

Issues and pull requests are welcome in
[innovayse/hostpanel-sdk](https://github.com/innovayse/hostpanel-sdk); see `CONTRIBUTING.md` there.
A change the host has to support as well (a new contract, a new manifest field, a new plugin
type) also needs a pull request in [innovayse/hostpanel](https://github.com/innovayse/hostpanel),
which includes this repository as a git submodule at `backend/src/Hostpanel.Sdk` and moves it to
the new commit.

Questions and chat: [Discord](https://discord.gg/NfqTkyTgD). To list a finished plugin for operators, see
[Publishing a plugin](https://hostpanel.innovayse.com/docs/developers/publishing-a-plugin) and the
[plugin marketplace](https://plugins-hostpanel.innovayse.com).
