using Librago.Configuration;
using Librago.Connectors;
using Librago.Persistence;
using Microsoft.Extensions.Options;

namespace Librago.Synchronization;

public sealed partial class SynchronizationWorker(
    IServiceScopeFactory scopeFactory,
    IOptions<LibragoOptions> options,
    LibraryConnectorResolver connectorResolver,
    LibragoDatabase database,
    TimeProvider timeProvider,
    ILogger<SynchronizationWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var initialDelay = await GetInitialDelayAsync(stoppingToken);
        if (initialDelay > TimeSpan.Zero)
        {
            LogStartupSynchronizationDeferred(logger, initialDelay);
            await Task.Delay(initialDelay, stoppingToken);
        }

        await RunSynchronizationAsync(stoppingToken);

        using var timer = new PeriodicTimer(options.Value.SynchronizationInterval);
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await RunSynchronizationAsync(stoppingToken);
        }
    }

    private async Task<TimeSpan> GetInitialDelayAsync(CancellationToken cancellationToken)
    {
        var configuredNetworks = options.Value.Accounts
            .GroupBy(
                account => connectorResolver.Resolve(account.Network).Network.Key,
                StringComparer.OrdinalIgnoreCase)
            .Select(group => new ConfiguredNetwork(
                group.Key,
                group.Select(account => account.AccountId).ToArray()))
            .ToArray();
        var states = await database.GetNetworkStatesAsync(cancellationToken);
        var reservationStates = await database.GetReservationNetworkStatesAsync(cancellationToken);

        var loanDelay = SynchronizationSchedule.GetInitialDelay(
            configuredNetworks,
            states,
            options.Value.SynchronizationInterval,
            timeProvider.GetUtcNow());
        var reservationDelay = SynchronizationSchedule.GetInitialDelay(
            configuredNetworks, reservationStates, options.Value.SynchronizationInterval,
            timeProvider.GetUtcNow());
        return loanDelay < reservationDelay ? loanDelay : reservationDelay;
    }

    private async Task RunSynchronizationAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var loans = scope.ServiceProvider.GetRequiredService<LoanSynchronizationService>();
        var reservations = scope.ServiceProvider.GetRequiredService<ReservationSynchronizationService>();
        // Keep portal sessions for the same account from authenticating concurrently.
        await TrySynchronizeAsync(() => loans.SynchronizeAllAsync(cancellationToken), cancellationToken);
        await TrySynchronizeAsync(() => reservations.SynchronizeAllAsync(cancellationToken), cancellationToken);
    }

    private async Task TrySynchronizeAsync(Func<Task> synchronize, CancellationToken cancellationToken)
    {
        try
        {
            await synchronize();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            LogCycleFailure(logger, exception.GetType().Name);
        }
    }

    [LoggerMessage(
        EventId = 1003,
        Level = LogLevel.Error,
        Message = "The synchronization cycle failed unexpectedly ({ExceptionType}).")]
    private static partial void LogCycleFailure(ILogger logger, string exceptionType);

    [LoggerMessage(
        EventId = 1004,
        Level = LogLevel.Information,
        Message = "Startup synchronization is not due for {Delay}.")]
    private static partial void LogStartupSynchronizationDeferred(ILogger logger, TimeSpan delay);
}
