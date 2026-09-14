using Librago.Synchronization;

namespace Librago.Tests;

public sealed class SynchronizationScheduleTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 14, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void GetInitialDelayReturnsZeroWhenAConfiguredNetworkHasNoState()
    {
        var delay = SynchronizationSchedule.GetInitialDelay(
            ["nantes", "nozay"],
            [State("nantes", Now.AddHours(-1))],
            TimeSpan.FromHours(6),
            Now);

        Assert.Equal(TimeSpan.Zero, delay);
    }

    [Fact]
    public void GetInitialDelayReturnsZeroWhenThePreviousAttemptIsDue()
    {
        var delay = SynchronizationSchedule.GetInitialDelay(
            ["nantes"],
            [State("nantes", Now.AddHours(-6))],
            TimeSpan.FromHours(6),
            Now);

        Assert.Equal(TimeSpan.Zero, delay);
    }

    [Fact]
    public void GetInitialDelayWaitsUntilTheEarliestConfiguredNetworkIsDue()
    {
        var delay = SynchronizationSchedule.GetInitialDelay(
            ["nantes", "nozay"],
            [State("nantes", Now.AddHours(-1)), State("nozay", Now.AddHours(-2))],
            TimeSpan.FromHours(6),
            Now);

        Assert.Equal(TimeSpan.FromHours(4), delay);
    }

    private static NetworkSynchronizationState State(string networkKey, DateTimeOffset attemptedAt) =>
        new(networkKey, networkKey, attemptedAt, attemptedAt, SynchronizationResult.Success);
}
