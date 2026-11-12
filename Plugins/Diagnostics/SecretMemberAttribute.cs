namespace Hostpanel.Sdk.Plugins.Diagnostics;

/// <summary>
/// Marks a property as holding a secret or personal value (a password, a token, an e-mail address, a gateway reference),
/// so the printed form of its record shows <see cref="SecretMask.Text"/> in its place. A positional record puts it on the
/// parameter as <c>[property: SecretMember]</c>, and the record is declared <c>partial</c>: a source generator that ships in
/// this package then adds the <c>PrintMembers</c> the generated <c>ToString</c> uses, so nobody writes or maintains it by
/// hand. Only the printed form changes: equality, serialization and the property itself stay as the record declares them.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
public sealed class SecretMemberAttribute : Attribute
{
}
