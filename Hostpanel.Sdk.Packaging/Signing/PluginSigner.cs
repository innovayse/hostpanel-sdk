using System.Security.Cryptography;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Crypto.Signers;

namespace Hostpanel.Sdk.Packaging.Signing;

/// <summary>
/// Signs a plugin package. A host never signs anything: this is for whoever publishes a plugin, in a release
/// pipeline or a signing tool, and for tests. The private key is held by the signer, outside any source repository,
/// and is never written into a package.
/// </summary>
public static class PluginSigner
{
    /// <summary>The length of an Ed25519 private key (its seed) in bytes.</summary>
    public const int PrivateKeyLength = 32;

    /// <summary>The suffix of the temporary file the signature is written to before it replaces <c>plugin.sig</c>.</summary>
    private const string TemporarySuffix = ".tmp";

    /// <summary>Makes a new key pair.</summary>
    /// <returns>The 32-byte private key and the matching 32-byte public key.</returns>
    public static (byte[] PrivateKey, byte[] PublicKey) GenerateKeyPair()
    {
        var privateKey = RandomNumberGenerator.GetBytes(PrivateKeyLength);
        return (privateKey, PublicKeyOf(privateKey));
    }

    /// <summary>Derives the public key of a private key.</summary>
    /// <param name="privateKey">The 32-byte private key.</param>
    /// <returns>The 32-byte public key.</returns>
    /// <exception cref="ArgumentException">The private key is not 32 bytes.</exception>
    public static byte[] PublicKeyOf(byte[] privateKey)
    {
        return PrivateParameters(privateKey).GeneratePublicKey().GetEncoded();
    }

    /// <summary>
    /// Signs the package in a folder and writes <c>plugin.sig</c> at its root, replacing an earlier one. Sign only
    /// after every other file is final: a change to any file afterwards makes the signature invalid. A file named
    /// <c>plugin.sig.tmp</c> in the package folder is removed first, as the leftover of an earlier run that died.
    /// </summary>
    /// <param name="packageDir">The package root: the folder holding <c>plugin.json</c>.</param>
    /// <param name="keyId">The id the host's trusted key list knows this key by.</param>
    /// <param name="privateKey">The 32-byte private key.</param>
    /// <returns>The signature that was written.</returns>
    /// <exception cref="InvalidDataException">The key id is not valid, or a file name in the package cannot be signed.</exception>
    /// <exception cref="ArgumentException">The package folder is empty, or the private key is not 32 bytes.</exception>
    /// <exception cref="DirectoryNotFoundException">The package folder does not exist.</exception>
    /// <exception cref="UnauthorizedAccessException">The leftover temporary file or the signature file cannot be removed or written.</exception>
    public static PluginSignatureFile Sign(string packageDir, string keyId, byte[] privateKey)
    {
        // Every argument is checked before anything on disk is touched, so a refused call never deletes a file.
        ArgumentException.ThrowIfNullOrEmpty(packageDir);
        if (!Directory.Exists(packageDir))
        {
            throw new DirectoryNotFoundException("The package folder does not exist.");
        }

        if (!TrustedKeyId.IsValid(keyId))
        {
            throw new InvalidDataException("The key id is not a valid key id.");
        }

        var parameters = PrivateParameters(privateKey);

        // A temporary file left by an earlier run that died between the write and the move would be listed in the payload
        // (only plugin.sig itself is skipped) and then deleted after signing, leaving a signature no one can verify.
        var leftover = Path.Combine(packageDir, PluginSignatureFile.FileName + TemporarySuffix);
        if (File.Exists(leftover))
        {
            File.Delete(leftover);
        }

        var payload = PluginSignaturePayload.Build(packageDir, keyId);

        var signer = new Ed25519Signer();
        signer.Init(forSigning: true, parameters);
        signer.BlockUpdate(payload, 0, payload.Length);

        var file = new PluginSignatureFile(keyId, signer.GenerateSignature());
        WriteAtomically(Path.Combine(packageDir, PluginSignatureFile.FileName), file.ToJson());
        return file;
    }

    /// <summary>
    /// Writes the signature file through a temporary file in the same folder and then moves it over the target, so a
    /// reader never sees half a signature and a failed write leaves the earlier signature in place.
    /// </summary>
    /// <param name="path">The full path of the signature file.</param>
    /// <param name="content">The text to store.</param>
    private static void WriteAtomically(string path, string content)
    {
        // The temporary file sits beside the target (a move inside one folder is atomic) and is removed even when the move
        // fails, because the payload lists every file but plugin.sig and a leftover would end up in the next signature.
        var temporary = path + TemporarySuffix;
        try
        {
            File.WriteAllText(temporary, content);
            File.Move(temporary, path, overwrite: true);
        }
        finally
        {
            if (File.Exists(temporary))
            {
                File.Delete(temporary);
            }
        }
    }

    /// <summary>Wraps raw private key bytes for the Ed25519 implementation.</summary>
    /// <param name="privateKey">The 32-byte private key.</param>
    /// <returns>The key parameters.</returns>
    /// <exception cref="ArgumentException">The private key is not 32 bytes.</exception>
    private static Ed25519PrivateKeyParameters PrivateParameters(byte[] privateKey)
    {
        ArgumentNullException.ThrowIfNull(privateKey);
        if (privateKey.Length != PrivateKeyLength)
        {
            throw new ArgumentException($"An Ed25519 private key is {PrivateKeyLength} bytes.", nameof(privateKey));
        }

        return new Ed25519PrivateKeyParameters(privateKey, 0);
    }
}
