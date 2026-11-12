using System.CodeDom.Compiler;
using System.Reflection;
using System.Runtime.CompilerServices;
using Hostpanel.Sdk.Plugins.Diagnostics;

namespace Hostpanel.Sdk.Tests.Diagnostics;

/// <summary>
/// The rules that keep a record from printing a secret, written once over any set of types. The SDK's own tests apply them to
/// the whole SDK assembly, and other tests apply them to planted types that break each rule, to prove they can fail.
/// </summary>
internal static class SecretPrintingRules
{
    /// <summary>The binding flags of a record's own <c>PrintMembers</c>, which is not public and not inherited.</summary>
    private const BindingFlags OwnPrintMembers = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;

    /// <summary>The name of the tool that writes the <c>PrintMembers</c> of the records with a secret member.</summary>
    private const string GeneratorName = "Hostpanel.Sdk.Generators";

    /// <summary>Words in a member's name that say it holds a credential, compared without regard to case. The one list every rule uses.</summary>
    internal static readonly string[] CredentialWords =
    [
        "PrivateKey", "Password", "Passphrase", "Secret", "Token", "ApiKey", "AccessKey", "AccessHash", "SigningKey", "LicenseKey", "EppCode", "AuthCode",
    ];

    /// <summary>Every record declared in an assembly, public or not.</summary>
    /// <param name="assembly">The assembly.</param>
    /// <returns>The record types.</returns>
    internal static IReadOnlyList<Type> RecordsOf(Assembly assembly)
    {
        return assembly.GetTypes().Where(IsRecord).ToList();
    }

    /// <summary>Tells whether a type is a record: the compiler gives every record a clone method.</summary>
    /// <param name="type">The type.</param>
    /// <returns><see langword="true"/> for a record class.</returns>
    internal static bool IsRecord(Type type)
    {
        return type.IsClass && type.GetMethod("<Clone>$", BindingFlags.Instance | BindingFlags.Public) is not null;
    }

    /// <summary>The public properties of a record that carry <see cref="SecretMemberAttribute"/>.</summary>
    /// <param name="type">The record.</param>
    /// <returns>The marked properties, empty when there are none.</returns>
    internal static PropertyInfo[] MarkedProperties(Type type)
    {
        return type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(property => property.IsDefined(typeof(SecretMemberAttribute), true))
            .ToArray();
    }

    /// <summary>
    /// Finds the records whose <c>PrintMembers</c> is neither the one the compiler synthesizes nor the one the generator
    /// writes: it was typed out, lists the members itself and goes stale when one is added.
    /// </summary>
    /// <param name="records">The records to check.</param>
    /// <returns>The names of the records that break the rule.</returns>
    internal static List<string> HandWrittenPrintMembers(IEnumerable<Type> records)
    {
        return records
            .Select(type => (type, method: type.GetMethod("PrintMembers", OwnPrintMembers)))
            .Where(pair => pair.method is not null)
            .Where(pair => !pair.method!.IsDefined(typeof(CompilerGeneratedAttribute), false) && !IsFromTheGenerator(pair.method))
            .Select(pair => pair.type.Name)
            .ToList();
    }

    /// <summary>Finds the records that override <c>ToString</c> by hand: the compiler marks the method it generates, so one without the mark was typed.</summary>
    /// <param name="records">The records to check.</param>
    /// <returns>The names of the records that break the rule.</returns>
    internal static List<string> HandWrittenToString(IEnumerable<Type> records)
    {
        return records
            .Where(type => type.GetMethod("ToString", BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly, Type.EmptyTypes) is { } method
                && !method.IsDefined(typeof(CompilerGeneratedAttribute), false))
            .Select(type => type.Name)
            .ToList();
    }

    /// <summary>
    /// Finds the members named after a credential that are not marked. The generator masks only what is marked, so such a
    /// member would print in clear. A flag, a number, a date or an enum named after a credential only describes it and is not
    /// a credential; every other type (text, bytes, a wrapper) can hold one.
    /// </summary>
    /// <param name="records">The records to check.</param>
    /// <returns>The <c>Type.Member</c> names that break the rule.</returns>
    internal static List<string> UnmarkedCredentials(IEnumerable<Type> records)
    {
        return records
            .SelectMany(type => type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly), (type, property) => (type, property))
            .Where(pair => CanHoldACredential(pair.property.PropertyType))
            .Where(pair => CredentialWords.Any(word => pair.property.Name.Contains(word, StringComparison.OrdinalIgnoreCase)))
            .Where(pair => !pair.property.IsDefined(typeof(SecretMemberAttribute), true))
            .Select(pair => $"{pair.type.Name}.{pair.property.Name}")
            .ToList();
    }

    /// <summary>Tells whether a type could hold a credential: anything but a flag, a number, a date, a duration, an identifier or an enum.</summary>
    /// <param name="type">The member's type.</param>
    /// <returns><see langword="true"/> for text, bytes and every other reference or wrapper type.</returns>
    private static bool CanHoldACredential(Type type)
    {
        var inner = Nullable.GetUnderlyingType(type) ?? type;
        return !(inner.IsPrimitive || inner.IsEnum || inner == typeof(decimal) || inner == typeof(DateTime)
            || inner == typeof(DateTimeOffset) || inner == typeof(TimeSpan) || inner == typeof(Guid));
    }

    /// <summary>Tells whether the source generator wrote a method.</summary>
    /// <param name="method">The method.</param>
    /// <returns><see langword="true"/> when it carries the generator's <c>GeneratedCode</c> mark.</returns>
    private static bool IsFromTheGenerator(MethodInfo method)
    {
        return method.GetCustomAttribute<GeneratedCodeAttribute>() is { } mark && mark.Tool == GeneratorName;
    }
}
