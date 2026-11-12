using Hostpanel.Sdk.Plugins.Payment;

namespace Hostpanel.Sdk.Tests.Payment;

/// <summary>Tests of the form POST kind of <see cref="PaymentSession"/>.</summary>
public sealed class PaymentFormPostTests
{
    /// <summary>A session built the old way is a plain redirect and equals what it always did.</summary>
    [Fact]
    public void PaymentSession_BuiltTheOldWay_IsAPlainRedirect()
    {
        var session = new PaymentSession("gw-1", "https://pay.example/gw-1");

        session.FormPost.Should().BeNull();
        session.Should().Be(new PaymentSession("gw-1", "https://pay.example/gw-1", null));
        session.GatewayOrderId.Should().Be("gw-1");
        session.RedirectUrl.Should().Be("https://pay.example/gw-1");
    }

    /// <summary>A session with a form carries the action and the fields in order, including a repeated name.</summary>
    [Fact]
    public void PaymentSession_WithForm_CarriesActionAndFields()
    {
        var form = new PaymentFormPost(
            "https://pay.example/hosted",
            [new KeyValuePair<string, string>("token", "t-1"), new KeyValuePair<string, string>("token", "t-2")]);

        var session = new PaymentSession("gw-1", string.Empty, form);

        session.FormPost!.Action.Should().Be("https://pay.example/hosted");
        session.FormPost.Fields.Select(field => field.Value).Should().Equal("t-1", "t-2");
        session.Should().NotBe(new PaymentSession("gw-1", string.Empty));
    }

    /// <summary>A form with no fields is a valid shape; the host decides what to do with it.</summary>
    [Fact]
    public void PaymentFormPost_NoFields_IsAShape()
    {
        var form = new PaymentFormPost("https://pay.example/hosted", []);

        form.Fields.Should().BeEmpty();
    }
}
