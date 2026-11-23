using Hostpanel.Sdk.Packaging.Enums;

namespace Hostpanel.Sdk.Packaging;

/// <summary>The result of <see cref="PackageFileReader.Read"/>: how the read ended and, when it succeeded, what the file holds.</summary>
/// <param name="Outcome">How the read ended.</param>
/// <param name="Content">The file bytes; <see langword="null"/> unless <paramref name="Outcome"/> is <see cref="PackageFileOutcome.Read"/>.</param>
internal sealed record PackageFileRead(PackageFileOutcome Outcome, byte[]? Content);
