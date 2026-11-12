using Hostpanel.Sdk.Packaging.Enums;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Crypto.Signers;

namespace Hostpanel.Sdk.Packaging.Signing;

/// <summary>
/// Verifies the detached Ed25519 signature of a plugin package against a set of trusted public keys. It answers one
/// question, whether the package is signed by a trusted key and unchanged since; what a host does with the answer
/// (which origins are exempt, whether an unsigned package may be admitted anyway) is the host's policy and is not here.
/// </summary>
public static class PluginSignatureValidator
{
    /// <summary>The length of an Ed25519 public key in bytes.</summary>
    public const int PublicKeyLength = 32;

    /// <summary>Checks the signature of the package in a folder against the trusted keys.</summary>
    /// <param name="packageDir">The package root: the folder holding the manifest and the signature file.</param>
    /// <param name="trustedKeys">
    /// The trusted raw 32-byte public keys by key id. The lookup uses the dictionary's own comparer, so the caller decides
    /// whether an id is matched exactly or ignoring case; build it with <see cref="StringComparer.Ordinal"/> for an exact
    /// match, which is what key ids are written for (they are lower case). A key that is not 32 bytes counts as no key.
    /// </param>
    /// <returns>The check the package fails, or <see langword="null"/> when it is signed by a trusted key and unchanged since.</returns>
    public static PluginSignatureViolation? Verify(string packageDir, IReadOnlyDictionary<string, byte[]> trustedKeys)
    {
        ArgumentException.ThrowIfNullOrEmpty(packageDir);
        ArgumentNullException.ThrowIfNull(trustedKeys);

        var path = Path.Combine(packageDir, PluginSignatureFile.FileName);
        if (!File.Exists(path))
        {
            return new PluginSignatureViolation(PluginSignatureRule.Unsigned, $"{PluginSignatureFile.FileName} is missing");
        }

        try
        {
            if (new FileInfo(path).Length > PluginSignatureFile.MaxFileBytes)
            {
                return new PluginSignatureViolation(
                    PluginSignatureRule.Malformed, $"{PluginSignatureFile.FileName} is larger than {PluginSignatureFile.MaxFileBytes} bytes");
            }

            var file = PluginSignatureFile.Parse(File.ReadAllText(path));
            if (!trustedKeys.TryGetValue(file.KeyId, out var publicKey) || publicKey is null || publicKey.Length != PublicKeyLength)
            {
                return new PluginSignatureViolation(PluginSignatureRule.UnknownKey, $"key '{file.KeyId}' is not a trusted key");
            }

            var payload = PluginSignaturePayload.Build(packageDir, file.KeyId);
            return VerifyPayload(payload, file.Signature, publicKey)
                ? null
                : new PluginSignatureViolation(PluginSignatureRule.Invalid, $"the signature of key '{file.KeyId}' does not match the files of the package");
        }
        catch (Exception ex) when (ex is FormatException or InvalidDataException)
        {
            return new PluginSignatureViolation(PluginSignatureRule.Malformed, ex.Message);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // The type only: an I/O message carries the absolute path of the file.
            return new PluginSignatureViolation(PluginSignatureRule.Malformed, $"the package could not be read ({ex.GetType().Name})");
        }
    }

    /// <summary>Checks one Ed25519 signature over a payload.</summary>
    /// <param name="payload">The signed bytes, as <see cref="PluginSignaturePayload.Build"/> builds them.</param>
    /// <param name="signature">The 64-byte signature.</param>
    /// <param name="publicKey">The 32-byte raw public key.</param>
    /// <returns><see langword="true"/> when the signature is valid for the payload under the key.</returns>
    /// <exception cref="ArgumentException">The public key is not 32 bytes.</exception>
    public static bool VerifyPayload(byte[] payload, byte[] signature, byte[] publicKey)
    {
        ArgumentNullException.ThrowIfNull(payload);
        ArgumentNullException.ThrowIfNull(signature);
        ArgumentNullException.ThrowIfNull(publicKey);
        if (publicKey.Length != PublicKeyLength)
        {
            throw new ArgumentException($"An Ed25519 public key is {PublicKeyLength} bytes.", nameof(publicKey));
        }

        if (signature.Length != PluginSignatureFile.SignatureLength)
        {
            return false;
        }

        var verifier = new Ed25519Signer();
        verifier.Init(forSigning: false, new Ed25519PublicKeyParameters(publicKey, 0));
        verifier.BlockUpdate(payload, 0, payload.Length);
        return verifier.VerifySignature(signature);
    }
}
