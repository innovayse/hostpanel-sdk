namespace Hostpanel.Sdk.Plugins.Ui;

/// <summary>
/// The stream event names, and the values of their members, a <c>chat</c> block renders apart from the reply text (since
/// 3.2.0). A plugin sends them as ordinary events of its streaming route, with camelCase JSON data. The names
/// <c>error</c> and <c>done</c> stay the host's: use <see cref="Issue"/> for a failure of the plugin's own. All text is
/// plain, already in the reader's language, and rendered as text. A panel ignores an event that does not fit its shape.
/// </summary>
public static class PluginChatEvents
{
    /// <summary>
    /// One row of the compact list of steps inside the reply: <c>{ "id": "build", "label": "Building", "state": "running" }</c>.
    /// <c>id</c> is optional (the <c>label</c> keys the row when it is absent); sending a key again updates the row, so a
    /// step goes from <see cref="StepRunning"/> to <see cref="StepDone"/> or <see cref="StepFailed"/> in place.
    /// </summary>
    public const string Step = "step";

    /// <summary>A step that is under way.</summary>
    public const string StepRunning = "running";

    /// <summary>A step that finished.</summary>
    public const string StepDone = "done";

    /// <summary>A step that did not finish.</summary>
    public const string StepFailed = "failed";

    /// <summary>
    /// A card inside the reply: <c>{ "tone": "success", "title": "Published", "text": "...", "actions": [...] }</c>. At least
    /// one of <c>title</c> and <c>text</c>. <c>actions</c> holds at most <see cref="MaxResultActions"/> objects, each with a
    /// <c>label</c> and exactly one of <c>route</c> (a <see cref="PluginViewRoute"/> of the plugin's own, called through
    /// the host under its usual rules, with an optional <c>confirm</c>) or <c>href</c> (under the rule of a link block).
    /// </summary>
    public const string Result = "result";

    /// <summary>A neutral result, also how an absent or unknown tone reads.</summary>
    public const string ToneInfo = "info";

    /// <summary>A result that went well.</summary>
    public const string ToneSuccess = "success";

    /// <summary>A result that needs attention.</summary>
    public const string ToneWarning = "warning";

    /// <summary>A result that went wrong.</summary>
    public const string ToneDanger = "danger";

    /// <summary>The most actions a panel renders on one result.</summary>
    public const int MaxResultActions = 5;

    /// <summary>
    /// A failure of the plugin's own, shown in the danger tone inside the reply: <c>{ "text": "The build failed." }</c>. The
    /// stream goes on; end it as usual.
    /// </summary>
    public const string Issue = "issue";

    /// <summary>Gets every step state. Compared exactly.</summary>
    public static IReadOnlyList<string> StepStates { get; } = [StepRunning, StepDone, StepFailed];

    /// <summary>Gets every result tone. Compared exactly.</summary>
    public static IReadOnlyList<string> ResultTones { get; } = [ToneInfo, ToneSuccess, ToneWarning, ToneDanger];
}
