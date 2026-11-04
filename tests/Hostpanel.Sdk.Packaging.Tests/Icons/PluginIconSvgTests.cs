using Hostpanel.Sdk.Packaging.Enums;
using Hostpanel.Sdk.Packaging.Icons;
using static Hostpanel.Sdk.Packaging.Tests.Icons.IconFixtures;

namespace Hostpanel.Sdk.Packaging.Tests.Icons;

/// <summary>Tests of the SVG rules of <see cref="Hostpanel.Sdk.Packaging.Icons.PluginIconChecker"/>: what is refused and what must pass.</summary>
public sealed class PluginIconSvgTests
{
    /// <summary>The namespace declaration of the link attributes, written on the element that uses one.</summary>
    private const string XlinkNs = "xmlns:xlink=\"http://www.w3.org/1999/xlink\"";

    /// <summary>A style that splits a forbidden pattern over CDATA, text, comments or child elements is read as one text.</summary>
    /// <param name="inner">The markup inside the root.</param>
    [Theory]
    [InlineData("<style>@im<![CDATA[port 'x.css';]]></style>")]
    [InlineData("<style><![CDATA[@im]]>port 'x.css';</style>")]
    [InlineData("<style><![CDATA[@]]><![CDATA[im]]><![CDATA[port 'x.css';]]></style>")]
    [InlineData("<style>url<![CDATA[(https://example.org/x)]]></style>")]
    [InlineData("<style>ur<![CDATA[l(https://example.org/x)]]></style>")]
    [InlineData("<style>@im<!-- split -->port 'x.css';</style>")]
    [InlineData("<style>url<!-- split -->(https://example.org/x)</style>")]
    [InlineData("<style>@im<b/>port 'x.css';</style>")]
    [InlineData("<style>@im<b>po</b>rt 'x.css';</style>")]
    [InlineData("<style>ur<b/>l(https://example.org/x)</style>")]
    [InlineData("<style>expres<![CDATA[sion(alert(1))]]></style>")]
    [InlineData("<style>fill:url(https://example.org/x)</style>")]
    [InlineData("<style>rect{width:expression(1)}</style>")]
    [InlineData("<style>@import url(x.css);</style>")]
    [InlineData("<STYLE>@import 'x.css';</STYLE>")]
    public void Check_StyleSplitOverNodes_IsInvalid(string inner)
    {
        CheckSvg(inner).Violation!.Rule.Should().Be(PluginIconRule.Invalid);
    }

    /// <summary>Plain text outside a stylesheet is not scanned: a title or a description may say anything.</summary>
    /// <param name="inner">The markup inside the root.</param>
    [Theory]
    [InlineData("<title>Expression (beta)</title><rect/>")]
    [InlineData("<title>Behavior: new</title><rect/>")]
    [InlineData("<desc>@import behavior: javascript: url(https://example.org) expression(1)</desc><rect/>")]
    [InlineData("<text>url(https://example.org/x)</text>")]
    [InlineData("<style>rect{fill:#fff}</style><title>Expression (beta)</title>")]
    public void Check_PlainTextOutsideStyle_IsAccepted(string inner)
    {
        var result = CheckSvg(inner);

        result.Violation.Should().BeNull();
        result.Icon.Should().NotBeNull();
    }

    /// <summary>An SVG that runs code, embeds content, has no root of its own or is not well-formed is invalid.</summary>
    /// <param name="svg">The SVG text.</param>
    [Theory]
    [InlineData("<svg xmlns=\"http://www.w3.org/2000/svg\"><script>alert(1)</script></svg>")]
    [InlineData("<svg xmlns=\"http://www.w3.org/2000/svg\"><foreignObject/></svg>")]
    [InlineData("<svg xmlns=\"http://www.w3.org/2000/svg\" onload=\"x()\"/>")]
    [InlineData("<svg xmlns=\"http://www.w3.org/2000/svg\"><a href=\"https://example.org\"/></svg>")]
    [InlineData("<svg xmlns=\"http://www.w3.org/2000/svg\"><rect style=\"fill:url(https://example.org/x)\"/></svg>")]
    [InlineData("<svg xmlns=\"http://www.w3.org/2000/svg\"><style>@import 'x.css';</style></svg>")]
    [InlineData("<svg xmlns=\"http://www.w3.org/2000/svg\"><set attributeName=\"href\" to=\"x\"/></svg>")]
    [InlineData("<svg xmlns=\"http://www.w3.org/2000/svg\"><animate attributeName=\"onclick\"/></svg>")]
    [InlineData("<?xml-stylesheet href=\"x.css\"?><svg xmlns=\"http://www.w3.org/2000/svg\"/>")]
    [InlineData("<!DOCTYPE svg [<!ENTITY x \"y\">]><svg xmlns=\"http://www.w3.org/2000/svg\"/>")]
    [InlineData("<html xmlns=\"http://www.w3.org/1999/xhtml\"/>")]
    [InlineData("<svg xmlns=\"http://www.w3.org/2000/svg\"")]
    [InlineData("<svg viewBox=\"0 0 1 1\"><rect/></svg>")]
    [InlineData("<svg/>")]
    [InlineData("<svg xmlns=\"http://example.org/other\"/>")]
    [InlineData("")]
    [InlineData("not xml at all")]
    public void Check_HostileOrBrokenSvg_IsInvalid(string svg)
    {
        CheckFile("icon.svg", svg).Violation!.Rule.Should().Be(PluginIconRule.Invalid);
    }

    /// <summary>Every element that runs code or loads another document is refused, in any letter case and at any depth.</summary>
    /// <param name="element">The element name.</param>
    [Theory]
    [InlineData("script")]
    [InlineData("SCRIPT")]
    [InlineData("foreignObject")]
    [InlineData("iframe")]
    [InlineData("object")]
    [InlineData("embed")]
    [InlineData("audio")]
    [InlineData("video")]
    [InlineData("canvas")]
    [InlineData("handler")]
    [InlineData("listener")]
    public void Check_BannedElement_IsInvalid(string element)
    {
        CheckSvg($"<{element}/>").Violation!.Rule.Should().Be(PluginIconRule.Invalid);
        CheckSvg($"<g><g><{element}>x</{element}></g></g>").Violation!.Rule.Should().Be(PluginIconRule.Invalid);
    }

    /// <summary>An event handler attribute is refused on the root, on a child, in capitals and under a namespace prefix.</summary>
    /// <param name="inner">The markup inside the root.</param>
    [Theory]
    [InlineData("<rect onclick=\"x()\"/>")]
    [InlineData("<rect ONCLICK=\"x()\"/>")]
    [InlineData("<g><rect onmouseover=\"x()\"/></g>")]
    [InlineData("<rect " + XlinkNs + " xlink:onclick=\"x()\"/>")]
    public void Check_EventHandlerAttribute_IsInvalid(string inner)
    {
        CheckSvg(inner).Violation!.Rule.Should().Be(PluginIconRule.Invalid);
    }

    /// <summary>A reference that leaves the document is refused, with or without a namespace prefix.</summary>
    /// <param name="inner">The markup inside the root.</param>
    [Theory]
    [InlineData("<image " + XlinkNs + " xlink:href=\"https://example.org/x.png\"/>")]
    [InlineData("<image href=\"https://example.org/x.png\"/>")]
    [InlineData("<image href=\"//example.org/x.png\"/>")]
    [InlineData("<image href=\"data:image/png;base64,AAAA\"/>")]
    [InlineData("<use href=\"icons.svg#a\"/>")]
    [InlineData("<use HREF=\"x.svg\"/>")]
    [InlineData("<use href=\"\"/>")]
    public void Check_ExternalReference_IsInvalid(string inner)
    {
        CheckSvg(inner).Violation!.Rule.Should().Be(PluginIconRule.Invalid);
    }

    /// <summary>A reference to a fragment of the same document is fine.</summary>
    /// <param name="inner">The markup inside the root.</param>
    [Theory]
    [InlineData("<defs><g id=\"a\"/></defs><use href=\"#a\"/>")]
    [InlineData("<defs><g id=\"a\"/></defs><use " + XlinkNs + " xlink:href=\"#a\"/>")]
    [InlineData("<defs><linearGradient id=\"g\"/></defs><rect fill=\"url(#g)\"/>")]
    [InlineData("<defs><linearGradient id=\"g\"/></defs><rect fill=\"url('#g')\"/>")]
    [InlineData("<defs><linearGradient id=\"g\"/></defs><rect fill=\"url( '#g' )\"/>")]
    [InlineData("<defs><linearGradient id=\"g\"/></defs><rect style=\"fill:url( #g )\"/>")]
    [InlineData("<defs><linearGradient id=\"g\"/></defs><style>rect{fill:url(#g)}</style><rect/>")]
    public void Check_FragmentReference_IsAccepted(string inner)
    {
        var result = CheckSvg(inner);

        result.Violation.Should().BeNull();
        result.Icon.Should().NotBeNull();
    }

    /// <summary>A <c>url(...)</c> to anything but a fragment is refused, however it is written.</summary>
    /// <param name="value">The attribute value or style text.</param>
    [Theory]
    [InlineData("fill:url(https://example.org/x)")]
    [InlineData("fill:URL(https://example.org/x)")]
    [InlineData("fill:Url(https://example.org/x)")]
    [InlineData("fill:url( https://example.org/x)")]
    [InlineData("fill:url('https://example.org/x')")]
    [InlineData("fill:url(\"https://example.org/x\")")]
    [InlineData("fill:url( 'https://example.org/x' )")]
    [InlineData("fill:url (https://example.org/x)")]
    [InlineData("fill:url(data:image/png;base64,AAAA)")]
    [InlineData("fill:url(x.png)")]
    [InlineData("fill:url(#g) url(x.png)")]
    [InlineData("fill:url()")]
    public void Check_ExternalUrlInStyleAttribute_IsInvalid(string value)
    {
        // A double quote in the value is written as an entity inside the attribute; the style element takes it as it is.
        var escaped = value.Replace("\"", "&quot;");

        CheckSvg($"<rect style=\"{escaped}\"/>").Violation!.Rule.Should().Be(PluginIconRule.Invalid);
        CheckSvg($"<style>{value}</style>").Violation!.Rule.Should().Be(PluginIconRule.Invalid);
    }

    /// <summary>A script URL, an <c>expression(</c>, a behavior or a binding in a style or an attribute is refused.</summary>
    /// <param name="value">The attribute value or style text.</param>
    [Theory]
    [InlineData("background:javascript:alert(1)")]
    [InlineData("background:JavaScript :alert(1)")]
    [InlineData("width:expression(alert(1))")]
    [InlineData("width:EXPRESSION (alert(1))")]
    [InlineData("behavior:url(#a)")]
    [InlineData("behavior : x")]
    [InlineData("-moz-binding:url(#a)")]
    [InlineData("-MOZ-BINDING:x")]
    [InlineData("@import 'x.css'")]
    [InlineData("@IMPORT 'x.css'")]
    public void Check_ActiveStyleContent_IsInvalid(string value)
    {
        CheckSvg($"<rect style=\"{value}\"/>").Violation!.Rule.Should().Be(PluginIconRule.Invalid);
        CheckSvg($"<rect data-x=\"{value}\"/>").Violation!.Rule.Should().Be(PluginIconRule.Invalid);
        CheckSvg($"<style>{value}</style>").Violation!.Rule.Should().Be(PluginIconRule.Invalid);
    }

    /// <summary>A reference to something outside the document is found even when the style spells it with CSS escapes or comments.</summary>
    /// <param name="svg">The SVG text.</param>
    [Theory]
    [InlineData("<svg xmlns=\"http://www.w3.org/2000/svg\"><rect style=\"fill:\\75 rl(https://example.org/x)\"/></svg>")]
    [InlineData("<svg xmlns=\"http://www.w3.org/2000/svg\"><rect style=\"fill:\\000075rl(https://example.org/x)\"/></svg>")]
    [InlineData("<svg xmlns=\"http://www.w3.org/2000/svg\"><rect style=\"fill:ur\\l(https://example.org/x)\"/></svg>")]
    [InlineData("<svg xmlns=\"http://www.w3.org/2000/svg\"><style>@\\69mport 'x.css';</style></svg>")]
    [InlineData("<svg xmlns=\"http://www.w3.org/2000/svg\"><style>@import/**/'x.css';</style></svg>")]
    [InlineData("<svg xmlns=\"http://www.w3.org/2000/svg\"><style>url/* c */(https://example.org/x)</style></svg>")]
    public void Check_EscapedStyleReference_IsInvalid(string svg)
    {
        CheckFile("icon.svg", svg).Violation!.Rule.Should().Be(PluginIconRule.Invalid);
    }

    /// <summary>
    /// An escape for a code point that is not a character decodes to the replacement character without throwing, and an
    /// escape written right after it still decodes, so the reference behind it is found.
    /// </summary>
    /// <param name="value">The style text.</param>
    [Theory]
    [InlineData("\\110000 \\75 rl(https://example.org/x)")]
    [InlineData("\\FFFFFF \\75 rl(https://example.org/x)")]
    [InlineData("\\0 \\75 rl(https://example.org/x)")]
    [InlineData("\\000000 \\75 rl(https://example.org/x)")]
    [InlineData("\\D800 \\75 rl(https://example.org/x)")]
    [InlineData("\\DFFF \\75 rl(https://example.org/x)")]
    [InlineData("\\1F600 \\75 rl(https://example.org/x)")]
    [InlineData("\\0 ur\\6C (https://example.org/x)")]
    public void Check_CssEscapeEdgeCases_StillFindTheReference(string value)
    {
        CheckSvg($"<style>{value}</style>").Violation!.Rule.Should().Be(PluginIconRule.Invalid);
    }

    /// <summary>An escape that is out of range, zero or a surrogate is harmless on its own and does not break the check.</summary>
    /// <param name="value">The style text.</param>
    [Theory]
    [InlineData("rect{fill:\\110000 red}")]
    [InlineData("rect{fill:\\0 red}")]
    [InlineData("rect{fill:\\D800 red}")]
    [InlineData("rect{fill:\\DC00 red}")]
    [InlineData("rect{fill:\\1F600 red}")]
    [InlineData("rect{fill:\\72 ed}")]
    public void Check_CssEscapeEdgeCases_HarmlessStyleIsAccepted(string value)
    {
        var result = CheckSvg($"<style>{value}</style>");

        result.Violation.Should().BeNull();
        result.Icon.Should().NotBeNull();
    }

    /// <summary>A valid SVG with an XML declaration passes, and so does an animation of a harmless attribute with values.</summary>
    /// <param name="svg">The SVG text.</param>
    [Theory]
    [InlineData("<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 1 1\"><rect width=\"1\" height=\"1\"/></svg>")]
    [InlineData("<svg xmlns=\"http://www.w3.org/2000/svg\"><rect><animate attributeName=\"fill\" from=\"#000\" to=\"#fff\" dur=\"1s\"/></rect></svg>")]
    [InlineData("<svg xmlns=\"http://www.w3.org/2000/svg\"><rect><animate attributeName=\"fill\" values=\"#000;#fff;#000\" dur=\"1s\"/></rect></svg>")]
    [InlineData("<svg xmlns=\"http://www.w3.org/2000/svg\"><rect><set attributeName=\"opacity\" to=\"0.5\"/></rect></svg>")]
    [InlineData("<!-- a comment --><svg xmlns=\"http://www.w3.org/2000/svg\"><!-- inside --><rect/></svg>")]
    public void Check_ValidSvg_IsAccepted(string svg)
    {
        var result = CheckFile("icon.svg", svg);

        result.Violation.Should().BeNull();
        result.Icon!.ContentType.Should().Be("image/svg+xml");
    }

    /// <summary>An animation that sets a reference or an event handler, or loads something outside, is refused.</summary>
    /// <param name="animation">The animation element.</param>
    [Theory]
    [InlineData("<animate attributeName=\"fill\" to=\"url(https://example.org/x)\"/>")]
    [InlineData("<animate attributeName=\"fill\" values=\"#000;url(https://example.org/x)\"/>")]
    [InlineData("<animate attributeName=\"fill\" values=\"javascript:alert(1)\"/>")]
    [InlineData("<animate attributeName=\"href\" to=\"https://example.org/x\"/>")]
    [InlineData("<animate attributeName=\" href \" to=\"https://example.org/x\"/>")]
    [InlineData("<animate attributeName=\"\thref\n\" to=\"https://example.org/x\"/>")]
    [InlineData("<animate attributeName=\"xlink:href\" to=\"https://example.org/x\"/>")]
    [InlineData("<animate attributeName=\" onclick\" to=\"x()\"/>")]
    [InlineData("<animate attributeName=\"ONLOAD\" to=\"x()\"/>")]
    [InlineData("<animate attributeName=\"svg:onclick\" to=\"x()\"/>")]
    [InlineData("<set attributeName=\"fill\" to=\"url(https://example.org/x)\"/>")]
    [InlineData("<set attributeName=\" href\" to=\"x\"/>")]
    public void Check_HostileAnimation_IsInvalid(string animation)
    {
        CheckSvg($"<rect>{animation}</rect>").Violation!.Rule.Should().Be(PluginIconRule.Invalid);
    }

    /// <summary>A detail never repeats a name or a value taken from the icon file.</summary>
    /// <param name="inner">The markup inside the root.</param>
    /// <param name="echo">A piece of the file that must not come back in the detail.</param>
    [Theory]
    [InlineData("<IFRAME/>", "IFRAME")]
    [InlineData("<rect onSECRETx=\"1\"/>", "SECRET")]
    [InlineData("<rect style=\"fill:url(https://secret-host.example/x)\"/>", "secret-host")]
    [InlineData("<image href=\"https://secret-host.example/x.png\"/>", "secret-host")]
    [InlineData("<style>@import 'secret-host.css';</style>", "secret-host")]
    public void Check_SvgDetail_NeverEchoesTheFile(string inner, string echo)
    {
        var detail = CheckSvg(inner).Violation!.Detail;

        detail.Should().NotContain(echo);
    }

    /// <summary>A file that is not valid UTF-8 text is invalid and the detail carries no parser message.</summary>
    [Fact]
    public void Check_BrokenEncoding_IsInvalidWithoutAParserMessage()
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes("<svg xmlns=\"http://www.w3.org/2000/svg\"><title>");
        var tail = System.Text.Encoding.UTF8.GetBytes("</title></svg>");
        var all = bytes.Concat(new byte[] { 0xC3, 0x28 }).Concat(tail).ToArray();

        var violation = CheckBytes("icon.svg", all).Violation!;

        violation.Rule.Should().Be(PluginIconRule.Invalid);
        violation.Detail.Should().Be("icon 'icon.svg' is not well-formed SVG");
    }

    /// <summary>
    /// A style made of unterminated comment openers up to the size cap is refused, and quickly: comments are stripped by a
    /// linear scan, so a crafted file cannot keep the checker busy.
    /// </summary>
    [Fact(Timeout = 20000)]
    public async Task Check_UnterminatedCommentsUpToTheSizeCap_IsInvalidAndFast()
    {
        const string prefix = SvgOpen + "<style>";
        const string suffix = "</style></svg>";
        var repeats = (int)((PluginIconChecker.MaxIconBytes - prefix.Length - suffix.Length) / "/*a".Length);
        var svg = prefix + string.Concat(Enumerable.Repeat("/*a", repeats)) + suffix;
        svg.Length.Should().BeLessThanOrEqualTo((int)PluginIconChecker.MaxIconBytes);

        var clock = System.Diagnostics.Stopwatch.StartNew();
        var result = await Task.Run(() => CheckFile("icon.svg", svg));
        clock.Stop();

        result.Icon.Should().BeNull();
        result.Violation!.Rule.Should().Be(PluginIconRule.Invalid);
        clock.Elapsed.Should().BeLessThan(TimeSpan.FromSeconds(5));
    }

    /// <summary>A comment that closes is read as a space, so it separates tokens the way a stylesheet does (a url followed by a comment and a bracket is still a url reference), and a closed one in a harmless style passes.</summary>
    [Fact]
    public void Check_ClosedComments_AreReadAsASpace()
    {
        CheckSvg("<style>rect{fill:#fff} /* note */ circle{fill:#000}</style>").Violation.Should().BeNull();
        CheckSvg("<style>url/**/(https://example.org/x)</style>").Violation!.Rule.Should().Be(PluginIconRule.Invalid);
        CheckSvg("<style>rect{fill:#fff} /* never closed</style>").Violation!.Rule.Should().Be(PluginIconRule.Invalid);
    }
}
