# Changelog

All notable changes to the `Innovayse.SDK` package are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this package adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

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
