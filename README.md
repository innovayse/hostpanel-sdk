# Innovayse.SDK

Plugin contracts for a hosting-panel host. A plugin compiles against this package, implements one
or more of the contracts below, and ships as its own assembly plus a `plugin.json` manifest.

## Plugin types

- **Provisioning** — hosting / server provisioning (control-panel and server plugins).
- **Payment** — a payment gateway.
- **Registrar** — a domain registrar.
- **Identity** — resolves which client a signed-in principal acts for (`IClientResolver`).
- **Fulfilment** — delivers digital products once an invoice is paid, and is asked before a
  refund moves money back (`IFulfilmentPlugin`).
- **Hosted** — a background worker the host starts and stops with its own lifetime
  (`IHostedPlugin`).

## Manifest

`plugin.json` declares `Id`, `Name`, `Version`, `Author`, `Description`, `Type`, `Category`,
`EntryPoint` (the fully-qualified type name the host loads) and `SdkVersion`.

The host loads a plugin from `plugins/<dir>/` by `EntryPoint`; never ship `Innovayse.SDK.dll`
inside your plugin folder — two copies of the SDK assembly make every cast fail.
