using Hostpanel.Sdk.Plugins.Enums;
using Hostpanel.Sdk.Plugins.Payment;

namespace Hostpanel.Sdk.Tests.Payment;

/// <summary>Tests of the session kind of <see cref="PaymentSession"/>: its defaults, its rules and the old
/// constructors.</summary>
public sealed class PaymentSessionKindTests
{
    /// <summary>The address every session of the tests redirects to.</summary>
    private const string Address = "https://pay.example/gw-1";

    /// <summary>A form every form post session of the tests posts.</summary>
    private static readonly PaymentFormPost Form = new(
        "https://pay.example/hosted", [new KeyValuePair<string, string>("token", "t-1")]);

    /// <summary>The values of the enum are explicit, start at 1 and are never renumbered.</summary>
    [Fact]
    public void PaymentSessionKind_Values_ArePinned()
    {
        Enum.GetValues<PaymentSessionKind>().Select(value => (int)value).Should().Equal(1, 2, 3);
        Enum.GetNames<PaymentSessionKind>().Should().Equal("Redirect", "FormPost", "BrowserStep");
        Enum.IsDefined((PaymentSessionKind)0).Should().BeFalse();
    }

    /// <summary>A session built the old way with two arguments is a redirect.</summary>
    [Fact]
    public void TwoArguments_IsARedirect()
    {
        var session = new PaymentSession("gw-1", Address);

        session.Kind.Should().Be(PaymentSessionKind.Redirect);
        session.FormPost.Should().BeNull();
    }

    /// <summary>A session built as in the form post version, with three arguments, keeps its meaning.</summary>
    [Fact]
    public void ThreeArguments_KeepTheirMeaning()
    {
        new PaymentSession("gw-1", Address, null).Kind.Should().Be(PaymentSessionKind.Redirect);
        new PaymentSession("gw-1", Form.Action, Form).Kind.Should().Be(PaymentSessionKind.FormPost);
    }

    /// <summary>The constructors of the earlier versions are still public, so a plugin built against them
    /// binds.</summary>
    [Fact]
    public void EarlierConstructors_StayPublic()
    {
        var two = typeof(PaymentSession).GetConstructor([typeof(string), typeof(string)]);
        var three = typeof(PaymentSession).GetConstructor([typeof(string), typeof(string), typeof(PaymentFormPost)]);

        two.Should().NotBeNull();
        three.Should().NotBeNull();
        two!.Invoke(["gw-1", Address]).Should().Be(new PaymentSession("gw-1", Address));
        three!.Invoke(["gw-1", Form.Action, Form]).Should().Be(new PaymentSession("gw-1", Form.Action, Form));
    }

    /// <summary>The browser step is its own kind, and the form post is still a form post.</summary>
    [Fact]
    public void BrowserStep_IsItsOwnKind()
    {
        var step = new PaymentSession("gw-1", Address, null, PaymentSessionKind.BrowserStep);
        var form = new PaymentSession("gw-1", Form.Action, Form, PaymentSessionKind.FormPost);

        step.Kind.Should().Be(PaymentSessionKind.BrowserStep);
        form.Kind.Should().Be(PaymentSessionKind.FormPost);
        step.Should().NotBe(new PaymentSession("gw-1", Address));
    }

    /// <summary>A form post with the redirect kind is a contradiction and is refused, not silently turned into a
    /// form.</summary>
    [Fact]
    public void FormPostWithRedirectKind_Throws()
    {
        var build = () => new PaymentSession("gw-1", Form.Action, Form, PaymentSessionKind.Redirect);

        build.Should().Throw<ArgumentException>();
    }

    /// <summary>A kind that contradicts the form post is refused.</summary>
    [Fact]
    public void ContradictingKind_Throws()
    {
        var formWithoutForm = () => new PaymentSession("gw-1", Address, null, PaymentSessionKind.FormPost);
        var stepWithForm = () => new PaymentSession("gw-1", Form.Action, Form, PaymentSessionKind.BrowserStep);

        formWithoutForm.Should().Throw<ArgumentException>();
        stepWithForm.Should().Throw<ArgumentException>();
    }

    /// <summary>An undefined kind is refused.</summary>
    /// <param name="value">The value to try.</param>
    [Theory]
    [InlineData(0)]
    [InlineData(4)]
    [InlineData(-1)]
    public void UndefinedKind_Throws(int value)
    {
        var build = () => new PaymentSession("gw-1", Address, null, (PaymentSessionKind)value);

        build.Should().Throw<ArgumentOutOfRangeException>();
    }

    /// <summary>A redirect session can become a browser step and back with a with expression.</summary>
    [Fact]
    public void With_Redirect_CanChangeToBrowserStepAndBack()
    {
        var redirect = new PaymentSession("gw-1", Address);

        var step = redirect with { Kind = PaymentSessionKind.BrowserStep };
        var back = step with { Kind = PaymentSessionKind.Redirect };

        step.Kind.Should().Be(PaymentSessionKind.BrowserStep);
        back.Should().Be(redirect);
    }

    /// <summary>Every with expression that would contradict the pair on a redirect or browser step session is
    /// refused.</summary>
    [Fact]
    public void With_RedirectOrBrowserStep_ContradictionThrows()
    {
        var redirect = new PaymentSession("gw-1", Address);
        var step = new PaymentSession("gw-1", Address, null, PaymentSessionKind.BrowserStep);

        var formKind = () => redirect with { Kind = PaymentSessionKind.FormPost };
        var formOnRedirect = () => redirect with { FormPost = Form };
        var formOnStep = () => step with { FormPost = Form };
        var undefined = () => redirect with { Kind = (PaymentSessionKind)9 };

        formKind.Should().Throw<ArgumentException>();
        formOnRedirect.Should().Throw<ArgumentException>();
        formOnStep.Should().Throw<ArgumentException>();
        undefined.Should().Throw<ArgumentOutOfRangeException>();
    }

    /// <summary>A form post session can have its form replaced and nothing else changed.</summary>
    [Fact]
    public void With_FormPost_CanReplaceTheForm()
    {
        var session = new PaymentSession("gw-1", Form.Action, Form);
        var other = new PaymentFormPost("https://pay.example/other", []);

        var replaced = session with { FormPost = other };

        replaced.FormPost.Should().BeSameAs(other);
        replaced.Kind.Should().Be(PaymentSessionKind.FormPost);
    }

    /// <summary>Every with expression that would take a form post session out of its kind is refused, in any
    /// order.</summary>
    [Fact]
    public void With_FormPost_ContradictionThrowsInAnyOrder()
    {
        var session = new PaymentSession("gw-1", Form.Action, Form);

        var noForm = () => session with { FormPost = null };
        var redirectKind = () => session with { Kind = PaymentSessionKind.Redirect };
        var stepKind = () => session with { Kind = PaymentSessionKind.BrowserStep };
        var formFirst = () => session with { FormPost = null, Kind = PaymentSessionKind.Redirect };
        var kindFirst = () => session with { Kind = PaymentSessionKind.Redirect, FormPost = null };

        noForm.Should().Throw<ArgumentException>();
        redirectKind.Should().Throw<ArgumentException>();
        stepKind.Should().Throw<ArgumentException>();
        formFirst.Should().Throw<ArgumentException>();
        kindFirst.Should().Throw<ArgumentException>();
    }

    /// <summary>A browser step that had a form added and removed does not come back as a hidden state.</summary>
    [Fact]
    public void With_BrowserStep_LeavesNoHiddenState()
    {
        var step = new PaymentSession("gw-1", Address, null, PaymentSessionKind.BrowserStep);
        var addForm = () => step with { FormPost = Form };

        addForm.Should().Throw<ArgumentException>();
        (step with { }).Kind.Should().Be(PaymentSessionKind.BrowserStep);
        (step with { Kind = PaymentSessionKind.Redirect }).FormPost.Should().BeNull();
    }

    /// <summary>The kind is part of equality: two sessions that differ only in it are not equal.</summary>
    [Fact]
    public void Equality_SeesTheKind()
    {
        var redirect = new PaymentSession("gw-1", Address);
        var step = new PaymentSession("gw-1", Address, null, PaymentSessionKind.BrowserStep);

        redirect.Should().NotBe(step);
        step.Should().Be(new PaymentSession("gw-1", Address, null, PaymentSessionKind.BrowserStep));
    }
}
