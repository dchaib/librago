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
    SynchronizationResult Result);

