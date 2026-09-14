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
        var configuredNetworkKeys = options.Value.Accounts
            .Select(account => connectorResolver.Resolve(account.Network).Network.Key)
            .ToArray();
        var states = await database.GetNetworkStatesAsync(cancellationToken);

        return SynchronizationSchedule.GetInitialDelay(
            configuredNetworkKeys,
            states,
            options.Value.SynchronizationInterval,
            timeProvider.GetUtcNow());
    }

    private async Task RunSynchronizationAsync(CancellationToken cancellationToken)
    {
        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var synchronizer = scope.ServiceProvider.GetRequiredService<LoanSynchronizationService>();
            await synchronizer.SynchronizeAllAsync(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            LogCycleFailure(logger, exception);
        }
    }

    [LoggerMessage(
        EventId = 1003,
        Level = LogLevel.Error,
        Message = "The synchronization cycle failed unexpectedly.")]
    private static partial void LogCycleFailure(ILogger logger, Exception exception);

    [LoggerMessage(
        EventId = 1004,
        Level = LogLevel.Information,
        Message = "Startup synchronization is not due for {Delay}.")]
    private static partial void LogStartupSynchronizationDeferred(ILogger logger, TimeSpan delay);
}
