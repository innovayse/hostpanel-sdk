namespace Hostpanel.Sdk.Plugins.Ui;

/// <summary>
/// The view document schema: the declarative document a plugin page or slot widget is rendered from, named by the
/// <c>view</c> member of a page or slot entry in <c>plugin.json</c>. This type holds the schema version, the block
/// kinds, the members whose text is a label and the members that name a plugin route. A new block kind, label member
/// or route member is a minor release of the package; removing or renaming one is a new <see cref="SchemaVersion"/>
/// and a major release.
/// </summary>
/// <remarks>
/// <para>
/// A view document is a JSON object: <c>{ "schema": 1, "blocks": [ { "kind": "header", "title": "pages.overview" } ] }</c>.
/// <c>schema</c> is <see cref="SchemaVersion"/> and <c>blocks</c> is an array of blocks. A block is any object in the
/// document that has a <c>kind</c> member (top-level blocks, the <c>blocks</c> of a tab, the <c>actions</c> of a header),
/// and its <c>kind</c> is one of <see cref="BlockKinds"/>. Every other member is the block's own data, rendered by the panel.
/// </para>
/// <para>
/// A string held by a member named in <see cref="LabelProperties"/>, at any depth, is a label: a key of the manifest
/// <c>locales</c> or plain text, by the same rule as a menu <c>label</c>. The host replaces each one with its text in
/// the caller's language (English when that language lacks the key) before the document leaves the API, so a panel
/// never sees a key.
/// </para>
/// <para>
/// An object held by a member named in <see cref="RouteProperties"/>, at any depth, names one route of the plugin's
/// <c>api</c> block, as a <see cref="PluginViewRoute"/>: <c>{ "method": "post", "path": "/services/{serviceId}/site/publish" }</c>.
/// Routes have no ids, so a view names a route by its method and its declared path template, written exactly as in
/// <c>api</c>; the area is the one of the page or slot that shows the view. The panel fills each <c>{parameter}</c>
/// from the page path parameter or the slot context key of the same name and calls the route through the host.
/// </para>
/// <para>
/// A view reacts to state with three members (since 2.2.0). A condition is the object
/// <c>{ "route": { "method": "get", "path": "/services/{serviceId}/site" }, "field": "site.status", "equals": ["ready", "live"] }</c>:
/// <c>route</c> names a GET route of the same area, <c>field</c> is a dotted path into its reply and <c>equals</c> is one
/// JSON value (a string, a number, <c>true</c>, <c>false</c> or <c>null</c>) or a list of 1 to
/// <see cref="MaxConditionValues"/> of them; the condition holds while the field equals one of them, compared exactly
/// (no conversion between a number and a string).
/// </para>
/// <list type="bullet">
/// <item><description><see cref="VisibleWhenProperty"/> on any block holds a condition: the panel shows the block only
/// while the condition holds, and hides it while the route is read, when the read fails and when the field is absent.
/// The panel reads the condition again whenever the block's <c>id</c> is refreshed.</description></item>
/// <item><description><see cref="ResetOnSuccessProperty"/> in a form's <c>submit</c> is <c>true</c> or <c>false</c>:
/// when <c>true</c>, every field returns to its empty value after a successful submit.</description></item>
/// <item><description><see cref="FollowUpProperty"/> on an action block, or in a form's <c>submit</c>, is
/// <c>{ "refresh": ["status"], "until": condition }</c>: after a successful call the panel reads the blocks whose
/// <c>id</c> is listed in <c>refresh</c> (1 to <see cref="MaxFollowUpRefresh"/> ids, each the <c>id</c> of a block of the
/// same document) again and again until the <c>until</c> condition holds. How often and how many times is the host's
/// decision, never the plugin's: the panel stops after its own bounded number of attempts and when the reader leaves
/// the page, so a plugin needs no polling of its own.</description></item>
/// </list>
/// <para>
/// Two blocks arrange a working screen (since 3.2.0). <c>split</c> holds <see cref="SplitPaneCount"/> panes side by side,
/// stacked on a phone: <c>{ "kind": "split", "panes": [ { "blocks": [...] }, { "blocks": [...] } ] }</c>; a split never
/// nests inside a split. <c>preview</c>, on a client page only, shows one absolute address in a sandboxed frame:
/// <c>{ "kind": "preview", "size": "desktop", "sources": [ { "id": "live", "label": "site.live", "route": { "method": "get", "path": "/site" }, "field": "liveUrl" } ] }</c>.
/// The panel reads the selected source's GET route and shows the address only when its origin is one the host allows for
/// the reader: a manifest <c>externalOrigins</c> entry or a domain of the reader's own hosting services. Anything else
/// shows nothing. A plugin cannot widen the allowed origins or the sandbox flags of the frame.
/// </para>
/// </remarks>
public static class PluginViews
{
    /// <summary>The view document schema version this package describes, the value of a document's <c>schema</c> member.</summary>
    public const int SchemaVersion = 1;

    /// <summary>
    /// Gets the block kinds of schema version 1: <c>header</c>, <c>text</c>, <c>stat</c>, <c>table</c>, <c>form</c>,
    /// <c>action</c>, <c>link</c>, <c>badge</c>, <c>empty</c>, <c>alert</c>, <c>tabs</c>, <c>chat</c>, <c>progress</c>,
    /// and since 3.2.0 <c>split</c> and <c>preview</c>. Compared exactly.
    /// </summary>
    public static IReadOnlyList<string> BlockKinds { get; } =
        ["header", "text", "stat", "table", "form", "action", "link", "badge", "empty", "alert", "tabs", "chat", "progress", SplitKind, PreviewKind];

    /// <summary>
    /// Gets the members of schema version 1 whose string value is a label, wherever they appear: <c>title</c> (a block,
    /// a tab), <c>label</c> (an action, a column, a field, a stat), <c>confirm</c> (an action), <c>placeholder</c> (a
    /// field, a chat) and <c>text</c> (a text or alert block, a suggestion). Compared exactly.
    /// </summary>
    public static IReadOnlyList<string> LabelProperties { get; } = ["title", "label", "confirm", "placeholder", "text"];

    /// <summary>
    /// Gets the members of schema version 1 whose value is a <see cref="PluginViewRoute"/>, wherever they appear:
    /// <c>route</c> (an action, a form submit, a data <c>source</c>), and <c>send</c> and <c>history</c> (a chat).
    /// Compared exactly.
    /// </summary>
    public static IReadOnlyList<string> RouteProperties { get; } = ["route", "send", "history"];

    /// <summary>
    /// The member of a block holding the condition under which the block is shown (since 2.2.0). Its value is a
    /// condition object: <c>route</c> (a GET <see cref="PluginViewRoute"/>), <c>field</c> (a dotted path into the reply)
    /// and <c>equals</c> (one JSON value or a list of 1 to <see cref="MaxConditionValues"/> of them).
    /// </summary>
    public const string VisibleWhenProperty = "visibleWhen";

    /// <summary>
    /// The member of a form's <c>submit</c> telling the panel to empty every field after a successful submit (since
    /// 2.2.0). Its value is <c>true</c> or <c>false</c>; absent means <c>false</c>.
    /// </summary>
    public const string ResetOnSuccessProperty = "resetOnSuccess";

    /// <summary>
    /// The member of an action block, or of a form's <c>submit</c>, naming the blocks the panel reads again after a
    /// successful call until a condition holds (since 2.2.0): <c>{ "refresh": [block ids], "until": condition }</c>.
    /// The interval and the number of attempts are the host's; a plugin cannot set them.
    /// </summary>
    public const string FollowUpProperty = "followUp";

    /// <summary>The most values the <c>equals</c> list of a condition may hold.</summary>
    public const int MaxConditionValues = 20;

    /// <summary>The most block ids the <c>refresh</c> list of a follow-up may hold.</summary>
    public const int MaxFollowUpRefresh = 20;

    /// <summary>The block kind holding two panes side by side (since 3.2.0).</summary>
    public const string SplitKind = "split";

    /// <summary>The member of a <c>split</c> block listing its panes, each an object holding only <c>blocks</c>.</summary>
    public const string PanesProperty = "panes";

    /// <summary>The exact number of panes a <c>split</c> block holds.</summary>
    public const int SplitPaneCount = 2;

    /// <summary>The block kind showing one address in a sandboxed frame, on a client page only (since 3.2.0).</summary>
    public const string PreviewKind = "preview";

    /// <summary>
    /// The member of a <c>preview</c> block listing the addresses the reader can switch between. Each source holds exactly
    /// <c>id</c> (unique in the block, <c>[a-z0-9][a-z0-9_-]*</c>, at most <see cref="MaxPreviewSourceIdLength"/>
    /// characters), <c>label</c>, <c>route</c> (a GET <see cref="PluginViewRoute"/>) and <c>field</c> (a dotted path into
    /// the reply, holding the address).
    /// </summary>
    public const string PreviewSourcesProperty = "sources";

    /// <summary>The most sources a <c>preview</c> block may hold.</summary>
    public const int MaxPreviewSources = 5;

    /// <summary>The longest <c>id</c> of a preview source, in characters.</summary>
    public const int MaxPreviewSourceIdLength = 64;

    /// <summary>The member of a <c>preview</c> block naming the frame size shown first; absent means <see cref="PreviewSizeDesktop"/>.</summary>
    public const string PreviewSizeProperty = "size";

    /// <summary>The preview size of a desktop screen, which fills its pane.</summary>
    public const string PreviewSizeDesktop = "desktop";

    /// <summary>The preview size of a phone screen. The pixel width is the panel's, never the plugin's.</summary>
    public const string PreviewSizePhone = "phone";

    /// <summary>Gets the values of <see cref="PreviewSizeProperty"/>. Compared exactly.</summary>
    public static IReadOnlyList<string> PreviewSizes { get; } = [PreviewSizeDesktop, PreviewSizePhone];
}
