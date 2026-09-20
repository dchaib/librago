namespace Librago.Synchronization;

public enum SynchronizationResult
{
    Success,
    Partial,
    Failed
}

public sealed record NetworkSynchronizationState(
    string NetworkKey,
    string NetworkName,
    DateTimeOffset LastAttemptAt,
    DateTimeOffset? LastCompleteSuccessAt,
    IReadOnlyList<string>? LastCompleteSuccessAccountIds,
    SynchronizationResult Result)
{
    public bool CoversAccounts(IEnumerable<string> accountIds) =>
        LastCompleteSuccessAccountIds is not null &&
        new HashSet<string>(LastCompleteSuccessAccountIds, StringComparer.OrdinalIgnoreCase)
            .SetEquals(accountIds);
}

public sealed record ConfiguredNetwork(
    string NetworkKey,
    IReadOnlyList<string> AccountIds);
