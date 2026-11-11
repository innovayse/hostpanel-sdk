# Contributing to Hostpanel.Sdk

Thank you for helping. This repository holds the plugin contracts a hostpanel host loads plugins
through. It is MIT licensed, and by contributing you agree that your contribution is licensed
under the same terms.

## Issues

Open an issue for a bug in a contract, a base class or the package readme, or to propose a new
contract. Say what you are building and what the current contracts do not let you do.

## Pull requests

1. Fork the repository and branch from `develop`.
2. Build and pack the project exactly as CI does:

   ```bash
   dotnet build Hostpanel.Sdk.csproj -c Release -warnaserror
   dotnet pack Hostpanel.Sdk.csproj -c Release -o artifacts
   dotnet build Hostpanel.Sdk.Packaging/Hostpanel.Sdk.Packaging.csproj -c Release -warnaserror
   dotnet pack Hostpanel.Sdk.Packaging/Hostpanel.Sdk.Packaging.csproj -c Release -o artifacts
   ```

3. Document every type and member with XML doc comments (private ones too), and brace every
   block. An undocumented public member fails the build.
4. Raise `<Version>` in `Directory.Build.props`, the one version both packages share, when the change should be published (see
   "Versioning" in `README.md`) and add an entry to `CHANGELOG.md`.
5. Open the pull request against `develop`. `main` is only updated from `develop`, and a push to `main` publishes the package.

## Changes the host must support

A new contract, a new manifest field or a new plugin type does nothing until the host knows about
it. Such a change also needs a pull request in
[innovayse/hostpanel](https://github.com/innovayse/hostpanel), which includes this repository as a
git submodule at `backend/src/Hostpanel.Sdk`: that pull request moves the submodule to your commit
and carries the host side. Link the two pull requests to each other.

## New operations are new interfaces

A contract never grows by a member. Adding a method to an interface that plugins already implement breaks every one of
them at compile time, and a plugin built on an earlier version would no longer load. A new operation therefore arrives as
a new interface that extends the existing one (for example `IRegistrarWhoisPlugin` extends `IRegistrarOperationsPlugin`),
with its own manifest capability in `PluginCapabilities`. The host checks `plugin is INewInterface` at run time and treats
a plugin that does not implement it as `NotSupported`. The same goes for a new refusal reason: append it to the enum and
never renumber the values that exist.
