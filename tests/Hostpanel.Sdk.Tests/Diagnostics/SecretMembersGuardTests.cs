using System.Reflection;
using Hostpanel.Sdk.Plugins.Diagnostics;

namespace Hostpanel.Sdk.Tests.Diagnostics;

/// <summary>
/// Guards that no record of the SDK prints a secret or personal value, and that none lists its members by hand. They scan
/// the whole SDK assembly, so a record or a member added later is covered without a change here. The same rules run over
/// planted records that break them, so each guard is shown to fail and not only to pass.
/// </summary>
public sealed class SecretMembersGuardTests
{
    /// <summary>The records of the SDK assembly.</summary>
    private static IReadOnlyList<Type> SdkRecords => SecretPrintingRules.RecordsOf(typeof(SecretMask).Assembly);

    /// <summary>The planted records that break the rules.</summary>
    private static IReadOnlyList<Type> Planted => typeof(PlantedRecords).GetNestedTypes(BindingFlags.NonPublic | BindingFlags.Public);

    /// <summary>
    /// A <c>PrintMembers</c> is either the one the compiler synthesizes or the one the generator writes. Anything else was
    /// typed out, lists the members itself and goes stale when one is added.
    /// </summary>
    [Fact]
    public void NoRecordHasAHandWrittenPrintMembers()
    {
        SecretPrintingRules.HandWrittenPrintMembers(SdkRecords)
            .Should().BeEmpty("PrintMembers is written by the source generator from [SecretMember], never by hand");
    }

    /// <summary>No record overrides <c>ToString</c> by hand: the compiler marks the method it generates, so one without the mark was typed.</summary>
    [Fact]
    public void NoRecordOverridesToStringByHand()
    {
        SecretPrintingRules.HandWrittenToString(SdkRecords).Should().BeEmpty("a record prints through its generated ToString");
    }

    /// <summary>
    /// The generator only masks what is marked, so a member named after a credential that is not marked would print in
    /// clear, whatever its type. Such a member must carry <see cref="SecretMemberAttribute"/>.
    /// </summary>
    [Fact]
    public void EveryCredentialNamedMember_IsMarkedSecret()
    {
        SecretPrintingRules.UnmarkedCredentials(SdkRecords).Should().BeEmpty("a credential named member needs [property: SecretMember]");
    }

    /// <summary>The guard for a typed out <c>PrintMembers</c> fails on a record that has one.</summary>
    [Fact]
    public void HandWrittenPrintMembersGuard_FailsOnAPlantedRecord()
    {
        SecretPrintingRules.HandWrittenPrintMembers(Planted).Should().Equal(nameof(PlantedRecords.HandWrittenPrintMembers));
    }

    /// <summary>The guard for a typed out <c>ToString</c> fails on a record that has one.</summary>
    [Fact]
    public void HandWrittenToStringGuard_FailsOnAPlantedRecord()
    {
        SecretPrintingRules.HandWrittenToString(Planted).Should().Equal(nameof(PlantedRecords.HandWrittenToString));
    }

    /// <summary>The guard for unmarked credentials fails on text, bytes and a wrapper, and leaves a flag alone.</summary>
    [Fact]
    public void UnmarkedCredentialsGuard_FailsOnPlantedMembersOfAnyType()
    {
        SecretPrintingRules.UnmarkedCredentials(Planted).Should().BeEquivalentTo(
            $"{nameof(PlantedRecords.UnmarkedCredentials)}.Password",
            $"{nameof(PlantedRecords.UnmarkedCredentials)}.PrivateKey",
            $"{nameof(PlantedRecords.UnmarkedCredentials)}.ApiToken");
    }

    /// <summary>
    /// Prints a sample of every record that has a marked member. The secret values never show, the mask does, and every
    /// other member still prints, so the generated code drops nothing.
    /// </summary>
    [Fact]
    public void EveryRecordWithASecretMember_PrintsItMaskedAndTheRestInClear()
    {
        var withSecrets = SdkRecords.Where(type => SecretPrintingRules.MarkedProperties(type).Length > 0).ToList();
        withSecrets.Should().NotBeEmpty();

        foreach (var type in withSecrets)
        {
            var sample = RecordSamples.Create(type);
            var printed = sample.ToString()!;

            foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                var value = property.GetValue(sample);
                if (property.IsDefined(typeof(SecretMemberAttribute), true))
                {
                    printed.Should().Contain($"{property.Name} = {SecretMask.Text}", $"{type.Name}.{property.Name} is secret");
                    if (value is string text)
                    {
                        printed.Should().NotContain(text, $"{type.Name}.{property.Name} is secret");
                    }
                }
                else if (value is string text)
                {
                    printed.Should().Contain($"{property.Name} = {text}", $"{type.Name}.{property.Name} is not secret");
                }
            }
        }
    }
}
