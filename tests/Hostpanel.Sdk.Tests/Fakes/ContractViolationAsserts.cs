using Hostpanel.Sdk.Plugins.StoredCredentials;

namespace Hostpanel.Sdk.Tests.Fakes;

/// <summary>The assertion the contract helper tests share: a helper call throws a violation that names what was broken.</summary>
internal static class ContractViolationAsserts
{
    /// <summary>Asserts that a helper call throws a violation whose message contains every given text.</summary>
    /// <param name="run">The helper call.</param>
    /// <param name="expected">The texts the message must contain.</param>
    /// <returns>A task that completes when the check passed.</returns>
    internal static async Task AssertViolationAsync(Func<Task> run, params string[] expected)
    {
        var message = (await run.Should().ThrowAsync<PluginContractViolationException>()).Which.Message;
        foreach (var part in expected)
        {
            message.Should().Contain(part);
        }
    }
}
