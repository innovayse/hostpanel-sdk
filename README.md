# Innovayse.SDK

Plugin contracts for a hostpanel host. A plugin compiles against this package, implements one
or more of the contracts below, and ships as its own assembly plus a `plugin.json` manifest.

## License

This package is MIT licensed (see `LICENSE` in the package). You decide how to license your
plugin: it may be free or paid, open source or closed source. The rest of the hostpanel
repository is AGPL-3.0-or-later; compiling against this package does not put your plugin under
the AGPL.

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

## Manifest

`plugin.json` sits beside the plugin assembly and declares `id`, `name`, `version`, `author`,
`description`, `type`, `category`, `entryPoint` (the full type name the host creates) and
`sdkVersion`, plus optional `fields` (the settings an administrator fills in), `color`,
`showInIntegrations` and `integrationsCategory`. The `id` is lower-case letters and digits,
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
