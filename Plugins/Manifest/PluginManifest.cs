using Hostpanel.Sdk.Plugins.Enums;

namespace Hostpanel.Sdk.Plugins.Manifest;

/// <summary>
/// Deserialised representation of a plugin's <c>plugin.json</c> manifest file.
/// </summary>
public sealed class PluginManifest
{
    /// <summary>The longest <see cref="Changelog"/> a manifest may carry, in characters.</summary>
    public const int MaxChangelogLength = 4000;

    /// <summary>Backing field of <see cref="Fields"/>; empty unless the manifest lists some.</summary>
    private readonly IReadOnlyList<PluginField> _fields = [];

    /// <summary>Backing field of <see cref="Capabilities"/>; empty unless the manifest lists some.</summary>
    private readonly IReadOnlyList<string> _capabilities = [];

    /// <summary>Backing field of <see cref="HostAccess"/>; empty unless the manifest lists some.</summary>
    private readonly IReadOnlyList<string> _hostAccess = [];

    /// <summary>Backing field of <see cref="ServerModules"/>; empty unless the manifest lists some.</summary>
    private readonly IReadOnlyList<string> _serverModules = [];

    /// <summary>Backing field of <see cref="Tags"/>; empty unless the manifest lists some.</summary>
    private readonly IReadOnlyList<string> _tags = [];

    /// <summary>Backing field of <see cref="Settings"/>; empty unless the manifest lists some.</summary>
    private readonly IReadOnlyList<PluginSetting> _settings = [];

    /// <summary>Backing field of <see cref="Locales"/>; empty unless the manifest ships some.</summary>
    private readonly IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> _locales =
        new Dictionary<string, IReadOnlyDictionary<string, string>>();

    /// <summary>Backing field of <see cref="Api"/>; declares no routes unless the manifest does.</summary>
    private readonly PluginApi _api = new();

    /// <summary>Backing field of <see cref="Ui"/>; adds nothing unless the manifest does.</summary>
    private readonly PluginUi _ui = new();

    /// <summary>Backing field of <see cref="ExternalOrigins"/>; empty unless the manifest lists some.</summary>
    private readonly IReadOnlyList<string> _externalOrigins = [];

    /// <summary>
    /// Gets the version of the manifest format. An absent <c>manifestVersion</c> reads as <c>1</c>, the format
    /// that has only <see cref="Fields"/>; <c>2</c> adds <see cref="Settings"/>, <see cref="Locales"/>,
    /// <see cref="Api"/>, <see cref="Ui"/> and <see cref="ExternalOrigins"/>.
    /// </summary>
    public int ManifestVersion { get; init; } = 1;

    /// <summary>Gets the unique plugin identifier, the <c>id</c> field of <c>plugin.json</c>.</summary>
    public required string Id { get; init; }

    /// <summary>Gets the human-readable display name.</summary>
    public required string Name { get; init; }

    /// <summary>Gets the semver version string.</summary>
    public required string Version { get; init; }

    /// <summary>Gets the author name or organisation.</summary>
    public required string Author { get; init; }

    /// <summary>Gets a short description shown in the admin panel.</summary>
    public required string Description { get; init; }

    /// <summary>Gets the plugin category type.</summary>
    public required PluginType Type { get; init; }

    /// <summary>Gets the display category label (e.g. "Hosting / Provisioning").</summary>
    public required string Category { get; init; }

    /// <summary>Gets the hex colour used for the plugin logo block.</summary>
    public string Color { get; init; } = "#6366f1";

    /// <summary>
    /// Gets the optional icon of the plugin: a path relative to the plugin package (forward slashes, for example
    /// <c>icon.svg</c> or <c>assets/icon.png</c>) to an SVG or PNG file. The host checks the file when the plugin is
    /// installed or loaded and shows it beside the plugin name; <see langword="null"/> (the default, and the value of
    /// a missing <c>icon</c>) shows the <see cref="Color"/> block with the plugin's initial instead.
    /// </summary>
    public string? Icon { get; init; }

    /// <summary>Gets the fully-qualified type name of the provider implementation.</summary>
    public required string EntryPoint { get; init; }

    /// <summary>Gets the SDK version this plugin was compiled against.</summary>
    public required string SdkVersion { get; init; }

    /// <summary>
    /// Gets the legacy list of configuration fields. Still read as written; <see cref="Settings"/>
    /// supersedes it, and <see cref="GetSettings"/> answers one list whichever of the two a manifest uses. A missing
    /// or <c>null</c> <c>fields</c> reads as an empty list, never <see langword="null"/>.
    /// </summary>
    public IReadOnlyList<PluginField> Fields
    {
        get => _fields;
        init => _fields = value ?? [];
    }

    /// <summary>
    /// Gets the typed settings an administrator fills in. A missing or <c>null</c> <c>settings</c> reads as an
    /// empty list, never <see langword="null"/>.
    /// </summary>
    public IReadOnlyList<PluginSetting> Settings
    {
        get => _settings;
        init => _settings = value ?? [];
    }

    /// <summary>
    /// Gets the texts of the plugin per locale: the locale code (for example <c>en</c>) to a map of text key to
    /// text. The keys are what <c>label</c> and <c>title</c> members refer to. A missing or <c>null</c>
    /// <c>locales</c> reads as an empty map, never <see langword="null"/>.
    /// </summary>
    public IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> Locales
    {
        get => _locales;
        init => _locales = value ?? new Dictionary<string, IReadOnlyDictionary<string, string>>();
    }

    /// <summary>Gets the routes the plugin answers. A missing or <c>null</c> <c>api</c> reads as no routes.</summary>
    public PluginApi Api
    {
        get => _api;
        init => _api = value ?? new PluginApi();
    }

    /// <summary>Gets the pages, menus and slot content the plugin adds. A missing or <c>null</c> <c>ui</c> reads as nothing added.</summary>
    public PluginUi Ui
    {
        get => _ui;
        init => _ui = value ?? new PluginUi();
    }

    /// <summary>
    /// Gets the web origins (scheme, host and port, for example <c>https://example.com</c>) the plugin's pages may
    /// load content from or link to. A missing or <c>null</c> <c>externalOrigins</c> reads as an empty list.
    /// </summary>
    public IReadOnlyList<string> ExternalOrigins
    {
        get => _externalOrigins;
        init => _externalOrigins = value ?? [];
    }

    /// <summary>Gets a value indicating whether this plugin should appear in the Apps &amp; Integrations list.</summary>
    public bool ShowInIntegrations { get; init; }

    /// <summary>Gets the category name shown in the integrations grid (e.g. "Provisioning").</summary>
    public string? IntegrationsCategory { get; init; }

    /// <summary>
    /// Gets the optional capabilities the plugin declares, named in <see cref="PluginCapabilities"/> (for example
    /// <c>storedCredentials</c>). Lets the admin and the client portal show what a plugin can do without activating
    /// it; each entry must agree with the interfaces the plugin class implements. <c>storedCredentials</c> fits
    /// payment plugins (<see cref="PluginType.Payment"/>) only; <c>endpoints</c> fits any plugin type. A type
    /// leaves the list empty when no capability applies to it. A missing or <c>null</c> <c>capabilities</c>
    /// in the JSON reads as an empty list, never <see langword="null"/>.
    /// </summary>
    public IReadOnlyList<string> Capabilities
    {
        get => _capabilities;
        init => _capabilities = value ?? [];
    }

    /// <summary>
    /// Gets the kinds of access to a client's hosting the plugin asks for, named in <see cref="Host.PluginHostAccess"/>
    /// (for example <c>ssh</c>); empty when it asks for none. A missing or <c>null</c> <c>hostAccess</c> in the JSON
    /// reads as an empty list. The admin plugins page shows each entry.
    /// </summary>
    public IReadOnlyList<string> HostAccess
    {
        get => _hostAccess;
        init => _hostAccess = value ?? [];
    }

    /// <summary>
    /// Gets the server module ids a provisioning plugin serves (the <c>serverModules</c> field, for example
    /// <c>["examplepanel"]</c>): the host sends a server whose module is listed here to this plugin. Required for a plugin
    /// that implements <see cref="Provisioning.IProvisioningPanelPlugin"/> (and so lists
    /// <see cref="PluginCapabilities.ProvisioningPanel"/>), and ignored for every other plugin. Each id follows
    /// <see cref="PluginServerModules"/>, which also validates the list. A missing or <c>null</c> <c>serverModules</c>
    /// reads as an empty list, never <see langword="null"/>.
    /// </summary>
    public IReadOnlyList<string> ServerModules
    {
        get => _serverModules;
        init => _serverModules = value ?? [];
    }

    /// <summary>
    /// Gets the plugin's tags (the <c>tags</c> field, for example <c>["backup", "billing"]</c>): short keywords that plugin
    /// registries use for discovery. Optional, and the host does nothing with them beyond reading and validating them. A tag
    /// is 2 to <see cref="PluginTags.MaxLength"/> lowercase letters, digits and hyphens with no leading <c>#</c>, and a
    /// manifest lists at most <see cref="PluginTags.MaxCount"/> of them (see <see cref="PluginTags"/>). A missing or
    /// <c>null</c> <c>tags</c> reads as an empty list, never <see langword="null"/>.
    /// </summary>
    public IReadOnlyList<string> Tags
    {
        get => _tags;
        init => _tags = value ?? [];
    }

    /// <summary>
    /// Gets the lowest host version the plugin runs on (the <c>minHostVersion</c> field, for example <c>2.1.0</c>):
    /// a semantic version, <c>major.minor.patch</c> with an optional pre-release. <see langword="null"/> when the
    /// manifest sets none, which means no lower limit.
    /// </summary>
    public string? MinHostVersion { get; init; }

    /// <summary>
    /// Gets the highest host version the plugin runs on (the <c>maxHostVersion</c> field), in the same format as
    /// <see cref="MinHostVersion"/> and never below it. <see langword="null"/> when the manifest sets none, which means
    /// no upper limit.
    /// </summary>
    public string? MaxHostVersion { get; init; }

    /// <summary>
    /// Gets the notes for this plugin version (the <c>changelog</c> field), plain text of at most
    /// <see cref="MaxChangelogLength"/> characters. <see langword="null"/> when the manifest carries no notes.
    /// </summary>
    public string? Changelog { get; init; }

    /// <summary>
    /// Answers the settings of the plugin whichever format the manifest uses: <see cref="Settings"/> when it lists
    /// any, otherwise each legacy <see cref="Fields"/> entry converted with <see cref="PluginSetting.FromField"/>.
    /// </summary>
    /// <returns>The settings in declared order; empty when the manifest declares neither.</returns>
    public IReadOnlyList<PluginSetting> GetSettings()
    {
        return _settings.Count > 0 ? _settings : Fields.Select(PluginSetting.FromField).ToList();
    }
}
