using Hostpanel.Sdk.Plugins.Enums;
using Hostpanel.Sdk.Plugins.StoredCredentials;

namespace Hostpanel.Sdk.Tests.StoredCredentials;

/// <summary>Tests of the kind and the optional expiry of <see cref="SavedMethod"/>.</summary>
public sealed class SavedMethodKindTests
{
    /// <summary>A method built the old way, with six arguments, binds and is a card.</summary>
    [Fact]
    public void SavedMethod_BuiltTheOldWay_IsACard()
    {
        var method = new SavedMethod("pm_1", "visa", "4242", 12, 2030, true);

        method.Kind.Should().Be(SavedMethodKind.Card);
        method.HasExpiry.Should().BeTrue();
    }

    /// <summary>The constructor keeps the parameters it had in 3.10.0, so a stored credential plugin compiled against it still binds and does not fail with a missing method.</summary>
    [Fact]
    public void SavedMethod_KeepsItsOriginalConstructor()
    {
        var constructor = typeof(SavedMethod).GetConstructors().Single();

        constructor.GetParameters().Select(parameter => parameter.ParameterType).Should().Equal(
            typeof(string), typeof(string), typeof(string), typeof(int), typeof(int), typeof(bool));
    }

    /// <summary>A bank account carries its kind and has no expiry.</summary>
    [Fact]
    public void SavedMethod_BankAccount_KeepsItsKindAndHasNoExpiry()
    {
        var method = new SavedMethod("ba_1", null, "6789", 0, 0, false) { Kind = SavedMethodKind.BankAccount };

        method.Kind.Should().Be(SavedMethodKind.BankAccount);
        method.HasExpiry.Should().BeFalse();
    }

    /// <summary>An expiry needs both the month and the year; one alone is not an expiry.</summary>
    /// <param name="month">The expiry month.</param>
    /// <param name="year">The expiry year.</param>
    [Theory]
    [InlineData(0, 2030)]
    [InlineData(6, 0)]
    public void SavedMethod_HalfAnExpiry_HasNone(int month, int year)
    {
        new SavedMethod("pm_1", null, null, month, year, false).HasExpiry.Should().BeFalse();
    }
}
