namespace Hostpanel.Sdk.Packaging.Enums;

/// <summary>
/// A structural rule a plugin manifest breaks. Only rules that need no host state are here; a host adds its own
/// (view documents, whether an entry point implements a contract, ...) beside these. Values start at 1; 0 is not a
/// rule. A host maps by name, never by cast.
/// </summary>
public enum PluginManifestRule
{
    /// <summary>The manifest version is not one the validator was asked to accept.</summary>
    ManifestVersionUnknown = 1,

    /// <summary>The manifest id is not a valid plugin id.</summary>
    PluginIdInvalid = 2,

    /// <summary>The manifest version is not a strict semantic version.</summary>
    VersionInvalid = 3,

    /// <summary>A client route, page or slot permission is neither the any-permission marker nor a name the host defines.</summary>
    PermissionUnknown = 4,

    /// <summary>An admin route, page or slot declares a permission, which the admin area does not take.</summary>
    AdminPermissionDeclared = 5,

    /// <summary>A route or page path is not a slash followed by lowercase literal segments and parameters.</summary>
    RoutePathInvalid = 6,

    /// <summary>A method and path template is declared twice in one area.</summary>
    RouteDuplicate = 7,

    /// <summary>A callback route breaks the callback rules of the SDK (a permission, or streaming).</summary>
    CallbackInvalid = 8,

    /// <summary>A page id is not a valid page id, or is declared twice in one area.</summary>
    PageIdInvalid = 9,

    /// <summary>A menu icon is not a plain icon name.</summary>
    MenuIconInvalid = 10,

    /// <summary>A slot widget id is not a safe id.</summary>
    SlotWidgetIdInvalid = 11,

    /// <summary>A widget id is declared twice for one slot.</summary>
    SlotWidgetDuplicate = 12,

    /// <summary>A slot entry names a slot the SDK does not define.</summary>
    SlotUnknown = 13,

    /// <summary>A slot entry names a context key its slot does not offer.</summary>
    SlotContextKeyNotOffered = 14,

    /// <summary>A text key used by a label, title, description or placeholder is not defined in the English texts.</summary>
    LabelKeyMissing = 15,

    /// <summary>An assembly in the package was built against an older SDK major version than the host provides.</summary>
    SdkAssemblyOutdated = 16,

    /// <summary>An assembly in the package could not be read.</summary>
    AssemblyUnreadable = 17,

    /// <summary>The <c>tags</c> list has more tags than allowed, a tag that is not 2 to 30 lowercase letters, digits and hyphens, or the same tag twice.</summary>
    TagsInvalid = 18,

    /// <summary>The <c>minHostVersion</c> or <c>maxHostVersion</c> is not a valid semantic version.</summary>
    HostVersionInvalid = 19,

    /// <summary>The <c>minHostVersion</c> is above the <c>maxHostVersion</c>.</summary>
    HostVersionRangeInverted = 20,

    /// <summary>The <c>changelog</c> is longer than <see cref="Hostpanel.Sdk.Plugins.Manifest.PluginManifest.MaxChangelogLength"/> characters.</summary>
    ChangelogTooLong = 21,

    /// <summary>
    /// The <c>serverAccountIdLabel</c> is empty or white space, is longer than
    /// <see cref="Hostpanel.Sdk.Plugins.Manifest.PluginManifest.MaxServerAccountIdLabelLength"/> characters, or holds a
    /// control character.
    /// </summary>
    AccountIdLabelInvalid = 22,
}
