using Hostpanel.Sdk.Packaging.Signing;

namespace Hostpanel.Sdk.Packaging.Tests.Signing;

/// <summary>Tests of <see cref="TrustedKeyId"/>: the shape of a signing key id.</summary>
public sealed class TrustedKeyIdTests
{
    /// <summary>Key ids are lower case words of letters, digits, dot, hyphen and underscore, 1 to 64 characters, with nothing after them.</summary>
    /// <param name="keyId">The candidate.</param>
    /// <param name="expected">Whether it is valid.</param>
    [Theory]
    [InlineData("a", true)]
    [InlineData("demo-key", true)]
    [InlineData("v1.2_x", true)]
    [InlineData("0abc", true)]
    [InlineData("", false)]
    [InlineData("-abc", false)]
    [InlineData(".abc", false)]
    [InlineData("Abc", false)]
    [InlineData("a b", false)]
    [InlineData("abc\n", false)]
    public void IsValid_FollowsTheShape(string keyId, bool expected)
    {
        TrustedKeyId.IsValid(keyId).Should().Be(expected);
    }

    /// <summary>A key id is at most 64 characters, and a missing one is not valid.</summary>
    [Fact]
    public void IsValid_BoundsTheLength()
    {
        TrustedKeyId.MaxLength.Should().Be(64);
        TrustedKeyId.IsValid(new string('a', TrustedKeyId.MaxLength)).Should().BeTrue();
        TrustedKeyId.IsValid(new string('a', TrustedKeyId.MaxLength + 1)).Should().BeFalse();
        TrustedKeyId.IsValid(null).Should().BeFalse();
    }

    /// <summary>EnsureValid accepts a valid key id and refuses every other with the one message the signing code gives.</summary>
    /// <param name="keyId">The candidate.</param>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("Upper")]
    [InlineData("two words")]
    public void EnsureValid_RefusesAnInvalidKeyId(string? keyId)
    {
        var ensure = () => TrustedKeyId.EnsureValid(keyId);

        ensure.Should().ThrowExactly<InvalidDataException>().WithMessage("The key id is not a valid key id.");
    }

    /// <summary>EnsureValid lets a valid key id through.</summary>
    [Fact]
    public void EnsureValid_AcceptsAValidKeyId()
    {
        var ensure = () => TrustedKeyId.EnsureValid("demo-key.1");

        ensure.Should().NotThrow();
    }
}
