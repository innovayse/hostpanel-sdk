namespace Hostpanel.Sdk.Tests.Fakes;

/// <summary>An object whose printed form leaks the value it was given, to show that the secret checks of the contract helpers catch it.</summary>
/// <param name="Value">The value the printed form shows.</param>
internal sealed record LeakyResult(string Value);
