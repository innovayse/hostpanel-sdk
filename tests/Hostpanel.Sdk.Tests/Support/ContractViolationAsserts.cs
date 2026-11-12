using Hostpanel.Sdk.Plugins.StoredCredentials;

namespace Hostpanel.Sdk.Tests.Support;

/// <summary>The assertions the contract helper tests share: a helper call throws a violation that names what was broken.</summary>
internal static class ContractViolationAsserts
{
    /// <summary>Runs a helper call that must throw a violation and gives back the message of it, for a test that checks more than that it contains a text.</summary>
    /// <param name="run">The helper call.</param>
    /// <returns>The message of the violation.</returns>
    internal static async Task<string> ViolationMessageAsync(Func<Task> run)
    {
        return (await run.Should().ThrowAsync<PluginContractViolationException>()).Which.Message;
    }

    /// <summary>Runs a helper call that must throw a violation and gives back the message of it.</summary>
    /// <param name="run">The helper call.</param>
    /// <returns>The message of the violation.</returns>
    internal static string ViolationMessage(Action run)
    {
        return run.Should().Throw<PluginContractViolationException>().Which.Message;
    }

    /// <summary>Asserts that a helper call throws a violation whose message contains every given text.</summary>
    /// <param name="run">The helper call.</param>
    /// <param name="expected">The texts the message must contain.</param>
    /// <returns>A task that completes when the check passed.</returns>
    internal static async Task AssertViolationAsync(Func<Task> run, params string[] expected)
    {
        var message = await ViolationMessageAsync(run);
        foreach (var part in expected)
        {
            message.Should().Contain(part);
        }
    }

    /// <summary>Asserts that a helper call throws a violation with exactly this message, for the tests that pin the wording.</summary>
    /// <param name="run">The helper call.</param>
    /// <param name="expected">The whole message.</param>
    /// <returns>A task that completes when the check passed.</returns>
    internal static async Task AssertExactViolationAsync(Func<Task> run, string expected)
    {
        (await ViolationMessageAsync(run)).Should().Be(expected);
    }
}
