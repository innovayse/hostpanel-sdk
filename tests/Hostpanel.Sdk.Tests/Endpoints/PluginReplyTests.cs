using Hostpanel.Sdk.Plugins.Endpoints;
using Hostpanel.Sdk.Plugins.Enums;

namespace Hostpanel.Sdk.Tests.Endpoints;

/// <summary>
/// Tests of the JSON, no content, problem and stream replies. The raw and the redirect replies have their own test classes.
/// </summary>
public sealed class PluginReplyTests
{
    /// <summary>A JSON reply is 200 unless it says 201, 202 or 203, and keeps its body.</summary>
    /// <param name="status">The status asked for.</param>
    [Theory]
    [InlineData(200)]
    [InlineData(201)]
    [InlineData(202)]
    [InlineData(203)]
    public void Json_AcceptsASuccessStatusWithABody(int status)
    {
        var body = new { ok = true };

        var reply = PluginReply.Json(body, status);

        reply.Kind.Should().Be(PluginReplyKind.Json);
        reply.Status.Should().Be(status);
        reply.Body.Should().BeSameAs(body);
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

    /// <summary>A problem accepts a failure status from 400 to 599 except the two challenges, and keeps its code and detail.</summary>
    /// <param name="status">The status asked for.</param>
    [Theory]
    [InlineData(400)]
    [InlineData(403)]
    [InlineData(404)]
    [InlineData(500)]
    [InlineData(599)]
    public void Problem_AcceptsAFailureStatus(int status)
    {
        var reply = PluginReply.Problem(status, "invoice_not_found", "No such invoice.");

        reply.Kind.Should().Be(PluginReplyKind.Problem);
        reply.Status.Should().Be(status);
        reply.Code.Should().Be("invoice_not_found");
        reply.Detail.Should().Be("No such invoice.");
    }

    /// <summary>A problem refuses a status below 400, above 599 and the two challenges the host answers itself, naming the way out.</summary>
    /// <param name="status">The status asked for.</param>
    [Theory]
    [InlineData(399)]
    [InlineData(401)]
    [InlineData(407)]
    [InlineData(600)]
    public void Problem_RefusesAStatusThatIsNotAPluginFailure(int status)
    {
        var problem = () => PluginReply.Problem(status, "invoice_not_found");

        problem.Should().ThrowExactly<ArgumentOutOfRangeException>()
            .WithParameterName("status")
            .Where(ex => ex.Message.Contains("A problem needs a 4xx or 5xx status other than 401 and 407; answer 403 to refuse a caller."));
    }

    /// <summary>A stream is 200 and carries the events it was given.</summary>
    [Fact]
    public void Stream_Is200AndCarriesTheEvents()
    {
        var events = NoEvents();

        var reply = PluginReply.Stream(events);

        reply.Kind.Should().Be(PluginReplyKind.Stream);
        reply.Status.Should().Be(200);
        reply.Events.Should().BeSameAs(events);
    }

    /// <summary>
    /// The default statuses are baked into the code of every plugin that leaves the argument out, so the metadata must
    /// keep saying 200 for a JSON reply and 303 for a redirect.
    /// </summary>
    /// <param name="method">The factory.</param>
    /// <param name="expected">The default status.</param>
    [Theory]
    [InlineData(nameof(PluginReply.Json), 200)]
    [InlineData(nameof(PluginReply.Redirect), 303)]
    public void StatusParameterDefaults_AreThoseOfTheContract(string method, int expected)
    {
        var parameter = typeof(PluginReply).GetMethod(method)!.GetParameters().Single(candidate => candidate.Name == "status");

        parameter.HasDefaultValue.Should().BeTrue();
        parameter.DefaultValue.Should().Be(expected);
    }

    /// <summary>An empty stream of events.</summary>
    /// <returns>The events.</returns>
    private static async IAsyncEnumerable<PluginEvent> NoEvents()
    {
        await Task.CompletedTask;
        yield break;
    }
}
