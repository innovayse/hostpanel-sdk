using System.CodeDom.Compiler;
using System.Reflection;
using System.Runtime.CompilerServices;
using Hostpanel.Sdk.Plugins.Diagnostics;

namespace Hostpanel.Sdk.Tests.Diagnostics;

/// <summary>
/// Guards that no record of the SDK prints a secret or personal value, and that none lists its members by hand. They scan
/// the whole SDK assembly, so a record or a member added later is covered without a change here.
/// </summary>
public sealed class SecretMembersGuardTests
{
    /// <summary>The binding flags of a record's own <c>PrintMembers</c>, which is not public and not inherited.</summary>
    private const BindingFlags OwnPrintMembers = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;

    /// <summary>The name of the tool that writes the <c>PrintMembers</c> of the records with a secret member.</summary>
    private const string GeneratorName = "Hostpanel.Sdk.Generators";

    /// <summary>Words in a member's name that say it holds a credential, compared without regard to case.</summary>
    private static readonly string[] CredentialWords =
    [
        "PrivateKey", "Password", "Passphrase", "Secret", "Token", "ApiKey", "AccessKey", "AccessHash", "SigningKey", "EppCode", "AuthCode",
    ];

    /// <summary>Every record the SDK declares, public or not.</summary>
    /// <returns>The record types.</returns>
    private static IEnumerable<Type> Records()
    {
        return typeof(SecretMask).Assembly.GetTypes()
            .Where(type => type.IsClass && type.GetMethod("<Clone>$", BindingFlags.Instance | BindingFlags.Public) is not null);
    }

    /// <summary>The public properties of a record that carry <see cref="SecretMemberAttribute"/>.</summary>
    /// <param name="type">The record.</param>
    /// <returns>The marked properties, empty when there are none.</returns>
    private static PropertyInfo[] MarkedProperties(Type type)
    {
        return type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(property => property.IsDefined(typeof(SecretMemberAttribute), true))
            .ToArray();
    }

    /// <summary>
    /// A <c>PrintMembers</c> is either the one the compiler synthesizes or the one the generator writes. Anything else was
    /// typed out, lists the members itself and goes stale when one is added.
    /// </summary>
    [Fact]
    public void NoRecordHasAHandWrittenPrintMembers()
    {
        var handWritten = Records()
            .Select(type => (type, method: type.GetMethod("PrintMembers", OwnPrintMembers)))
            .Where(pair => pair.method is not null)
            .Where(pair => !pair.method!.IsDefined(typeof(CompilerGeneratedAttribute), false) && !IsFromTheGenerator(pair.method))
            .Select(pair => pair.type.Name)
            .ToList();

        handWritten.Should().BeEmpty("PrintMembers is written by the source generator from [SecretMember], never by hand");
    }

    /// <summary>No record overrides <c>ToString</c> by hand: the compiler marks the method it generates, so one without the mark was typed.</summary>
    [Fact]
    public void NoRecordOverridesToStringByHand()
    {
        var handWritten = Records()
            .Where(type => type.GetMethod("ToString", BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly, Type.EmptyTypes) is { } method
                && !method.IsDefined(typeof(CompilerGeneratedAttribute), false))
            .Select(type => type.Name)
            .ToList();

        handWritten.Should().BeEmpty("a record prints through its generated ToString");
    }

    /// <summary>
    /// The generator only masks what is marked, so a string named after a credential that is not marked would print in
    /// clear. A member whose name is a credential word must carry <see cref="SecretMemberAttribute"/>.
    /// </summary>
    [Fact]
    public void EveryCredentialNamedStringMember_IsMarkedSecret()
    {
        var unmarked = Records()
            .SelectMany(type => type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly), (type, property) => (type, property))
            .Where(pair => pair.property.PropertyType == typeof(string))
            .Where(pair => CredentialWords.Any(word => pair.property.Name.Contains(word, StringComparison.OrdinalIgnoreCase)))
            .Where(pair => !pair.property.IsDefined(typeof(SecretMemberAttribute), true))
            .Select(pair => $"{pair.type.Name}.{pair.property.Name}")
            .ToList();

        unmarked.Should().BeEmpty("a credential named member needs [property: SecretMember]");
    }

    /// <summary>
    /// Prints a sample of every record that has a marked member. The secret values never show, the mask does, and every
    /// other member still prints, so the generated code drops nothing.
    /// </summary>
    [Fact]
    public void EveryRecordWithASecretMember_PrintsItMaskedAndTheRestInClear()
    {
        var withSecrets = Records().Where(type => MarkedProperties(type).Length > 0).ToList();
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

    /// <summary>Tells whether the source generator wrote a method.</summary>
    /// <param name="method">The method.</param>
    /// <returns><see langword="true"/> when it carries the generator's <c>GeneratedCode</c> mark.</returns>
    private static bool IsFromTheGenerator(MethodInfo method)
    {
        return method.GetCustomAttribute<GeneratedCodeAttribute>() is { } mark && mark.Tool == GeneratorName;
    }
}
