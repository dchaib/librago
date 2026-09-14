namespace Librago.Synchronization;

internal static class SynchronizationSchedule
{
    public static TimeSpan GetInitialDelay(
        IEnumerable<string> configuredNetworkKeys,
        IReadOnlyCollection<NetworkSynchronizationState> states,
        TimeSpan interval,
        DateTimeOffset now)
    {
        var statesByNetwork = states.ToDictionary(
            state => state.NetworkKey,
            StringComparer.OrdinalIgnoreCase);
        DateTimeOffset? nextAttemptAt = null;

        foreach (var networkKey in configuredNetworkKeys.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            if (!statesByNetwork.TryGetValue(networkKey, out var state))
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
