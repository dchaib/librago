namespace Librago.Synchronization;

internal static class SynchronizationSchedule
{
    public static TimeSpan GetInitialDelay(
        IEnumerable<ConfiguredNetwork> configuredNetworks,
        IReadOnlyCollection<NetworkSynchronizationState> states,
        TimeSpan interval,
        DateTimeOffset now)
    {
        var statesByNetwork = states.ToDictionary(
            state => state.NetworkKey,
            StringComparer.OrdinalIgnoreCase);
        DateTimeOffset? nextAttemptAt = null;

        foreach (var configuredNetwork in configuredNetworks)
        {
            if (!statesByNetwork.TryGetValue(configuredNetwork.NetworkKey, out var state) ||
                !state.CoversAccounts(configuredNetwork.AccountIds))
            {
                return TimeSpan.Zero;
            }

            var dueAt = state.LastAttemptAt + interval;
            if (dueAt <= now)
            {
                return TimeSpan.Zero;
            }

            nextAttemptAt = nextAttemptAt is null || dueAt < nextAttemptAt
                ? dueAt
                : nextAttemptAt;
        }

        return nextAttemptAt is null ? TimeSpan.Zero : nextAttemptAt.Value - now;
    }
}
