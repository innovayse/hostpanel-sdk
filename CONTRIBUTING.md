# Contributing to Innovayse.SDK

Thank you for helping. This repository holds the plugin contracts a hostpanel host loads plugins
through. It is MIT licensed, and by contributing you agree that your contribution is licensed
under the same terms.

## Issues

Open an issue for a bug in a contract, a base class or the package readme, or to propose a new
contract. Say what you are building and what the current contracts do not let you do.

## Pull requests

1. Fork the repository and branch from `main`.
2. Build and pack the project exactly as CI does:

   ```bash
   dotnet build Innovayse.SDK.csproj -c Release -warnaserror
   dotnet pack Innovayse.SDK.csproj -c Release -o artifacts
   ```

3. Document every type and member with XML doc comments (private ones too), and brace every
   block. An undocumented public member fails the build.
4. Raise `<Version>` in `Innovayse.SDK.csproj` when the change should be published (see
   "Versioning" in `README.md`) and add an entry to `CHANGELOG.md`.
5. Open the pull request against `main`.

## Changes the host must support

A new contract, a new manifest field or a new plugin type does nothing until the host knows about
it. Such a change also needs a pull request in
[innovayse/hostpanel](https://github.com/innovayse/hostpanel), which includes this repository as a
git submodule at `backend/src/Innovayse.SDK`: that pull request moves the submodule to your commit
and carries the host side. Link the two pull requests to each other.
