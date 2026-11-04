namespace Hostpanel.Sdk.Plugins.StoredCredentials;

/// <summary>
/// Checks a plugin author runs in the plugin's own tests to prove it keeps the promises of
/// <see cref="IStoredCredentialPlugin"/> that the compiler cannot see. Nothing here depends on a test framework.
/// </summary>
public static class StoredCredentialContract
{
    /// <summary>
    /// Calls <see cref="IStoredCredentialPlugin.CompleteSaveMethodAsync"/> repeatedly with one session id and asserts the
    /// promise of that method: every call answers the same result, a saved method is the same one every time and never
    /// stored twice, and an abandoned or expired session answers <c>Saved = false</c> without throwing. The caller
    /// arranges the session first (a session the payer completed, or one that is abandoned or unknown) and passes its id.
    /// </summary>
    /// <param name="plugin">The plugin under test.</param>
    /// <param name="sessionId">The id of the session to complete, as returned in <see cref="SaveMethodSession.SessionId"/>.</param>
    /// <param name="calls">How many times to complete the session; at least 2.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The result every call answered, so the test can assert more about it.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="plugin"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="sessionId"/> is empty or white space.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="calls"/> is less than 2.</exception>
    /// <exception cref="PluginContractViolationException">
    /// A call threw, answered <see langword="null"/>, answered <c>Saved = true</c> without a method or
    /// <c>Saved = false</c> with one, or answered a different result from the first call.
    /// </exception>
    public static async Task<SavedMethodResult> AssertCompleteSaveMethodIsIdempotentAsync(
        IStoredCredentialPlugin plugin,
        string sessionId,
        int calls = 3,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(plugin);
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);
        ArgumentOutOfRangeException.ThrowIfLessThan(calls, 2);

        SavedMethodResult? first = null;
        for (var call = 1; call <= calls; call++)
        {
            SavedMethodResult result;
            try
            {
                result = await plugin.CompleteSaveMethodAsync(sessionId, ct);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                throw new PluginContractViolationException(
                    $"CompleteSaveMethodAsync threw on call {call} for session '{sessionId}': {ex.GetType().Name}. An abandoned or expired session must answer Saved = false, and a repeat must answer the same result.");
            }

            if (result is null)
            {
                throw new PluginContractViolationException($"CompleteSaveMethodAsync answered null on call {call}.");
            }

            if (result.Saved != (result.Method is not null))
            {
                throw new PluginContractViolationException(
                    $"CompleteSaveMethodAsync answered Saved = {result.Saved} with{(result.Method is null ? "out" : string.Empty)} a method on call {call}; Saved and Method must agree.");
            }

            if (first is null)
            {
                first = result;
            }
            else if (result != first)
            {
                throw new PluginContractViolationException(
                    $"CompleteSaveMethodAsync answered a different result on call {call} than on call 1 for session '{sessionId}'. It must be idempotent per session id.");
            }
        }

        return first!;
    }
}
