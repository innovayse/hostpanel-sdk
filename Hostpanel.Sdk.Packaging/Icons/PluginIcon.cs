namespace Hostpanel.Sdk.Packaging.Icons;

/// <summary>A plugin icon that passed <see cref="PluginIconChecker"/>: the bytes a host can keep in memory and serve without another file read.</summary>
/// <param name="ContentType">The media type: <c>image/svg+xml</c> or <c>image/png</c>.</param>
/// <param name="Content">The checked file content.</param>
/// <param name="ETag">A strong entity tag, quoted, derived from the content.</param>
public sealed record PluginIcon(string ContentType, byte[] Content, string ETag);
