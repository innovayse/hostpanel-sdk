using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Text.RegularExpressions;
using Hostpanel.Sdk.Packaging.Enums;
using Hostpanel.Sdk.Packaging.Naming;
using Hostpanel.Sdk.Plugins.Enums;
using Hostpanel.Sdk.Plugins.Manifest;
using Hostpanel.Sdk.Plugins.Slots;

namespace Hostpanel.Sdk.Packaging.Manifest;

/// <summary>
/// The structural manifest rules that need no host state, written once so a host checking an upload and an author
/// checking a package before publishing run the same code. <see cref="Validate"/> checks the manifest alone and
/// <see cref="ValidateSdkReference(string, int)"/> the assemblies beside it. Rules that need the host (whether the entry point
/// implements a contract, whether a view file exists, whether a file name clashes with another installed plugin) stay
/// with the host.
/// </summary>
public static partial class PluginManifestChecker
{
    /// <summary>The locale whose texts must define every text key a manifest uses; the other locales may fall back to it.</summary>
    private const string EnglishLocale = "en";

    /// <summary>The file pattern of the assemblies read in a plugin folder.</summary>
    private const string AssemblyPattern = "*.dll";

    /// <summary>The longest icon name a menu may declare, in characters; the bound is part of <see cref="IconNamePattern"/>.</summary>
    private const int MaxIconNameLength = 64;

    /// <summary>The name of the SDK assembly a plugin references, read from the assembly this package compiled against.</summary>
    private static readonly string SdkAssemblyName = typeof(PluginManifest).Assembly.GetName().Name!;

    /// <summary>The SDK major version of this package: the default for <see cref="ValidateSdkReference(string)"/>.</summary>
    private static readonly int SdkMajorVersion = typeof(PluginManifest).Assembly.GetName().Version!.Major;

    /// <summary>
    /// Matches a text key as the SDK documents it for <c>label</c> and <c>title</c> members: dot separated words
    /// such as <c>menu.reports</c>, with no spaces. Anything else is plain text that is shown as written. The end
    /// is <c>\z</c>, not <c>$</c>, because <c>$</c> also matches before a trailing newline.
    /// </summary>
    /// <returns>The compiled pattern.</returns>
    [GeneratedRegex(@"\A[A-Za-z][A-Za-z0-9_-]*(\.[A-Za-z0-9_-]+)+\z", RegexOptions.CultureInvariant, RegexLimits.TimeoutMilliseconds)]
    private static partial Regex TextKeyPattern();

    /// <summary>Matches a slot widget id: the same safe shape as a literal route segment, because the id is a key in a setting and a URL-safe reference.</summary>
    /// <returns>The compiled pattern.</returns>
    [GeneratedRegex(@"\A[a-z0-9][a-z0-9_-]*\z", RegexOptions.CultureInvariant, RegexLimits.TimeoutMilliseconds)]
    private static partial Regex WidgetIdPattern();

    /// <summary>
    /// Matches a menu icon name: 1 to <see cref="MaxIconNameLength"/> lowercase letters, digits and hyphens, so a name
    /// can only select an icon the panel already ships, never point at a URL or carry markup. <c>\z</c>, not <c>$</c>,
    /// so a trailing newline is refused too.
    /// </summary>
    /// <returns>The compiled pattern.</returns>
    [GeneratedRegex(@"\A[a-z0-9-]{1,64}\z", RegexOptions.CultureInvariant, RegexLimits.TimeoutMilliseconds)]
    private static partial Regex IconNamePattern();

    /// <summary>
    /// Checks the manifest alone: its version, id, permissions, route paths, text keys and slots. A version 1 manifest
    /// declares none of the blocks these rules look at, so it passes by having nothing to break.
    /// </summary>
    /// <param name="manifest">The manifest, already read.</param>
    /// <param name="options">What the host decides: the manifest versions it reads and the client permission names it defines.</param>
    /// <returns>The first rule the manifest breaks, or <see langword="null"/> when it meets them all.</returns>
    public static PluginManifestViolation? Validate(PluginManifest manifest, PluginManifestOptions options)
    {
        ArgumentNullException.ThrowIfNull(manifest);
        ArgumentNullException.ThrowIfNull(options);

        if (!options.SupportedManifestVersions.Contains(manifest.ManifestVersion))
        {
            return new PluginManifestViolation(
                PluginManifestRule.ManifestVersionUnknown,
                $"manifestVersion {manifest.ManifestVersion} is not one of {string.Join(", ", options.SupportedManifestVersions.Order())}");
        }

        if (!PluginIdentifier.IsValid(manifest.Id))
        {
            return new PluginManifestViolation(PluginManifestRule.PluginIdInvalid, "the manifest id is not a valid plugin id");
        }

        // The version decides upgrade, same and downgrade; a text that is not a strict version could slip past those
        // checks, so it is refused here.
        if (!PluginVersion.TryParse(manifest.Version, out _))
        {
            return new PluginManifestViolation(
                PluginManifestRule.VersionInvalid, "the manifest version is not a valid version: use major.minor.patch, for example 1.2.3");
        }

        return CheckRoutes(PluginArea.Client, manifest.Api.Client, options)
            ?? CheckRoutes(PluginArea.Admin, manifest.Api.Admin, options)
            ?? CheckCallbacks(manifest.Api, options)
            ?? CheckPages(PluginArea.Client, manifest.Ui.Client, options)
            ?? CheckPages(PluginArea.Admin, manifest.Ui.Admin, options)
            ?? CheckSlots(manifest.Ui.Slots, options)
            ?? CheckTags(manifest.Tags)
            ?? CheckHostVersions(manifest.MinHostVersion, manifest.MaxHostVersion)
            ?? CheckChangelog(manifest.Changelog)
            ?? CheckTextKeys(manifest);
    }

    /// <summary>Checks the assemblies in a package folder against the major version of the SDK this package was built with.</summary>
    /// <param name="pluginDirectory">The folder holding the plugin's assemblies.</param>
    /// <returns>The violation naming the first outdated or unreadable assembly, or <see langword="null"/> when none is.</returns>
    public static PluginManifestViolation? ValidateSdkReference(string pluginDirectory)
    {
        return ValidateSdkReference(pluginDirectory, SdkMajorVersion);
    }

    /// <summary>
    /// Checks the assemblies in a plugin folder: one built against an older SDK major version than the host's is
    /// refused up front, because loading it would fail with a type load exception that names nothing useful. The
    /// assembly metadata is read without loading it, so the plugin never reaches the default load context.
    /// </summary>
    /// <param name="pluginDirectory">The folder holding the plugin's assemblies.</param>
    /// <param name="hostSdkMajorVersion">The SDK major version the host provides.</param>
    /// <returns>The violation naming the first outdated or unreadable assembly, or <see langword="null"/> when none is.</returns>
    public static PluginManifestViolation? ValidateSdkReference(string pluginDirectory, int hostSdkMajorVersion)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(pluginDirectory);

        string[] assemblyPaths;
        try
        {
            // Listed inside the try: a folder that is missing or cannot be listed is as much a refusal as a file that cannot be read.
            assemblyPaths = Directory.EnumerateFiles(pluginDirectory, AssemblyPattern, SearchOption.TopDirectoryOnly).ToArray();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return new PluginManifestViolation(
                PluginManifestRule.AssemblyUnreadable, $"the plugin folder could not be listed ({ex.GetType().Name})");
        }

        foreach (var assemblyPath in assemblyPaths)
        {
            Version? referenced;
            try
            {
                referenced = ReadSdkReferenceVersion(assemblyPath);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                // A file that cannot be read is a refusal of this plugin, not a fault of the request; only the exception type is kept.
                return new PluginManifestViolation(
                    PluginManifestRule.AssemblyUnreadable, $"{Path.GetFileName(assemblyPath)} could not be read ({ex.GetType().Name})");
            }

            if (referenced is not null && referenced.Major < hostSdkMajorVersion)
            {
                return new PluginManifestViolation(
                    PluginManifestRule.SdkAssemblyOutdated,
                    $"{Path.GetFileName(assemblyPath)} was built against SDK {referenced.Major}.{referenced.Minor}, "
                    + $"this host provides SDK {hostSdkMajorVersion}; rebuild the plugin against the current SDK");
            }
        }

        return null;
    }

    /// <summary>Gets the English texts of a manifest, the locale that must define every text key.</summary>
    /// <param name="manifest">The manifest.</param>
    /// <returns>The texts by key, or <see langword="null"/> when the manifest ships no English locale.</returns>
    public static IReadOnlyDictionary<string, string>? EnglishTexts(PluginManifest manifest)
    {
        ArgumentNullException.ThrowIfNull(manifest);

        return manifest.Locales
            .FirstOrDefault(pair => string.Equals(pair.Key, EnglishLocale, StringComparison.OrdinalIgnoreCase)).Value;
    }

    /// <summary>Tells whether a label value is shaped like a text key (dot separated words, no spaces) rather than plain text.</summary>
    /// <param name="value">The label value.</param>
    /// <returns><see langword="true"/> when the value has the shape of a text key.</returns>
    public static bool IsTextKey(string value)
    {
        ArgumentNullException.ThrowIfNull(value);

        return TextKeyPattern().IsMatch(value);
    }

    /// <summary>Checks that a value taken as a text key is defined in the English texts; the one rule manifest and view labels share.</summary>
    /// <param name="where">Where the value is written, for the log.</param>
    /// <param name="key">The text key.</param>
    /// <param name="english">The English texts, or <see langword="null"/> when there are none.</param>
    /// <returns>The violation, or <see langword="null"/> when the key is defined.</returns>
    public static PluginManifestViolation? CheckKeyDefined(string where, string key, IReadOnlyDictionary<string, string>? english)
    {
        return english is null || !english.ContainsKey(key)
            ? new PluginManifestViolation(
                PluginManifestRule.LabelKeyMissing, $"{where} uses the text key '{key}', which the '{EnglishLocale}' texts do not define")
            : null;
    }

    /// <summary>Reads the version of the SDK that a managed assembly references, from its metadata only.</summary>
    /// <param name="assemblyPath">The assembly file.</param>
    /// <returns>The referenced SDK version, or <see langword="null"/> when the file is not a managed assembly or does not reference the SDK.</returns>
    /// <exception cref="IOException">The file cannot be opened or read.</exception>
    /// <exception cref="UnauthorizedAccessException">The caller may not read the file.</exception>
    private static Version? ReadSdkReferenceVersion(string assemblyPath)
    {
        try
        {
            using var stream = File.OpenRead(assemblyPath);
            using var peReader = new PEReader(stream);
            if (!peReader.HasMetadata)
            {
                return null;
            }

            var metadata = peReader.GetMetadataReader();
            foreach (var handle in metadata.AssemblyReferences)
            {
                var reference = metadata.GetAssemblyReference(handle);
                if (string.Equals(metadata.GetString(reference.Name), SdkAssemblyName, StringComparison.Ordinal))
                {
                    return reference.Version;
                }
            }

            return null;
        }
        catch (BadImageFormatException)
        {
            // A native library or a damaged file: it cannot have been built against the SDK, and loading it fails on its own terms.
            return null;
        }
    }

    /// <summary>Checks the routes one area declares: permission, path and uniqueness.</summary>
    /// <param name="area">The panel the routes belong to.</param>
    /// <param name="routes">The routes of the area.</param>
    /// <param name="options">What the host decides.</param>
    /// <returns>The first violation, or <see langword="null"/>.</returns>
    private static PluginManifestViolation? CheckRoutes(PluginArea area, IReadOnlyList<PluginRoute> routes, PluginManifestOptions options)
    {
        var seen = new HashSet<(PluginRouteMethod Method, string Template)>();
        foreach (var route in routes)
        {
            var where = $"api.{AreaMember(area)} {route.Method} '{route.Path}'";
            var violation = CheckPermission(area, route.Permission, where, options);
            if (violation is not null)
            {
                return violation;
            }

            if (!PluginRoutePath.TryNormalize(route.Path, out var template))
            {
                return PathViolation(where);
            }

            if (!seen.Add((route.Method, template)))
            {
                return new PluginManifestViolation(
                    PluginManifestRule.RouteDuplicate, $"{where} is declared more than once in the {AreaMember(area)} area");
            }
        }

        return null;
    }

    /// <summary>
    /// Checks the callback routes: the SDK's own rule first (<see cref="PluginApi.CallbackProblems"/>: no permission, no
    /// streaming), then the path rule and uniqueness routes have.
    /// </summary>
    /// <param name="api">The manifest's <c>api</c> block.</param>
    /// <param name="options">What the host decides.</param>
    /// <returns>The first violation, or <see langword="null"/>.</returns>
    private static PluginManifestViolation? CheckCallbacks(PluginApi api, PluginManifestOptions options)
    {
        var problems = api.CallbackProblems();
        if (problems.Count > 0)
        {
            return new PluginManifestViolation(PluginManifestRule.CallbackInvalid, $"api.callbacks: {problems[0]}");
        }

        return CheckRoutes(PluginArea.Callback, api.Callbacks, options);
    }

    /// <summary>The violation for a path that breaks the path rule.</summary>
    /// <param name="where">Where the path is declared, for the log.</param>
    /// <returns>The <see cref="PluginManifestRule.RoutePathInvalid"/> violation.</returns>
    private static PluginManifestViolation PathViolation(string where)
    {
        return new PluginManifestViolation(
            PluginManifestRule.RoutePathInvalid,
            $"{where}: a path is lowercase segments and {{parameters}} below a leading slash, with no empty segment and no '..'");
    }

    /// <summary>
    /// Checks the pages of one area: the page id is a <see cref="PluginPageIdentifier"/> and unique in the area, a
    /// permission the area does not take or does not know, and the path rule of routes.
    /// </summary>
    /// <param name="area">The panel the pages belong to.</param>
    /// <param name="ui">The menus and pages of the area.</param>
    /// <param name="options">What the host decides.</param>
    /// <returns>The first violation, or <see langword="null"/>.</returns>
    private static PluginManifestViolation? CheckPages(PluginArea area, PluginUiArea ui, PluginManifestOptions options)
    {
        var seenIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (var page in ui.Pages)
        {
            var where = $"ui.{AreaMember(area)}.pages '{page.Id}' path '{page.Path}'";

            // A page lookup answers not found for an id outside this rule, so a page accepted here must meet it or it is never reachable.
            if (!PluginPageIdentifier.IsValid(page.Id))
            {
                return new PluginManifestViolation(
                    PluginManifestRule.PageIdInvalid,
                    $"{where}: a page id is 1 to {PluginPageIdentifier.MaxLength} lowercase letters, digits, hyphens and underscores, starting with a letter or digit");
            }

            // The lookup finds a page by area and id, so a second page with the same id in one area would be shadowed.
            if (!seenIds.Add(page.Id))
            {
                return new PluginManifestViolation(
                    PluginManifestRule.PageIdInvalid, $"{where}: the page id is declared more than once in the {AreaMember(area)} area");
            }

            var violation = CheckPermission(area, page.Permission, where, options);
            if (violation is not null)
            {
                return violation;
            }

            // A page path sits below the plugin's own prefix like a route path does, so the same rule holds.
            if (!PluginRoutePath.TryNormalize(page.Path, out _))
            {
                return PathViolation(where);
            }
        }

        return CheckMenuIcons(area, ui.Menus);
    }

    /// <summary>
    /// Checks the icon each menu of one area names, when it names one. A panel looks the name up in its own icon set,
    /// so only a plain name is accepted: never a URL, markup or a path that a panel could be led to fetch or render.
    /// </summary>
    /// <param name="area">The panel the menus belong to.</param>
    /// <param name="menus">The menus of the area.</param>
    /// <returns>The first violation, or <see langword="null"/>.</returns>
    private static PluginManifestViolation? CheckMenuIcons(PluginArea area, IReadOnlyList<PluginMenuItem> menus)
    {
        foreach (var menu in menus)
        {
            if (menu.Icon is not null && !IconNamePattern().IsMatch(menu.Icon))
            {
                return new PluginManifestViolation(
                    PluginManifestRule.MenuIconInvalid,
                    $"ui.{AreaMember(area)}.menus '{menu.Id}' icon: an icon is 1 to {MaxIconNameLength} lowercase letters, digits and hyphens");
            }
        }

        return null;
    }

    /// <summary>Checks the host version range: each bound, when set, is a semantic version, and the lower is not above the upper.</summary>
    /// <param name="min">The <c>minHostVersion</c>, or <see langword="null"/> for no lower limit.</param>
    /// <param name="max">The <c>maxHostVersion</c>, or <see langword="null"/> for no upper limit.</param>
    /// <returns>The first violation, or <see langword="null"/>.</returns>
    private static PluginManifestViolation? CheckHostVersions(string? min, string? max)
    {
        PluginVersion? lower = null;
        if (min is not null)
        {
            if (!PluginVersion.TryParse(min, out var parsed))
            {
                return new PluginManifestViolation(
                    PluginManifestRule.HostVersionInvalid, "minHostVersion is not a valid version: use major.minor.patch, for example 1.2.3");
            }

            lower = parsed;
        }

        PluginVersion? upper = null;
        if (max is not null)
        {
            if (!PluginVersion.TryParse(max, out var parsed))
            {
                return new PluginManifestViolation(
                    PluginManifestRule.HostVersionInvalid, "maxHostVersion is not a valid version: use major.minor.patch, for example 1.2.3");
            }

            upper = parsed;
        }

        if (lower is not null && upper is not null && lower.CompareTo(upper) > 0)
        {
            return new PluginManifestViolation(
                PluginManifestRule.HostVersionRangeInverted, $"minHostVersion {min} is above maxHostVersion {max}");
        }

        return null;
    }

    /// <summary>Checks the <c>changelog</c>: at most <see cref="PluginManifest.MaxChangelogLength"/> characters.</summary>
    /// <param name="changelog">The manifest's changelog, or <see langword="null"/> when it has none.</param>
    /// <returns>The violation, or <see langword="null"/>.</returns>
    private static PluginManifestViolation? CheckChangelog(string? changelog)
    {
        if (changelog is not null && changelog.Length > PluginManifest.MaxChangelogLength)
        {
            return new PluginManifestViolation(
                PluginManifestRule.ChangelogTooLong, $"changelog: at most {PluginManifest.MaxChangelogLength} characters are allowed");
        }

        return null;
    }

    /// <summary>Checks the <c>tags</c> list: at most <see cref="PluginTags.MaxCount"/> tags, each well formed, none twice.</summary>
    /// <param name="tags">The manifest's tags.</param>
    /// <returns>The first violation, or <see langword="null"/>.</returns>
    private static PluginManifestViolation? CheckTags(IReadOnlyList<string> tags)
    {
        if (tags.Count > PluginTags.MaxCount)
        {
            return new PluginManifestViolation(PluginManifestRule.TagsInvalid, $"tags: at most {PluginTags.MaxCount} tags are allowed");
        }

        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var tag in tags)
        {
            if (!PluginTags.IsWellFormed(tag))
            {
                return new PluginManifestViolation(
                    PluginManifestRule.TagsInvalid,
                    $"tags: a tag is {PluginTags.MinLength} to {PluginTags.MaxLength} lowercase letters, digits and hyphens, with no leading '#'");
            }

            if (!seen.Add(tag))
            {
                return new PluginManifestViolation(PluginManifestRule.TagsInvalid, $"tags: '{tag}' is listed more than once");
            }
        }

        return null;
    }

    /// <summary>Checks the slot entries: the widget id is safe and unique per slot, the slot exists, offers each context key, and its permission fits its area.</summary>
    /// <param name="slots">The slot entries.</param>
    /// <param name="options">What the host decides.</param>
    /// <returns>The first violation, or <see langword="null"/>.</returns>
    private static PluginManifestViolation? CheckSlots(IReadOnlyList<PluginSlotEntry> slots, PluginManifestOptions options)
    {
        var seen = new HashSet<(string Slot, string Id)>();
        foreach (var entry in slots)
        {
            var where = $"ui.slots '{entry.Id}'";
            if (entry.Id is null || !WidgetIdPattern().IsMatch(entry.Id))
            {
                return new PluginManifestViolation(
                    PluginManifestRule.SlotWidgetIdInvalid, $"{where}: the widget id is not lowercase letters, digits, hyphens and underscores");
            }

            if (!seen.Add((entry.Slot, entry.Id)))
            {
                return new PluginManifestViolation(
                    PluginManifestRule.SlotWidgetDuplicate, $"{where}: declared more than once for slot '{entry.Slot}'");
            }

            var slot = PluginSlots.Find(entry.Slot);
            if (slot is null)
            {
                return new PluginManifestViolation(PluginManifestRule.SlotUnknown, $"{where}: the host offers no slot '{entry.Slot}'");
            }

            var missing = entry.ContextKeys.FirstOrDefault(key => !slot.Offers(key));
            if (missing is not null)
            {
                return new PluginManifestViolation(
                    PluginManifestRule.SlotContextKeyNotOffered, $"{where}: slot '{slot.Id}' does not offer the context key '{missing}'");
            }

            var violation = CheckPermission(slot.Area, entry.Permission, where, options);
            if (violation is not null)
            {
                return violation;
            }
        }

        return null;
    }

    /// <summary>
    /// Checks a permission against the area it is declared in: an admin entry takes none, a client entry names
    /// <see cref="PluginPermissions.Any"/> or a client permission the host defines.
    /// </summary>
    /// <param name="area">The panel of the entry.</param>
    /// <param name="permission">The declared permission, or <see langword="null"/> for none.</param>
    /// <param name="where">Where the entry is declared, for the log.</param>
    /// <param name="options">What the host decides.</param>
    /// <returns>The violation, or <see langword="null"/>.</returns>
    private static PluginManifestViolation? CheckPermission(PluginArea area, string? permission, string where, PluginManifestOptions options)
    {
        if (permission is null)
        {
            return null;
        }

        if (area == PluginArea.Admin)
        {
            return new PluginManifestViolation(
                PluginManifestRule.AdminPermissionDeclared, $"{where} declares permission '{permission}' and the admin area takes none");
        }

        if (permission == PluginPermissions.Any || options.ClientPermissionNames.Contains(permission))
        {
            return null;
        }

        return new PluginManifestViolation(
            PluginManifestRule.PermissionUnknown,
            $"{where} names permission '{permission}', which is neither '{PluginPermissions.Any}' nor a client permission");
    }

    /// <summary>
    /// Checks that every text key the menus, pages, settings and slots use is defined in the English texts. Two kinds
    /// of member are told apart. A menu or setting <c>label</c> and a page or slot <c>title</c> is a key when its value is
    /// shaped like one (see <see cref="IsTextKey"/>); any other value is plain text and needs no entry. A setting
    /// <c>description</c> and <c>placeholder</c> is usually literal help text or an example, which can have the shape of a
    /// key, so it counts as a key only when some locale defines that exact value; it must then be defined in English too.
    /// </summary>
    /// <param name="manifest">The manifest.</param>
    /// <returns>The first violation, or <see langword="null"/>.</returns>
    private static PluginManifestViolation? CheckTextKeys(PluginManifest manifest)
    {
        var english = EnglishTexts(manifest);

        foreach (var (where, value, isHelpText) in TextValues(manifest))
        {
            if (value is null)
            {
                continue;
            }

            var isKey = isHelpText
                ? manifest.Locales.Values.Any(texts => texts.ContainsKey(value))
                : IsTextKey(value);
            if (isKey)
            {
                var missing = CheckKeyDefined(where, value, english);
                if (missing is not null)
                {
                    return missing;
                }
            }
        }

        return null;
    }

    /// <summary>Lists every text member a manifest can fill with a key: menu labels, page and slot titles, setting texts.</summary>
    /// <param name="manifest">The manifest.</param>
    /// <returns>
    /// Each member's location for the log, its value (which may be <see langword="null"/>), and whether it is help text
    /// (a setting description or placeholder) that is a key only when a locale defines it.
    /// </returns>
    private static IEnumerable<(string Where, string? Value, bool IsHelpText)> TextValues(PluginManifest manifest)
    {
        (PluginArea Area, PluginUiArea Ui)[] areas = [(PluginArea.Client, manifest.Ui.Client), (PluginArea.Admin, manifest.Ui.Admin)];
        foreach (var (area, ui) in areas)
        {
            foreach (var menu in ui.Menus)
            {
                yield return ($"ui.{AreaMember(area)}.menus '{menu.Id}' label", menu.Label, false);
            }

            foreach (var page in ui.Pages)
            {
                yield return ($"ui.{AreaMember(area)}.pages '{page.Id}' title", page.Title, false);
            }
        }

        foreach (var entry in manifest.Ui.Slots)
        {
            yield return ($"ui.slots '{entry.Id}' title", entry.Title, false);
        }

        foreach (var setting in manifest.Settings)
        {
            yield return ($"settings '{setting.Key}' label", setting.Label, false);
            yield return ($"settings '{setting.Key}' description", setting.Description, true);
            yield return ($"settings '{setting.Key}' placeholder", setting.Placeholder, true);
        }
    }

    /// <summary>The name of an area's member in <c>api</c> and <c>ui</c>, as the manifest spells it.</summary>
    /// <param name="area">The panel.</param>
    /// <returns><c>client</c>, <c>admin</c> or <c>callbacks</c>.</returns>
    private static string AreaMember(PluginArea area)
    {
        return area switch
        {
            PluginArea.Admin => "admin",
            PluginArea.Callback => "callbacks",
            _ => "client",
        };
    }
}
