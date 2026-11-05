using Hostpanel.Sdk.Packaging.Enums;
using Hostpanel.Sdk.Packaging.Signing;
using Hostpanel.Sdk.Packaging.Tests.Support;

namespace Hostpanel.Sdk.Packaging.Tests.Signing;

/// <summary>Tests of <see cref="PluginSigner"/> and <see cref="PluginSignatureValidator"/>: what is signed verifies, anything else does not.</summary>
public sealed class PluginSignerTests
{
    /// <summary>The key id used by most tests.</summary>
    private const string KeyId = "demo-key";

    /// <summary>Makes the trusted-key map a validator is given.</summary>
    /// <param name="keyId">The key id.</param>
    /// <param name="publicKey">The raw public key.</param>
    /// <returns>A one-entry map.</returns>
    private static Dictionary<string, byte[]> Trust(string keyId, byte[] publicKey)
    {
        return new Dictionary<string, byte[]>(StringComparer.Ordinal) { [keyId] = publicKey };
    }

    /// <summary>The first test vector of RFC 8032 (an empty message): the public key derives from the seed and the signature verifies.</summary>
    [Fact]
    public void Ed25519_Rfc8032VectorOne_Verifies()
    {
        var seed = Convert.FromHexString("9d61b19deffd5a60ba844af492ec2cc44449c5697b326919703bac031cae7f60");
        var publicKey = Convert.FromHexString("d75a980182b10ab7d54bfed3c964073a0ee172f3daa62325af021a68f707511a");
        var signature = Convert.FromHexString(
            "e5564300c360ac729086e2cc806e828a84877f1eb8e5d974d873e065224901555fb8821590a33bacc61e39701cf9b46bd25bf5f0595bbe24655141438e7a100b");

        PluginSigner.PublicKeyOf(seed).Should().Equal(publicKey);
        PluginSignatureValidator.VerifyPayload([], signature, publicKey).Should().BeTrue();
        PluginSignatureValidator.VerifyPayload([1], signature, publicKey).Should().BeFalse();
    }

    /// <summary>A generated key pair is 32 + 32 bytes and the public key is the one the private key derives.</summary>
    [Fact]
    public void GenerateKeyPair_ReturnsMatchingKeys()
    {
        var (privateKey, publicKey) = PluginSigner.GenerateKeyPair();

        privateKey.Should().HaveCount(PluginSigner.PrivateKeyLength);
        publicKey.Should().HaveCount(PluginSignatureValidator.PublicKeyLength);
        PluginSigner.PublicKeyOf(privateKey).Should().Equal(publicKey);
    }

    /// <summary>A private key that is not 32 bytes is refused.</summary>
    [Fact]
    public void PublicKeyOf_WrongLength_Throws()
    {
        var act = () => PluginSigner.PublicKeyOf(new byte[16]);

        act.Should().Throw<ArgumentException>();
    }

    /// <summary>A signed package verifies, and the file written is the one returned.</summary>
    [Fact]
    public void Verify_SignedPackage_IsAccepted()
    {
        using var folder = new TempFolder();
        GoldenPackage.Write(folder);
        var (privateKey, publicKey) = PluginSigner.GenerateKeyPair();

        var file = PluginSigner.Sign(folder.Path, KeyId, privateKey);

        PluginSignatureValidator.Verify(folder.Path, Trust(KeyId, publicKey)).Should().BeNull();
        PluginSignatureFile.Parse(File.ReadAllText(Path.Combine(folder.Path, PluginSignatureFile.FileName)))
            .Should().BeEquivalentTo(file);
    }

    /// <summary>Signing again after a change replaces the earlier signature, which verifies.</summary>
    [Fact]
    public void Sign_Twice_ReplacesTheSignature()
    {
        using var folder = new TempFolder();
        GoldenPackage.Write(folder);
        var (privateKey, publicKey) = PluginSigner.GenerateKeyPair();
        PluginSigner.Sign(folder.Path, KeyId, privateKey);

        folder.Write("lib/Demo.dll", "changed");
        PluginSigner.Sign(folder.Path, KeyId, privateKey);

        PluginSignatureValidator.Verify(folder.Path, Trust(KeyId, publicKey)).Should().BeNull();
    }

    /// <summary>A file changed after signing makes the signature invalid.</summary>
    [Fact]
    public void Verify_ChangedFile_IsInvalid()
    {
        using var folder = new TempFolder();
        GoldenPackage.Write(folder);
        var (privateKey, publicKey) = PluginSigner.GenerateKeyPair();
        PluginSigner.Sign(folder.Path, KeyId, privateKey);

        folder.Write("lib/Demo.dll", "tampered");

        PluginSignatureValidator.Verify(folder.Path, Trust(KeyId, publicKey))!.Rule.Should().Be(PluginSignatureRule.Invalid);
    }

    /// <summary>A file added after signing makes the signature invalid.</summary>
    [Fact]
    public void Verify_AddedFile_IsInvalid()
    {
        using var folder = new TempFolder();
        GoldenPackage.Write(folder);
        var (privateKey, publicKey) = PluginSigner.GenerateKeyPair();
        PluginSigner.Sign(folder.Path, KeyId, privateKey);

        folder.Write("extra/payload.dll", "x");

        PluginSignatureValidator.Verify(folder.Path, Trust(KeyId, publicKey))!.Rule.Should().Be(PluginSignatureRule.Invalid);
    }

    /// <summary>A file removed after signing makes the signature invalid.</summary>
    [Fact]
    public void Verify_RemovedFile_IsInvalid()
    {
        using var folder = new TempFolder();
        GoldenPackage.Write(folder);
        var (privateKey, publicKey) = PluginSigner.GenerateKeyPair();
        PluginSigner.Sign(folder.Path, KeyId, privateKey);

        File.Delete(Path.Combine(folder.Path, "Zed.txt"));

        PluginSignatureValidator.Verify(folder.Path, Trust(KeyId, publicKey))!.Rule.Should().Be(PluginSignatureRule.Invalid);
    }

    /// <summary>A package signed by a key the validator was not told about is an unknown key, not an invalid signature.</summary>
    [Fact]
    public void Verify_KeyNotTrusted_IsUnknownKey()
    {
        using var folder = new TempFolder();
        GoldenPackage.Write(folder);
        var (privateKey, _) = PluginSigner.GenerateKeyPair();
        var (_, otherPublicKey) = PluginSigner.GenerateKeyPair();
        PluginSigner.Sign(folder.Path, KeyId, privateKey);

        PluginSignatureValidator.Verify(folder.Path, Trust("another-key", otherPublicKey))!.Rule.Should().Be(PluginSignatureRule.UnknownKey);
    }

    /// <summary>A signature cannot be presented under another trusted key id: the id is inside the signed payload.</summary>
    [Fact]
    public void Verify_SignaturePresentedUnderAnotherKeyId_IsInvalid()
    {
        using var folder = new TempFolder();
        GoldenPackage.Write(folder);
        var (privateKey, publicKey) = PluginSigner.GenerateKeyPair();
        var file = PluginSigner.Sign(folder.Path, KeyId, privateKey);

        File.WriteAllText(
            Path.Combine(folder.Path, PluginSignatureFile.FileName), new PluginSignatureFile("renamed-key", file.Signature).ToJson());
        var trusted = Trust("renamed-key", publicKey);

        PluginSignatureValidator.Verify(folder.Path, trusted)!.Rule.Should().Be(PluginSignatureRule.Invalid);
    }

    /// <summary>A package with no signature file is unsigned.</summary>
    [Fact]
    public void Verify_NoSignatureFile_IsUnsigned()
    {
        using var folder = new TempFolder();
        folder.Write("plugin.json", "{}");

        PluginSignatureValidator.Verify(folder.Path, new Dictionary<string, byte[]>())!.Rule.Should().Be(PluginSignatureRule.Unsigned);
    }

    /// <summary>Text that is not a signature, and a signature file over the size cap, are malformed.</summary>
    /// <param name="content">The content of the signature file.</param>
    [Theory]
    [InlineData("not json")]
    [InlineData("[]")]
    [InlineData("{\"keyId\":\"demo-key\"}")]
    [InlineData("{\"keyId\":\"Bad Key\",\"signature\":\"AAAA\"}")]
    [InlineData("{\"keyId\":\"demo-key\",\"signature\":\"AAAA\"}")]
    public void Verify_GarbageSignatureFile_IsMalformed(string content)
    {
        using var folder = new TempFolder();
        folder.Write("plugin.json", "{}");
        folder.Write(PluginSignatureFile.FileName, content);

        PluginSignatureValidator.Verify(folder.Path, new Dictionary<string, byte[]>())!.Rule.Should().Be(PluginSignatureRule.Malformed);
    }

    /// <summary>A signature file larger than the cap is malformed without being parsed.</summary>
    [Fact]
    public void Verify_OversizedSignatureFile_IsMalformed()
    {
        using var folder = new TempFolder();
        folder.Write("plugin.json", "{}");
        folder.Write(PluginSignatureFile.FileName, new string(' ', PluginSignatureFile.MaxFileBytes + 1));

        PluginSignatureValidator.Verify(folder.Path, new Dictionary<string, byte[]>())!.Rule.Should().Be(PluginSignatureRule.Malformed);
    }

    /// <summary>A public key of the wrong length is never a match.</summary>
    [Fact]
    public void VerifyPayload_WrongPublicKeyLength_Throws()
    {
        var act = () => PluginSignatureValidator.VerifyPayload([1], new byte[64], new byte[5]);

        act.Should().Throw<ArgumentException>();
    }

    /// <summary>A signature of the wrong length does not verify.</summary>
    [Fact]
    public void VerifyPayload_WrongSignatureLength_IsFalse()
    {
        var (_, publicKey) = PluginSigner.GenerateKeyPair();

        PluginSignatureValidator.VerifyPayload([1], new byte[10], publicKey).Should().BeFalse();
    }

    /// <summary>A trusted key that is not 32 bytes counts as no key: the signature is reported as an unknown key, not as invalid and not as an exception.</summary>
    /// <param name="length">The length of the configured key.</param>
    [Theory]
    [InlineData(0)]
    [InlineData(31)]
    [InlineData(33)]
    [InlineData(64)]
    public void Verify_TrustedKeyOfTheWrongLength_IsUnknownKey(int length)
    {
        using var folder = new TempFolder();
        GoldenPackage.Write(folder);
        var (privateKey, _) = PluginSigner.GenerateKeyPair();
        PluginSigner.Sign(folder.Path, KeyId, privateKey);

        var violation = PluginSignatureValidator.Verify(folder.Path, Trust(KeyId, new byte[length]));

        violation!.Rule.Should().Be(PluginSignatureRule.UnknownKey);
    }

    /// <summary>A <c>plugin.sig</c> that is a symbolic link cannot be verified: the package holds a link, so it is malformed.</summary>
    [Fact]
    public void Verify_SignatureFileThatIsALink_IsMalformed()
    {
        using var folder = new TempFolder();
        using var outside = new TempFolder();
        GoldenPackage.Write(folder);
        var (privateKey, publicKey) = PluginSigner.GenerateKeyPair();
        PluginSigner.Sign(folder.Path, KeyId, privateKey);
        var real = Path.Combine(outside.Path, "real.sig");
        File.Move(Path.Combine(folder.Path, PluginSignatureFile.FileName), real);
        if (!SymbolicLinks.TryCreateFileLink(Path.Combine(folder.Path, PluginSignatureFile.FileName), real))
        {
            // The system does not allow links to this account: there is no link to refuse.
            return;
        }

        var violation = PluginSignatureValidator.Verify(folder.Path, Trust(KeyId, publicKey));

        violation!.Rule.Should().Be(PluginSignatureRule.Malformed);
    }

    /// <summary>A symbolic link anywhere inside the package makes it unverifiable, even though the signature was made before the link was added.</summary>
    [Fact]
    public void Verify_PackageWithAnInnerLink_IsMalformed()
    {
        using var folder = new TempFolder();
        using var outside = new TempFolder();
        GoldenPackage.Write(folder);
        var (privateKey, publicKey) = PluginSigner.GenerateKeyPair();
        PluginSigner.Sign(folder.Path, KeyId, privateKey);
        var secret = outside.Write("secret.txt", "outside the package");
        if (!SymbolicLinks.TryCreateFileLink(Path.Combine(folder.Path, "lib", "link.txt"), secret))
        {
            // The system does not allow links to this account: there is no link to refuse.
            return;
        }

        var violation = PluginSignatureValidator.Verify(folder.Path, Trust(KeyId, publicKey));

        violation!.Rule.Should().Be(PluginSignatureRule.Malformed);
    }

    /// <summary>Signing a package that holds a link is refused, so a link can never be signed over.</summary>
    [Fact]
    public void Sign_PackageWithAnInnerLink_Throws()
    {
        using var folder = new TempFolder();
        using var outside = new TempFolder();
        GoldenPackage.Write(folder);
        var secret = outside.Write("secret.txt", "outside the package");
        if (!SymbolicLinks.TryCreateFileLink(Path.Combine(folder.Path, "lib", "link.txt"), secret))
        {
            // The system does not allow links to this account: there is no link to refuse.
            return;
        }
        var (privateKey, _) = PluginSigner.GenerateKeyPair();

        var act = () => PluginSigner.Sign(folder.Path, KeyId, privateKey);

        act.Should().Throw<InvalidDataException>();
    }

    /// <summary>Signing leaves no temporary file in the package, and a failed signing leaves the earlier signature as it was.</summary>
    [Fact]
    public void Sign_LeavesNoTemporaryFile_AndKeepsTheEarlierSignatureOnFailure()
    {
        using var folder = new TempFolder();
        GoldenPackage.Write(folder);
        var (privateKey, publicKey) = PluginSigner.GenerateKeyPair();
        PluginSigner.Sign(folder.Path, KeyId, privateKey);
        var before = File.ReadAllText(Path.Combine(folder.Path, PluginSignatureFile.FileName));

        var act = () => PluginSigner.Sign(folder.Path, "Not A Valid Key", privateKey);

        act.Should().Throw<InvalidDataException>();
        File.ReadAllText(Path.Combine(folder.Path, PluginSignatureFile.FileName)).Should().Be(before);
        Directory.GetFiles(folder.Path, "*.tmp", SearchOption.AllDirectories).Should().BeEmpty();
        PluginSignatureValidator.Verify(folder.Path, Trust(KeyId, publicKey)).Should().BeNull();
    }

    /// <summary>A temporary file left by an earlier run that died is removed first, so it is not listed in the signed payload.</summary>
    [Fact]
    public void Sign_LeftoverTemporaryFile_IsRemovedAndNotSigned()
    {
        using var folder = new TempFolder();
        GoldenPackage.Write(folder);
        var leftover = folder.Write(PluginSignatureFile.FileName + ".tmp", "half written");
        var (privateKey, publicKey) = PluginSigner.GenerateKeyPair();

        PluginSigner.Sign(folder.Path, KeyId, privateKey);

        File.Exists(leftover).Should().BeFalse();
        PluginSignatureValidator.Verify(folder.Path, Trust(KeyId, publicKey)).Should().BeNull();
    }

    /// <summary>A call that is refused for its arguments leaves a file named like the temporary one untouched.</summary>
    /// <param name="keyId">A key id that is not valid.</param>
    [Theory]
    [InlineData("")]
    [InlineData("Not Valid")]
    public void Sign_InvalidKeyId_DoesNotDeleteTheTemporaryName(string keyId)
    {
        using var folder = new TempFolder();
        GoldenPackage.Write(folder);
        var temporary = folder.Write("plugin.sig.tmp", "keep");
        var (privateKey, _) = PluginSigner.GenerateKeyPair();

        var act = () => PluginSigner.Sign(folder.Path, keyId, privateKey);

        act.Should().Throw<InvalidDataException>();
        File.Exists(temporary).Should().BeTrue();
    }

    /// <summary>A private key of the wrong length is refused before a file named like the temporary one is removed.</summary>
    [Fact]
    public void Sign_WrongKeyLength_DoesNotDeleteTheTemporaryName()
    {
        using var folder = new TempFolder();
        GoldenPackage.Write(folder);
        var temporary = folder.Write("plugin.sig.tmp", "keep");

        var act = () => PluginSigner.Sign(folder.Path, KeyId, new byte[16]);

        act.Should().Throw<ArgumentException>();
        File.Exists(temporary).Should().BeTrue();
    }

    /// <summary>An empty or missing package folder is refused with the documented exceptions.</summary>
    [Fact]
    public void Sign_EmptyOrMissingFolder_Throws()
    {
        using var folder = new TempFolder();
        var (privateKey, _) = PluginSigner.GenerateKeyPair();

        var empty = () => PluginSigner.Sign(string.Empty, KeyId, privateKey);
        var missing = () => PluginSigner.Sign(Path.Combine(folder.Path, "nope"), KeyId, privateKey);

        empty.Should().Throw<ArgumentException>();
        missing.Should().Throw<DirectoryNotFoundException>();
    }

    /// <summary>A valid call removes a leftover temporary file, so it is not signed into the payload.</summary>
    [Fact]
    public void Sign_LeftoverTemporaryFile_IsRemovedAndPackageVerifies()
    {
        using var folder = new TempFolder();
        GoldenPackage.Write(folder);
        var temporary = folder.Write("plugin.sig.tmp", "stale");
        var (privateKey, publicKey) = PluginSigner.GenerateKeyPair();

        PluginSigner.Sign(folder.Path, KeyId, privateKey);

        File.Exists(temporary).Should().BeFalse();
        PluginSignatureValidator.Verify(folder.Path, Trust(KeyId, publicKey)).Should().BeNull();
    }
}
