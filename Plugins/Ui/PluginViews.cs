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
/// </remarks>
public static class PluginViews
{
    /// <summary>The view document schema version this package describes, the value of a document's <c>schema</c> member.</summary>
    public const int SchemaVersion = 1;

    /// <summary>
    /// Gets the block kinds of schema version 1: <c>header</c>, <c>text</c>, <c>stat</c>, <c>table</c>, <c>form</c>,
    /// <c>action</c>, <c>link</c>, <c>badge</c>, <c>empty</c>, <c>alert</c>, <c>tabs</c>, <c>chat</c> and
    /// <c>progress</c>. Compared exactly.
    /// </summary>
    public static IReadOnlyList<string> BlockKinds { get; } =
        ["header", "text", "stat", "table", "form", "action", "link", "badge", "empty", "alert", "tabs", "chat", "progress"];

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
}
