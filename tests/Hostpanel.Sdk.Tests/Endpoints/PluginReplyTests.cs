using Hostpanel.Sdk.Plugins.Endpoints;
using Hostpanel.Sdk.Plugins.Enums;

namespace Hostpanel.Sdk.Tests.Endpoints;

/// <summary>Tests of the statuses the JSON, no content, problem, raw, redirect and stream replies accept.</summary>
public sealed class PluginReplyTests
{
    /// <summary>A JSON reply is 200 unless it says 201, 202 or 203.</summary>
    /// <param name="status">The status asked for.</param>
    [Theory]
    [InlineData(200)]
    [InlineData(201)]
    [InlineData(202)]
    [InlineData(203)]
    public void Json_AcceptsASuccessStatusWithABody(int status)
    {
        var reply = PluginReply.Json(new { ok = true }, status);

        reply.Kind.Should().Be(PluginReplyKind.Json);
        reply.Status.Should().Be(status);
    }

    /// <summary>A JSON reply without a status is 200.</summary>
    [Fact]
    public void Json_DefaultsTo200()
    {
        PluginReply.Json(new { ok = true }).Status.Should().Be(200);
    }

    /// <summary>A JSON reply refuses 204, which has no body, and every other status, naming the four it accepts.</summary>
    /// <param name="status">The status asked for.</param>
    [Theory]
    [InlineData(199)]
    [InlineData(204)]
    [InlineData(301)]
    [InlineData(404)]
    public void Json_RefusesAnyOtherStatus(int status)
    {
        var json = () => PluginReply.Json(new { ok = true }, status);

        json.Should().ThrowExactly<ArgumentOutOfRangeException>()
            .WithParameterName("status")
            .Where(ex => ex.Message.Contains("A JSON reply needs a status of 200, 201, 202 or 203."));
    }

    /// <summary>A reply without content is 204.</summary>
    [Fact]
    public void NoContent_Is204()
    {
        var reply = PluginReply.NoContent();

        reply.Kind.Should().Be(PluginReplyKind.NoContent);
        reply.Status.Should().Be(204);
    }

    /// <summary>A problem accepts a failure status from 400 to 599 except the two challenges the host answers itself.</summary>
    /// <param name="status">The status asked for.</param>
    /// <param name="accepted">Whether it is accepted.</param>
    [Theory]
    [InlineData(399, false)]
    [InlineData(400, true)]
    [InlineData(401, false)]
    [InlineData(403, true)]
    [InlineData(407, false)]
    [InlineData(599, true)]
    [InlineData(600, false)]
    public void Problem_AcceptsAFailureStatusButNotAHostChallenge(int status, bool accepted)
    {
        var problem = () => PluginReply.Problem(status, "invoice_not_found");

        if (accepted)
        {
            problem.Should().NotThrow();
        }
        else
        {
            problem.Should().ThrowExactly<ArgumentOutOfRangeException>()
                .Where(ex => ex.Message.Contains("A problem needs a 4xx or 5xx status other than 401 and 407; answer 403 to refuse a caller."));
        }
    }

    /// <summary>A raw reply refuses 401 and 407 as a problem does, and names the range it accepts.</summary>
    /// <param name="status">The status asked for.</param>
    [Theory]
    [InlineData(199)]
    [InlineData(401)]
    [InlineData(407)]
    [InlineData(600)]
    public void Raw_RefusesAStatusOutsideItsRange(int status)
    {
        var raw = () => PluginReply.Raw(status, default, "text/plain");

        raw.Should().ThrowExactly<ArgumentOutOfRangeException>()
            .Where(ex => ex.Message.Contains("A raw reply needs a status from 200 to 599 other than 401 and 407; answer 403 to refuse a caller."));
    }

    /// <summary>A redirect names the five statuses it accepts, and a stream answers 200.</summary>
    [Fact]
    public void Redirect_NamesItsStatuses_AndStreamIs200()
    {
        var redirect = () => PluginReply.Redirect("/next", 200);

        redirect.Should().ThrowExactly<ArgumentOutOfRangeException>()
            .Where(ex => ex.Message.Contains("A redirect needs a status of 301, 302, 303, 307 or 308."));
        PluginReply.Redirect("/next").Status.Should().Be(303);
        PluginReply.Stream(AsyncEnumerable()).Status.Should().Be(200);
    }

    /// <summary>An empty stream of events.</summary>
    /// <returns>The events.</returns>
    private static async IAsyncEnumerable<PluginEvent> AsyncEnumerable()
    {
        await Task.CompletedTask;
        yield break;
    }
}
