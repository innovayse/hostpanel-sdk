# Changelog

All notable changes to the `Innovayse.SDK` package are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this package adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

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
