# Innovayse.SDK

Plugin contracts for a hostpanel host. A plugin compiles against this package, implements one
or more of the contracts below, and ships as its own assembly plus a `plugin.json` manifest.

## License

This package is MIT licensed (see `LICENSE` in the package). You decide how to license your
plugin: it may be free or paid, open source or closed source. The hostpanel host
([innovayse/hostpanel](https://github.com/innovayse/hostpanel)) is AGPL-3.0-or-later; compiling
against this package does not put your plugin under the AGPL.

## Referencing the package

Pin the version you build against, so a new SDK release never changes your plugin by surprise:

```bash
dotnet add package Innovayse.SDK --version 1.2.0
```

or, in the plugin's `.csproj`:

```xml
<ItemGroup>
  <!-- ExcludeAssets="runtime" keeps Innovayse.SDK.dll out of the plugin's output: the host already
       loads it, and a second copy in the plugin folder breaks every cast (see "Shipping" below). -->
  <PackageReference Include="Innovayse.SDK" Version="1.2.0" ExcludeAssets="runtime" />
</ItemGroup>
```

The package targets `net8.0`, so a plugin can target `net8.0` or later. The version follows the
contract: a new minor version adds members, a new major version removes or renames them. The
manifest's `sdkVersion` field records the major and minor version you built against.

## Plugin types

| `PluginType` | Contract | What it is for |
|---|---|---|
| `Provisioning` | `IProvisioningPlugin`, `ProvisioningProviderBase` | Hosting accounts on a server or control panel. The contract is a marker today: the base class gives settings access, there are no operations yet. |
| `Payment` | `IPaymentPlugin`, `PaymentGatewayBase` | A payment gateway: start a payment, read its status, refund it. |
| `Registrar` | `IRegistrarPlugin`, `RegistrarProviderBase` | A domain registrar. The contract is a marker today: the base class gives settings access, there are no operations yet. |
| `Identity` | `IClientResolver` | Decides which client a signed-in principal acts for and what it may do there. At most one identity plugin may be enabled. |
| `Fulfilment` | `IFulfilmentPlugin` | Delivers digital products once an invoice is paid, and is asked before a refund moves money back. Every enabled fulfilment plugin sees every digital invoice. |
| `Hosted` | `IHostedPlugin` | A background worker the host starts after its own services are up and stops on shutdown. A plugin of another type may implement it too. |

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

### Stored credential plugins

A payment plugin may also implement `IStoredCredentialPlugin` to keep payment methods at the gateway and charge
them later without the payer present. `IPaymentPlugin` does not change. The host checks for the interface at run
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
- **Saving a method.** `BeginSaveMethodAsync` returns a `SaveMethodMode`. With `EmbeddedToken` your own front end
  part mounts its card field with `ClientToken`; with `Redirect` the payer goes to `RedirectUrl` and comes back,
  and the host calls `CompleteSaveMethodAsync`. The consent wording and the tick box stay in the host.
- The host stores no methods: it lists them live through `ListMethodsAsync` and keeps only its consent link to a
  `MethodRef`.

## Manifest

`plugin.json` sits beside the plugin assembly and declares `id`, `name`, `version`, `author`,
`description`, `type`, `category`, `entryPoint` (the full type name the host creates) and
`sdkVersion`, plus optional `fields` (the settings an administrator fills in), `color`,
`showInIntegrations`, `integrationsCategory` and `capabilities` (the optional capabilities the plugin
implements, for example `storedCredentials`). The `id` is lower-case letters and digits,
words joined by single hyphens.

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
    <PackageReference Include="Innovayse.SDK" Version="1.2.0" ExcludeAssets="runtime" />
    <None Include="plugin.json" CopyToOutputDirectory="PreserveNewest" />
  </ItemGroup>
</Project>
```

The plugin:

```csharp
using Innovayse.SDK.Plugins.Hosting;
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
  "sdkVersion": "1.2",
  "fields": [
    { "key": "interval_seconds", "label": "Seconds between heartbeats", "type": "text", "required": false }
  ]
}
```

## Shipping

Zip the plugin's own assembly, its own dependencies and `plugin.json` with the manifest at the
root of the archive, and upload it in the admin panel under Plugins, or place the folder in the
host's `plugins/<id>/` folder. Never ship `Innovayse.SDK.dll` or a host assembly inside the plugin:
the host refuses such an archive, and a second copy of the SDK on disk would make every cast fail.

## Versioning

The package follows [semantic versioning](https://semver.org/): a patch release fixes, a minor
release adds members, a major release removes or renames them. A version is published by raising
`<Version>` in `Innovayse.SDK.csproj`: the publish workflow pushes a version the feed does not have
yet and skips one it already has, so a change that keeps the version publishes nothing. Each
release is listed in `CHANGELOG.md`.

## Contributing

Issues and pull requests are welcome in
[innovayse/hostpanel-sdk](https://github.com/innovayse/hostpanel-sdk); see `CONTRIBUTING.md` there.
A change the host has to support as well (a new contract, a new manifest field, a new plugin
type) also needs a pull request in [innovayse/hostpanel](https://github.com/innovayse/hostpanel),
which includes this repository as a git submodule at `backend/src/Innovayse.SDK` and moves it to
the new commit.
