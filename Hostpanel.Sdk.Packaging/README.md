# Hostpanel.Sdk.Packaging

The checks a plugin package must pass, in one MIT licensed package, so the host that installs a package and the
author who publishes it run the same code.

- `Hostpanel.Sdk.Packaging.Signing`: `PluginSignatureFile`, `PluginSignaturePayload` (the exact v1 payload),
  `PluginSigner` (key generation and signing), `PluginSignatureValidator` (verification) and `TrustedKeyId`.
- `Hostpanel.Sdk.Packaging.Naming`: `PluginIdentifier`, `PluginPageIdentifier`, `PluginVersion` and
  `PluginAssemblyNames` (the assembly names a package may not carry).
- `Hostpanel.Sdk.Packaging.Archive`: `PluginArchiveGuard` (entry count, uncompressed size, path traversal).
- `Hostpanel.Sdk.Packaging.Icons`: `PluginIconChecker`.
- `Hostpanel.Sdk.Packaging.Manifest`: `PluginManifestChecker` (the rules that need no host state).

Every refusal is an enum value (`Hostpanel.Sdk.Packaging.Enums`) with a detail text that names a package-relative
path or a member of the manifest, never an absolute path of the machine it ran on.

## Signing in a release pipeline

```csharp
var (privateKey, publicKey) = PluginSigner.GenerateKeyPair();
PluginSigner.Sign(packageDirectory, "my-key-id", privateKey);
```

Sign the folder that will be zipped, after its last change: any later change to any file makes the signature invalid.

## The payload header is a wire identifier

The first line of the signed payload, `PluginSignaturePayload.Header` (`innovayse-plugin-signature-v1`), is a fixed
identifier of the signature format, not a product name to be renamed or localised. Every signature ever made covers
these exact bytes, so changing the string would invalidate all of them. A new payload format gets a new header
(`...-v2`) and a new reader; the v1 text stays as it is.
