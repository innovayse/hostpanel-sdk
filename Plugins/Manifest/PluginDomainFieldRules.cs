using System.Globalization;
using System.Text.RegularExpressions;
using Hostpanel.Sdk.Plugins.Enums;

namespace Hostpanel.Sdk.Plugins.Manifest;

/// <summary>
/// The one implementation of the domain field rules, shared by the host and by a plugin's own tests: which fields a
/// manifest asks for a domain (<see cref="Match"/>) and whether the values given for them are acceptable
/// (<see cref="Validate"/>). The shapes a manifest may use for a field key, a zone and a pattern are here too, so the
/// package checker and the host judge a manifest by the same code.
/// </summary>
public static class PluginDomainFieldRules
{
    /// <summary>How long a pattern may run on one value, in milliseconds. A pattern that runs longer refuses the value.</summary>
    public const int PatternTimeoutMilliseconds = 1000;

    /// <summary>The longest label of a zone, in characters.</summary>
    private const int MaxZoneLabelLength = 63;

    /// <summary>The longest zone, in characters.</summary>
    private const int MaxZoneLength = 253;

    /// <summary>The format dates travel in.</summary>
    private const string DateFormat = "yyyy-MM-dd";

    /// <summary>How long a pattern may run on one value, <see cref="PatternTimeoutMilliseconds"/> as a time span.</summary>
    private static readonly TimeSpan PatternTimeout = TimeSpan.FromMilliseconds(PatternTimeoutMilliseconds);

    /// <summary>The punycode form of a unicode domain name is produced by this mapping.</summary>
    private static readonly IdnMapping Idn = new();

    /// <summary>
    /// Answers the fields a manifest asks for one domain. Of the sets whose holder is <see cref="PluginDomainHolder.Any"/> or
    /// the given holder, whose operations include the given one and one of whose zones the domain belongs to, the set with
    /// the longest matching zone wins and its fields are the answer, so a set for <c>co.uk</c> replaces the one for
    /// <c>uk</c>. A unicode domain name is read in its punycode form, and case is ignored. The first of two sets with equally
    /// long zones wins; the package checker refuses a manifest where that could happen.
    /// </summary>
    /// <param name="manifest">The manifest of the registrar plugin.</param>
    /// <param name="domainName">The fully qualified domain name, for example <c>example.co.uk</c>.</param>
    /// <param name="holder">The registrant kind, <see cref="PluginDomainHolder.Individual"/> or <see cref="PluginDomainHolder.Organization"/>; <see cref="PluginDomainHolder.Any"/> matches only the sets declared for any holder.</param>
    /// <param name="operation">The one operation about to run.</param>
    /// <returns>The fields in declared order; empty when no set applies or the winning set declares none.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="manifest"/> or <paramref name="domainName"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="operation"/> is <see cref="PluginDomainOperations.None"/>.</exception>
    public static IReadOnlyList<PluginDomainField> Match(
        PluginManifest manifest, string domainName, PluginDomainHolder holder, PluginDomainOperations operation)
    {
        ArgumentNullException.ThrowIfNull(manifest);
        ArgumentNullException.ThrowIfNull(domainName);
        if (operation == PluginDomainOperations.None)
        {
            throw new ArgumentOutOfRangeException(nameof(operation), operation, "An operation is needed to match domain fields.");
        }

        if (!TryNormalizeDomain(domainName, out var domain))
        {
            return [];
        }

        PluginDomainFieldSet? best = null;
        var bestLength = 0;
        foreach (var set in manifest.DomainFields)
        {
            var applies = (set.Holder == PluginDomainHolder.Any || set.Holder == holder)
                && (set.Operations & operation) == operation;
            if (!applies)
            {
                continue;
            }

            foreach (var zone in set.Zones)
            {
                if (zone.Length > bestLength && domain.EndsWith("." + zone, StringComparison.Ordinal))
                {
                    best = set;
                    bestLength = zone.Length;
                }
            }
        }

        return best?.Fields ?? [];
    }

    /// <summary>
    /// Checks the values given for the fields of a domain. Every attribute that is not a declared field is
    /// <see cref="PluginDomainFieldViolationReason.Unknown"/>. A declared field is checked in this order and reports the
    /// first reason that fails: a missing, empty or white space value is <see cref="PluginDomainFieldViolationReason.Required"/>
    /// for a required field and fine for an optional one; a select value that is none of the options is
    /// <see cref="PluginDomainFieldViolationReason.Option"/>; a date, number or web address in the wrong format is
    /// <see cref="PluginDomainFieldViolationReason.Format"/>; a value outside the length bounds is
    /// <see cref="PluginDomainFieldViolationReason.Length"/>; and one the pattern does not match in full (or whose pattern is
    /// invalid or runs too long) is <see cref="PluginDomainFieldViolationReason.Pattern"/>. Keys compare exactly, and values
    /// are not trimmed.
    /// </summary>
    /// <param name="fields">The fields <see cref="Match"/> answered for the domain.</param>
    /// <param name="attributes">The values by field key, as the <c>ExtendedAttributes</c> of a registrar context carry them.</param>
    /// <returns>One violation per offending field, in the order of <paramref name="fields"/>, then the unknown keys in key order; empty when the values are acceptable.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="fields"/> or <paramref name="attributes"/> is <see langword="null"/>.</exception>
    public static IReadOnlyList<PluginDomainFieldViolation> Validate(
        IReadOnlyList<PluginDomainField> fields, IReadOnlyDictionary<string, string> attributes)
    {
        ArgumentNullException.ThrowIfNull(fields);
        ArgumentNullException.ThrowIfNull(attributes);

        List<PluginDomainFieldViolation> violations = [];
        foreach (var field in fields)
        {
            attributes.TryGetValue(field.Key, out var value);
            var reason = CheckValue(field, value);
            if (reason is { } found)
            {
                violations.Add(new PluginDomainFieldViolation(field.Key, found));
            }
        }

        var declared = fields.Select(field => field.Key).ToHashSet(StringComparer.Ordinal);
        violations.AddRange(attributes.Keys
            .Where(key => !declared.Contains(key))
            .Order(StringComparer.Ordinal)
            .Select(key => new PluginDomainFieldViolation(key, PluginDomainFieldViolationReason.Unknown)));
        return violations;
    }

    /// <summary>
    /// Tells whether a domain field key is well formed: 1 to <see cref="PluginDomainField.MaxKeyLength"/> ASCII letters, digits,
    /// dots, underscores, colons and hyphens, starting with a letter or digit.
    /// </summary>
    /// <param name="key">The key to check.</param>
    /// <returns><see langword="true"/> when the key may name a domain field.</returns>
    public static bool IsValidKey(string? key)
    {
        if (string.IsNullOrEmpty(key) || key.Length > PluginDomainField.MaxKeyLength || !IsAsciiLetterOrDigit(key[0]))
        {
            return false;
        }

        return key.All(c => IsAsciiLetterOrDigit(c) || c is '.' or '_' or ':' or '-');
    }

    /// <summary>
    /// Tells whether a zone is well formed: dot separated labels of lower case ASCII letters, digits and hyphens (a punycode
    /// label such as <c>xn--p1ai</c> qualifies), each 1 to 63 characters and neither starting nor ending with a hyphen, with
    /// no leading or trailing dot, at most 253 characters in all.
    /// </summary>
    /// <param name="zone">The zone to check.</param>
    /// <returns><see langword="true"/> when the zone may appear in <see cref="PluginDomainFieldSet.Zones"/>.</returns>
    public static bool IsValidZone(string? zone)
    {
        if (string.IsNullOrEmpty(zone) || zone.Length > MaxZoneLength)
        {
            return false;
        }

        return zone.Split('.').All(IsValidZoneLabel);
    }

    /// <summary>
    /// Builds the expression a field's pattern stands for: the pattern must match the whole value, and it runs under a
    /// time limit. The pattern is compiled alone first, so one that only becomes valid by being wrapped (an unbalanced
    /// group) is refused.
    /// </summary>
    /// <param name="pattern">The pattern of a field.</param>
    /// <param name="timeout">How long the expression may run on one value.</param>
    /// <returns>The expression.</returns>
    /// <exception cref="ArgumentException"><paramref name="pattern"/> is not a valid regular expression or is longer than <see cref="PluginDomainField.MaxPatternLength"/>.</exception>
    public static Regex CreatePattern(string pattern, TimeSpan timeout)
    {
        ArgumentNullException.ThrowIfNull(pattern);
        if (pattern.Length > PluginDomainField.MaxPatternLength)
        {
            throw new ArgumentException($"A pattern has at most {PluginDomainField.MaxPatternLength} characters.", nameof(pattern));
        }

        _ = new Regex(pattern, RegexOptions.CultureInvariant, timeout);
        return new Regex($@"\A(?:{pattern})\z", RegexOptions.CultureInvariant, timeout);
    }

    /// <summary>Checks one value against its field.</summary>
    /// <param name="field">The field.</param>
    /// <param name="value">The value given for it, or <see langword="null"/> when none was.</param>
    /// <returns>The first reason the value is refused, or <see langword="null"/> when it is acceptable.</returns>
    private static PluginDomainFieldViolationReason? CheckValue(PluginDomainField field, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return field.Required ? PluginDomainFieldViolationReason.Required : null;
        }

        if (field.Type == PluginDomainFieldType.Select)
        {
            return field.Options.Any(option => string.Equals(option.Value, value, StringComparison.Ordinal))
                ? null
                : PluginDomainFieldViolationReason.Option;
        }

        if (!HasFormat(field.Type, value))
        {
            return PluginDomainFieldViolationReason.Format;
        }

        if ((field.MinLength is { } min && value.Length < min) || (field.MaxLength is { } max && value.Length > max))
        {
            return PluginDomainFieldViolationReason.Length;
        }

        return field.Pattern is not null && !MatchesPattern(field.Pattern, value) ? PluginDomainFieldViolationReason.Pattern : null;
    }

    /// <summary>Tells whether a value is in the format its kind needs.</summary>
    /// <param name="type">The kind of the field.</param>
    /// <param name="value">The value, not empty.</param>
    /// <returns><see langword="true"/> when the value is in the format; kinds with no format of their own always are.</returns>
    private static bool HasFormat(PluginDomainFieldType type, string value)
    {
        return type switch
        {
            PluginDomainFieldType.Date => DateOnly.TryParseExact(value, DateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out _),
            PluginDomainFieldType.Number => decimal.TryParse(
                value, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out _),
            PluginDomainFieldType.Url => Uri.TryCreate(value, UriKind.Absolute, out var uri)
                && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps),
            _ => true,
        };
    }

    /// <summary>Tells whether a pattern matches a whole value; a pattern that is invalid or runs too long matches nothing.</summary>
    /// <param name="pattern">The pattern of the field.</param>
    /// <param name="value">The value.</param>
    /// <returns><see langword="true"/> when the pattern matches the whole value.</returns>
    private static bool MatchesPattern(string pattern, string value)
    {
        try
        {
            return CreatePattern(pattern, PatternTimeout).IsMatch(value);
        }
        catch (Exception ex) when (ex is ArgumentException or RegexMatchTimeoutException)
        {
            // Failing closed: a pattern the checker would have refused never lets a value through.
            return false;
        }
    }

    /// <summary>Turns a domain name into the lower case punycode form the zones are written in.</summary>
    /// <param name="domainName">The domain name as given.</param>
    /// <param name="domain">The normalised name, without a trailing dot.</param>
    /// <returns><see langword="false"/> when the name is empty or cannot be written in punycode.</returns>
    private static bool TryNormalizeDomain(string domainName, out string domain)
    {
        domain = domainName.Trim().TrimEnd('.');
        if (domain.Length == 0)
        {
            return false;
        }

        try
        {
            domain = Idn.GetAscii(domain).ToLowerInvariant();
            return true;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    /// <summary>Tells whether one label of a zone is well formed.</summary>
    /// <param name="label">The label.</param>
    /// <returns><see langword="true"/> when it is 1 to 63 lower case letters, digits and inner hyphens.</returns>
    private static bool IsValidZoneLabel(string label)
    {
        if (label.Length == 0 || label.Length > MaxZoneLabelLength || label[0] == '-' || label[^1] == '-')
        {
            return false;
        }

        return label.All(c => c is >= 'a' and <= 'z' || c is >= '0' and <= '9' || c == '-');
    }

    /// <summary>Tells whether a character is an ASCII letter or digit.</summary>
    /// <param name="c">The character.</param>
    /// <returns><see langword="true"/> for <c>A-Z</c>, <c>a-z</c> and <c>0-9</c>.</returns>
    private static bool IsAsciiLetterOrDigit(char c)
    {
        return c is >= 'a' and <= 'z' or >= 'A' and <= 'Z' or >= '0' and <= '9';
    }
}
